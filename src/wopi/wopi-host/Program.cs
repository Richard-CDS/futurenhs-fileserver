using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;
using System;

namespace FutureNHS.WOPIHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        // Example of how to dynamically handle changes to app config without restarting the application
        // https://docs.microsoft.com/en-us/azure/azure-app-configuration/enable-dynamic-configuration-aspnet-core?tabs=core3x 

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                    webBuilder.ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        var settings = config.Build();

                        // We will want to use the application's managed identity (when hosted in Azure) to connect to the configuration service and key vault
                        // If running locally and you don't have access to it, populate the AzureAppConfiguration-Primary and optionally the AzureAppConfiguration-Secondary
                        // configuration values and it will connect using that method instead

                        var credential = new DefaultAzureCredential();

                        // We will pull down our app configuration from the Azure Configuration Service, noting that we first pull down 
                        // all configuration without a label, and then override some/all of those setting with those labelled with the 
                        // value held in the ASPNETCORE_ENVIRONMENT variable (production, development etc).
                        // This should allow us to easily manage different config (including feature flags) as we move between environments, 
                        // but unfortunately, if you are using secrets.json and the variables isn't set to 'Development', the secrets will 
                        // not be imported, so either you can't use secrets.json or your env label has to be Development (ie not dev, prod etc)

                        // For added resilience in a multi-region configuration, we can add a secondary endpoint to retrieve configuration 
                        // from just in case the primary is not available.  Hardly ideal, but ACS doesn't support geo-failover so until it 
                        // does we have to do the best we can and try to keep settings in sync ourselves :(

                        if (bool.TryParse(Environment.GetEnvironmentVariable("USE_AZURE_APP_CONFIGURATION"), out var useAppConfig) && useAppConfig)
                        {
                            var secondaryConnectionString = settings.GetConnectionString("AzureAppConfiguration-Secondary");
                            var secondaryEndpoint = settings["AzureAppConfiguration:SecondaryEndpoint"];

                            var isMultiRegion = !string.IsNullOrWhiteSpace(secondaryConnectionString) || Uri.IsWellFormedUriString(secondaryEndpoint, UriKind.Absolute);

                            var environmentLabel = hostingContext.HostingEnvironment.EnvironmentName;

                            var cacheRefreshSchedule = TimeSpan.FromSeconds(5);

                            if (isMultiRegion)
                            {
                                config.AddAzureAppConfiguration(
                                    options =>
                                    {
                                        if (!string.IsNullOrWhiteSpace(secondaryConnectionString))
                                        {
                                            options = options.Connect(secondaryConnectionString);
                                        }
                                        else
                                        {
                                            options = options.Connect(new Uri(secondaryEndpoint, UriKind.Absolute), credential);
                                        }

                                        options.Select(keyFilter: KeyFilter.Any, labelFilter: LabelFilter.Null)
                                               .Select(keyFilter: KeyFilter.Any, labelFilter: environmentLabel)
                                               .ConfigureRefresh(refreshOptions => refreshOptions.Register("FileServer_SentinelKey", refreshAll: true))
                                               .ConfigureKeyVault(kv => kv.SetCredential(credential))
                                               .UseFeatureFlags(featureFlagOptions => featureFlagOptions.CacheExpirationInterval = cacheRefreshSchedule);
                                    },
                                    optional: true
                                    );
                            }

                            var primaryConnectionString = settings.GetConnectionString("AzureAppConfiguration-Primary");
                            var primaryEndpoint = settings["AzureAppConfiguration:PrimaryEndpoint"];

                            config.AddAzureAppConfiguration(
                                options =>
                                {
                                    if (!string.IsNullOrWhiteSpace(primaryConnectionString))
                                    {
                                        options = options.Connect(primaryConnectionString);
                                    }
                                    else if (Uri.IsWellFormedUriString(primaryEndpoint, UriKind.Absolute))
                                    {
                                        options = options.Connect(new Uri(primaryEndpoint, UriKind.Absolute), credential);
                                    }
                                    else throw new ApplicationException("If the USE_AZURE_APP_CONFIGURATION environment variable is set to true then either the ConnectionStrings:AzureAppConfiguration-Primary or the AzureAppConfiguration:PrimaryEndpoint setting must be present and well formed");

                                    options.Select(keyFilter: KeyFilter.Any, labelFilter: LabelFilter.Null)
                                           .Select(keyFilter: KeyFilter.Any, labelFilter: environmentLabel)
                                           .ConfigureRefresh(refreshOptions => refreshOptions.Register("FileServer_SentinelKey", refreshAll: true)
                                                                                             .SetCacheExpiration(cacheRefreshSchedule))
                                           .ConfigureKeyVault(kv => kv.SetCredential(credential))
                                           .UseFeatureFlags(featureFlagOptions => featureFlagOptions.CacheExpirationInterval = cacheRefreshSchedule);
                                },
                                optional: isMultiRegion
                                );
                        }
                    })
                .UseStartup<Startup>());
    }
}
