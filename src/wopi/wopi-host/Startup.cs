using FutureNHS.WOPIHost.Configuration;
using FutureNHS.WOPIHost.HttpHelpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FutureNHS.WOPIHost
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient("wopi-discovery-document").AddCoreResiliencyPolicies();

            services.AddMemoryCache();

            var appInsightsInstrumentationKey = _configuration.GetValue<string>("APPINSIGHTS_INSTRUMENTATIONKEY");

            if (!string.IsNullOrWhiteSpace(appInsightsInstrumentationKey))
            {
                services.AddApplicationInsightsTelemetry(appInsightsInstrumentationKey);
            }
            else services.AddLogging();

            services.AddHttpContextAccessor();

            if (bool.TryParse(Environment.GetEnvironmentVariable("USE_AZURE_APP_CONFIGURATION"), out var useAppConfig) && useAppConfig)
            {
                services.AddAzureAppConfiguration();
            }

            services.Configure<Features>(_configuration.GetSection("FeatureManagement"), binderOptions => binderOptions.BindNonPublicProperties = true);
            services.Configure<WopiConfiguration>(_configuration.GetSection("Wopi"));

            services.AddSingleton<ISystemClock>(new SystemClock());

            services.AddScoped<RetryHandler>();
            services.AddScoped<WopiRequestFactory>();
            services.AddScoped<IWopiRequestFactory>(sp => sp.GetRequiredService<WopiRequestFactory>());

            services.AddScoped<WopiDiscoveryDocumentFactory>();
            services.AddScoped<IWopiDiscoveryDocumentFactory>(sp => sp.GetRequiredService<WopiDiscoveryDocumentFactory>());

            services.AddFeatureManagement().
                     AddFeatureFilter<TimeWindowFilter>().      // enable a feature between a start and end date ....... https://docs.microsoft.com/dotnet/api/microsoft.featuremanagement.featurefilters.timewindowfilter?view=azure-dotnet-preview
                     AddFeatureFilter<PercentageFilter>();      // for randomly sampling a percentage of the audience .. https://docs.microsoft.com/dotnet/api/microsoft.featuremanagement.featurefilters.percentagefilter?view=azure-dotnet-preview
                   //AddFeatureFilter<TargetingFilter>();       // for targeting certain audiences ..................... https://docs.microsoft.com/dotnet/api/microsoft.featuremanagement.featurefilters.targetingfilter?view=azure-dotnet-preview
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            if (bool.TryParse(Environment.GetEnvironmentVariable("USE_AZURE_APP_CONFIGURATION"), out var useAppConfig) && useAppConfig)
            {
                app.UseAzureAppConfiguration();
            }

            app.UseHttpsRedirection();
            //app.UseStaticFiles();
            //app.UseCookiePolicy();
            app.UseRouting();
            //app.UseRequestLocalization();
            //app.UseCors();
            //app.UseAuthentication();
            //app.UseAuthorization();
            //app.UseSession();
            //app.UseResponseCompression();
            //app.UseResponseCaching();

            app.UseMiddleware<WopiMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/error", GetProductionErrorPage);
                endpoints.MapGet("/wopi/health-check", GetHealthCheckPageAsync);
                endpoints.MapGet("/wopi/collabora",  ctxt => GetCollaboraHostPageAsync(ctxt));
           });
        }

        private static async Task GetProductionErrorPage(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var sb = new StringBuilder();

            sb.AppendLine($"<!doctype html>");
            sb.AppendLine($"<html>");
            sb.AppendLine($"  <body>");
            sb.AppendLine($"    <p>");
            sb.AppendLine($"      ==============================</br>");
            sb.AppendLine($"      Oooops - Internal server error</br>");
            sb.AppendLine($"      ==============================</br>");
            sb.AppendLine($"    </p>");
            sb.AppendLine($"  </body>");
            sb.AppendLine($"</html>");

            await httpContext.Response.WriteAsync(sb.ToString());
        }

        private static async Task GetHealthCheckPageAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = StatusCodes.Status200OK;

            var sb = new StringBuilder();

            sb.AppendLine($"<!doctype html>");
            sb.AppendLine($"<html>");
            sb.AppendLine($"  <body>");
            sb.AppendLine($"    <p>");
            sb.AppendLine($"      ======================================================</br>");
            sb.AppendLine($"      Hello from the CDS FutureNHS File Server PoC at {DateTime.UtcNow}</br>");
            sb.AppendLine($"      ======================================================</br>");
            sb.AppendLine($"    </p>");
            sb.AppendLine($"    <p>");
            sb.AppendLine($"      <h1>Environment Variables</h1></br>");
            sb.AppendLine($"      USE_AZURE_APP_CONFIGURATION = {Environment.GetEnvironmentVariable("USE_AZURE_APP_CONFIGURATION")}</br>");
            sb.AppendLine($"    </p>");
            sb.AppendLine($"    <p>");
            sb.AppendLine($"      <h1>Server Variables</h1></br>");

            var feature = httpContext.Features.OfType<Microsoft.AspNetCore.Http.Features.IServerVariablesFeature>().FirstOrDefault();

            if (feature is object)
            {
                // https://docs.microsoft.com/en-us/previous-versions/iis/6.0-sdk/ms524602(v=vs.90)?redirectedfrom=MSDN
                sb.AppendLine($"      UNENCODED_URL = {feature["UNENCODED_URL"] ?? "null"}</br>");                
            }
            else sb.AppendLine($"      IServerVariablesFeature is missing</br>");

            sb.AppendLine($"    </p>");
            sb.AppendLine($"  </body>");
            sb.AppendLine($"</html>");

            await httpContext.Response.WriteAsync(sb.ToString());
        }

        /// <summary>
        /// This is here purely as an example of how a host page needs to be rendered for it to be able to first post to Collabora and then have it 
        /// relay back to this WOPI host to serve up and manage the actual file
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        private static async Task GetCollaboraHostPageAsync(HttpContext httpContext)
        {
            var cancellationToken = httpContext.RequestAborted;

            // Try to get the discovery document from the Collabora server.  This tells us what document types are supported, but more importantly
            // the encrption keys it is using to sign the callback requests it sends to us.  We will use these keys to assure non-reupidation/tampering
            // If we fail to load this file, we cannot continue to build the page we are return

            var wopiDiscoveryDocumentFactory = httpContext.RequestServices.GetRequiredService<IWopiDiscoveryDocumentFactory>();

            var wopiDiscoveryDocument = await wopiDiscoveryDocumentFactory.CreateDocumentAsync(cancellationToken);

            if (wopiDiscoveryDocument is null) throw new ApplicationException("Unable to download the WOPI Discovery Document - remote host returned non-success status code");  

            // Identify the file that we want to view/edit by inspecting the incoming request for an id.  

            var fileId = httpContext.Request.Query["file_id"].FirstOrDefault()?.Trim();

            if (string.IsNullOrWhiteSpace(fileId)) fileId = "Excel-Spreadsheet.xlsx";

            var wopiConfiguration = httpContext.RequestServices.GetRequiredService<IOptionsSnapshot<WopiConfiguration>>().Value;

            var hostFileEndpointUrl = wopiConfiguration.HostFilesEndpoint;

            var wopiHostFileEndpointUrl = new Uri(Path.Combine(hostFileEndpointUrl, fileId), UriKind.Absolute);
            
            var fileExtension = Path.GetExtension(fileId);

            if (string.IsNullOrWhiteSpace(fileExtension)) return;  // TODO - Return appropriate status code to caller

            var fileAction = "view"; // edit | view | etc (see comments in discoveryDoc.GetEndpointForAsync)

            var collaboraOnlineEndpoint = wopiDiscoveryDocument.GetEndpointForFileExtension(fileExtension, fileAction, wopiHostFileEndpointUrl);

            if (collaboraOnlineEndpoint is null || !collaboraOnlineEndpoint.IsAbsoluteUri) return;  // TODO - Return appropriate status code to caller

            var sb = new StringBuilder();

            // https://wopi.readthedocs.io/projects/wopirest/en/latest/concepts.html#term-wopisrc

            // When running locally in DEBUG ...
            // https://127.0.0.1:9980/loleaflet/4aa2794/loleaflet.html? is the Collabora endpoint for this document type, pulled out of the discovery xml file hosted by Collabora
            // https://127.0.0.1:44355/wopi/files/<FILE_ID> is the url Collabora uses to callback to us to get the file information and contents

            // In Azure, the container will be mapped to port 80 in docker run command

            // TODO - Generate a token with a set TTL that is specific to the current user and file combination
            //        This token will be sent back to us by Collabora by way of it verifying the request (it will be signed so we know it 
            //        comes from them and hasn't been tampered with outside of the servers)
            //        For now, we'll just use a Guid

            var accessToken = Guid.NewGuid().ToString().Replace("-", string.Empty);

            // TODO - This is either going to have to be generated by MVCForum or somehow injected by it after a call to our API,
            //        but given the need for input elements, it might be more appropriate for us to just generate the token and 
            //        return both it and the collabora endpoint that needs to be used, or MVCForum gets the discovery document itself
            //        and generates a token we can later understand

            httpContext.Response.StatusCode = StatusCodes.Status200OK;

            sb.AppendLine($"<!doctype html>");
            sb.AppendLine($"<html>");
            sb.AppendLine($"  <body>");
            sb.AppendLine($"    <form action=\"{collaboraOnlineEndpoint.AbsoluteUri}\" enctype =\"multipart/form-data\" method=\"post\">");
            sb.AppendLine($"      <input name=\"access_token\" value=\"{ accessToken }\" type=\"hidden\">");
            sb.AppendLine($"      <input type=\"submit\" value=\"Load Document (Collabora)\">");
            sb.AppendLine($"    </form>");
            sb.AppendLine($"  </body>");
            sb.AppendLine($"</html>");

            await httpContext.Response.WriteAsync(sb.ToString());
        }
    }
}
