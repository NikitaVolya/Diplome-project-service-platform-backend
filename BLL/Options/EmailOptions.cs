

namespace BLL.Options
{
    public class EmailOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string From { get; set; } = string.Empty;
    }
}
