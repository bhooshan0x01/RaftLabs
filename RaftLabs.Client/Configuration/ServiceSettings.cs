namespace RaftLabs.Client.Configuration;

public class ServiceSettings
{
    public const int DefaultCacheExpirationMinutes = 5;
    public const int DefaultRetryCount = 3;
    public const string AllUsersCacheKey = "all_users";
    public const string UserCacheKeyFormat = "user_{0}";
} 