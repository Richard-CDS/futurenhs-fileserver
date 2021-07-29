using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
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
            services.AddHttpClient();

            services.AddMemoryCache();

            services.AddHttpContextAccessor();

            if (bool.TryParse(Environment.GetEnvironmentVariable("USE_AZURE_APP_CONFIGURATION"), out var useAppConfig) && useAppConfig)
            {
                services.AddAzureAppConfiguration();
            }

            var featureManagementConfigSection = _configuration.GetSection("FeatureManagement");

            services.Configure<Features>(featureManagementConfigSection, binderOptions => binderOptions.BindNonPublicProperties = true);

            services.AddFeatureManagement().
                     AddFeatureFilter<TimeWindowFilter>().      // enable a feature between a start and end date ....... https://docs.microsoft.com/dotnet/api/microsoft.featuremanagement.featurefilters.timewindowfilter?view=azure-dotnet-preview
                     AddFeatureFilter<PercentageFilter>();      // for randomly sampling a percentage of the audience .. https://docs.microsoft.com/dotnet/api/microsoft.featuremanagement.featurefilters.percentagefilter?view=azure-dotnet-preview
                   //AddFeatureFilter<TargetingFilter>();       // for targeting certain audiences ..................... https://docs.microsoft.com/dotnet/api/microsoft.featuremanagement.featurefilters.targetingfilter?view=azure-dotnet-preview

            services.AddTransient<WopiRequestFactory>();
            services.AddTransient<IWopiRequestFactory>(sp => sp.GetRequiredService<WopiRequestFactory>());

            services.AddTransient<WopiDiscoveryDocumentFactory>();
            services.AddTransient<IWopiDiscoveryDocumentFactory>(sp => sp.GetRequiredService<WopiDiscoveryDocumentFactory>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (bool.TryParse(Environment.GetEnvironmentVariable("USE_AZURE_APP_CONFIGURATION"), out var useAppConfig) && useAppConfig)
            {
                app.UseAzureAppConfiguration();
            }

            app.UseMiddleware<WopiMiddleware>();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                var memoryCache = app.ApplicationServices.GetRequiredService<IMemoryCache>();
                var wopiDiscoveryDocumentFactory = app.ApplicationServices.GetRequiredService<IWopiDiscoveryDocumentFactory>();

                endpoints.MapGet("/wopi/health-check", GetHealthCheckPageAsync);
                endpoints.MapGet("/wopi/collabora",  ctxt => GetCollaboraHostPageAsync(ctxt, memoryCache, wopiDiscoveryDocumentFactory));
           });
        }

        private static async Task GetHealthCheckPageAsync(HttpContext httpContext)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<!doctype html>");
            sb.AppendLine($"<html>");
            sb.AppendLine($"  <body>");
            sb.AppendLine($"    <p>");
            sb.AppendLine($"      =============================================================");
            sb.AppendLine($"      Hello from the CDS FutureNHS File Server PoC: I seem to be ok");
            sb.AppendLine($"      =============================================================");
            sb.AppendLine($"      USE_AZURE_APP_CONFIGURATION = {Environment.GetEnvironmentVariable("USE_AZURE_APP_CONFIGURATION")}");
            sb.AppendLine($"    </p>");
            sb.AppendLine($"  </body>");
            sb.AppendLine($"</html>");

            await httpContext.Response.WriteAsync(sb.ToString());

            httpContext.Response.StatusCode = StatusCodes.Status200OK;
        }

        private static async Task GetCollaboraHostPageAsync(HttpContext httpContext, IMemoryCache memoryCache, IWopiDiscoveryDocumentFactory wopiDiscoveryDocumentFactory)
        {
            var cancellationToken = httpContext.RequestAborted;

            var wopiDiscoveryDocument = await wopiDiscoveryDocumentFactory.CreateDocumentAsync(cancellationToken);

            if (wopiDiscoveryDocument is null) return;  // TODO - Return appropriate status code to caller

            var fileId = httpContext.Request.Query["file_id"].FirstOrDefault()?.Trim();

            if (string.IsNullOrWhiteSpace(fileId)) return;  // TODO - Return appropriate status code to caller

            //var wopiServerFileEndpoint = new Uri("https://futurenhs.cds.co.uk/gateway/wopi/host/files/" + fileId, UriKind.Absolute);
            var wopiServerFileEndpoint = new Uri("http://host.docker.internal:44355/wopi/files/" + fileId, UriKind.Absolute);

            var fileExtension = Path.GetExtension(fileId);

            if (string.IsNullOrWhiteSpace(fileExtension)) return;  // TODO - Return appropriate status code to caller

            var fileAction = "view"; // edit | view | etc (see comments in discoveryDoc.GetEndpointForAsync)

            var collaboraOnlineEndpoint = await wopiDiscoveryDocument.GetEndpointForFileExtensionAsync(fileExtension, fileAction, wopiServerFileEndpoint);

            if (string.IsNullOrWhiteSpace(collaboraOnlineEndpoint)) return;  // TODO - Return appropriate status code to caller

            var sb = new StringBuilder();

            // https://wopi.readthedocs.io/projects/wopirest/en/latest/concepts.html#term-wopisrc

            // https://127.0.0.1:9980/loleaflet/4aa2794/loleaflet.html? is pulled out of the discovery xml file hosted by Collabora
            // https://127.0.0.1:44355/wopi/files/<FILE_ID> is the url Collabora uses to callback to us to get the file information and contents

            // TODO - Generate a token with a set TTL that is specific to the current user and file combination

            var accessToken = Guid.NewGuid().ToString().Replace("-", string.Empty);

            sb.AppendLine($"<!doctype html>");
            sb.AppendLine($"<html>");
            sb.AppendLine($"  <body>");
            sb.AppendLine($"    <form action=\"{collaboraOnlineEndpoint}\" enctype =\"multipart/form-data\" method=\"post\">");
            sb.AppendLine($"      <input name=\"access_token\" value=\"{ accessToken }\" type=\"hidden\">");
            sb.AppendLine($"      <input type=\"submit\" value=\"Load Document (Collabora)\">");
            sb.AppendLine($"    </form>");
            sb.AppendLine($"  </body>");
            sb.AppendLine($"</html>");

            await httpContext.Response.WriteAsync(sb.ToString());

            httpContext.Response.StatusCode = StatusCodes.Status200OK;
        }
    }
}
