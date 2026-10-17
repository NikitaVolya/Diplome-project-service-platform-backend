namespace BLL.Options
{
    public class WayForPayOptions
    {
        public const string SectionName = "WayForPay";

        public string MerchantAccount { get; set; } = string.Empty;

        public string MerchantSecretKey { get; set; } = string.Empty;

        /// <summary>Domain registered for the merchant in the WayForPay cabinet.</summary>
        public string DomainName { get; set; } = string.Empty;
    }
}
