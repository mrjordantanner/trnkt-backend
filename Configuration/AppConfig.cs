namespace Trnkt.Configuration
{
    public class AppConfig
    {
        public string TableName { get; set; }
        public string JwtKey { get; set; }
        public string JwtIssuer { get; set; }
        public string JwtAudience { get; set; }
    }
}