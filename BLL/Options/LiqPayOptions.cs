namespace BLL.Options
{
    public class LiqPayOptions
    {
        public const string SectionName = "LiqPay";

        public string PublicKey { get; set; } = string.Empty;

        public string PrivateKey { get; set; } = string.Empty;
    }
}
