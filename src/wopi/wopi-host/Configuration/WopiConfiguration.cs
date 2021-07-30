namespace FutureNHS.WOPIHost.Configuration
{
    public sealed class WopiConfiguration
    {
        public WopiDiscoveryConfiguration DiscoveryDocumentSource { get; set; }
    }

    public sealed class WopiDiscoveryConfiguration
    {
        public string Scheme { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Path { get; set; }
    }
}
