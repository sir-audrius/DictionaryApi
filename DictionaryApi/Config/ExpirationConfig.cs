namespace DictionaryApi.Config
{
    public class ExpirationConfig
    {
        public int MaxExpirationInSeconds { get; set; }
        public int DefaultExpirationInSeconds { get; set; }
        public int CleanupPeriodInSeconds { get; set; }
    }
}