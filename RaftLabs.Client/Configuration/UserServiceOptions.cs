namespace RaftLabs.Client.Configuration;

public class UserServiceOptions
{
    public string BaseUrl { get; set; } = "https://reqres.in/api";
    
    public int CacheExpirationMinutes { get; set; } = ServiceSettings.DefaultCacheExpirationMinutes;
    
    public int RetryCount { get; set; } = ServiceSettings.DefaultRetryCount;
} 