namespace ScanTextImage.Options
{
    public class AzureAd
    {
        public const string ConfigSection = "AzureAd";
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}
