﻿using System;

namespace FutureNHS.WOPIHost.Configuration
{
    public sealed class AzurePlatformConfiguration
    {
        public AzureBlobStorageConfiguration? AzureBlobStorage { get; set; }
        public AzureAppConfiguration? AzureAppConfiguration { get; set; }
    }

    public sealed class AzureBlobStorageConfiguration
    {
        public Uri? PrimaryServiceUrl { get; set; }
        public Uri? GeoRedundantServiceUrl { get; set; }
        public string? ContainerName { get; set; }
    }

    public sealed class AzureAppConfiguration
    {
        public int? CacheExpirationIntervalInSeconds { get; set; }

        public Uri? PrimaryServiceUrl { get; set; }
        public Uri? GeoRedundantServiceUrl { get; set; }
    }
}
