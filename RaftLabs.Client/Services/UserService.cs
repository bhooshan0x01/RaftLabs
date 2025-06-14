using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RaftLabs.Client.Configuration;
using RaftLabs.Client.Dtos;
using RaftLabs.Client.Exceptions;
using RaftLabs.Client.Services.Interfaces;

namespace RaftLabs.Client.Services;

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    private readonly UserServiceOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public UserService(
        HttpClient httpClient,
        IOptions<UserServiceOptions> options,
        IMemoryCache cache,
        ILogger<UserService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                _options.RetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
    
    public async Task<IEnumerable<UserDto>?> GetAllUsersAsync()
    {
        if (_cache.TryGetValue(ServiceSettings.AllUsersCacheKey, out IEnumerable<UserDto>? cachedUsers))
        {
            _logger.LogInformation("Cache hit for GetAllUsersAsync");
            return cachedUsers;
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            _logger.LogInformation("Fetching all users from API");
            var users = new List<UserDto>();
            int page = 1;
            int totalPages;

            do
            {
                var response = await _httpClient.GetAsync($"{_options.BaseUrl}/users?page={page}");
                if (response.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    throw new TimeoutException($"Request timed out while fetching users on page {page}");
                }
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Could not fetch users on page {Page}: {StatusCode}", page, response.StatusCode);
                    throw new UserServiceException($"Failed to fetch users on page {page}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var paginated = JsonConvert.DeserializeObject<PaginatedUserDto>(content);

                if (paginated?.Data == null)
                {
                    _logger.LogError("Failed to deserialize paginated user data");
                    throw new UserServiceException("Failed to deserialize paginated user data");
                }

                users.AddRange(paginated.Data);
                totalPages = paginated.TotalPage;
                page++;
            } while (page <= totalPages);

            _cache.Set(
                ServiceSettings.AllUsersCacheKey,
                users,
                TimeSpan.FromMinutes(_options.CacheExpirationMinutes));

            return users;
        });
    }
    
    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be greater than zero", nameof(userId));
        }

        var cacheKey = string.Format(ServiceSettings.UserCacheKeyFormat, userId);
        if (_cache.TryGetValue(cacheKey, out UserDto? cachedUser))
        {
            _logger.LogInformation("Cache hit for user {UserId}", userId);
            return cachedUser;
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            _logger.LogInformation("Fetching user {UserId} from API", userId);
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUrl}/users/{userId}");
            request.Headers.Add("x-api-key", "reqres-free-v1");
            
            var response = await _httpClient.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.RequestTimeout)
            {
                throw new TimeoutException($"Request timed out while fetching user {userId}");
            }
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch user {UserId}: {StatusCode}", userId, response.StatusCode);
                throw new UserServiceException($"Failed to fetch user {userId}: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var root = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            var user = JsonConvert.DeserializeObject<UserDto>(root?["data"]?.ToString() ?? "")
                      ?? throw new UserServiceException($"User deserialization returned null for ID {userId}");

            _cache.Set(
                cacheKey,
                user,
                TimeSpan.FromMinutes(_options.CacheExpirationMinutes));

            return user;
        });
    }
}