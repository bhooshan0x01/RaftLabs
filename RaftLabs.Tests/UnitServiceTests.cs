using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using RaftLabs.Client.Configuration;
using RaftLabs.Client.Dtos;
using RaftLabs.Client.Exceptions;
using RaftLabs.Client.Services;

namespace RaftLabs.Tests;

public class UnitServiceTests
{
    private static HttpClient CreateMockHttpClient(Func<int, HttpResponseMessage> responseFactory)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        int callCount = 0;

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => responseFactory(callCount++));

        return new HttpClient(handlerMock.Object);
    }

    private static IOptions<UserServiceOptions> CreateOptions(string baseUrl = "https://reqres.in/api")
    {
        var options = new UserServiceOptions
        {
            BaseUrl = baseUrl,
            CacheExpirationMinutes = 5,
            RetryCount = 3
        };
        return Options.Create(options);
    }

    [Fact]
    public async Task GetUserByIdAsync_Should_Throw_On_NotFound()
    {
        var client = CreateMockHttpClient(_ => new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = Mock.Of<ILogger<UserService>>();
        var options = CreateOptions();
        var service = new UserService(client, options, cache, logger);

        await Assert.ThrowsAsync<UserServiceException>(() => service.GetUserByIdAsync(999));
    }

    [Fact]
    public async Task GetUserByIdAsync_Should_Return_User_On_Success()
    {
        var json = JsonConvert.SerializeObject(new
        {
            data = new
            {
                id = 1,
                email = "test@reqres.in",
                first_name = "Test",
                last_name = "User",
                avatar = "avatar.png"
            }
        });

        var client = CreateMockHttpClient(_ => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json)
        });

        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = Mock.Of<ILogger<UserService>>();
        var options = CreateOptions();
        var service = new UserService(client, options, cache, logger);

        var result = await service.GetUserByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("test@reqres.in", result.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_Should_Return_From_Cache()
    {
        var user = new UserDto(1, "cached@reqres.in", "Cache", "Hit", "avatar.png");
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cacheKey = string.Format(ServiceSettings.UserCacheKeyFormat, 1);
        cache.Set(cacheKey, user);

        var client = CreateMockHttpClient(_ => new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
        var logger = Mock.Of<ILogger<UserService>>();
        var options = CreateOptions();
        var service = new UserService(client, options, cache, logger);

        var result = await service.GetUserByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("cached@reqres.in", result.Email);
    }

    [Fact]
    public async Task GetAllUsersAsync_Should_Throw_On_Failure()
    {
        var client = CreateMockHttpClient(_ => new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = Mock.Of<ILogger<UserService>>();
        var options = CreateOptions();
        var service = new UserService(client, options, cache, logger);

        await Assert.ThrowsAsync<UserServiceException>(() => service.GetAllUsersAsync());
    }

    [Fact]
    public async Task GetAllUsersAsync_Should_Return_All_Users()
    {
        var json = JsonConvert.SerializeObject(new
        {
            page = 1,
            total_pages = 1,
            data = new[]
            {
                new { id = 1, email = "a@b.com", first_name = "A", last_name = "B", avatar = "a.png" },
                new { id = 2, email = "c@d.com", first_name = "C", last_name = "D", avatar = "c.png" }
            }
        });

        var client = CreateMockHttpClient(_ => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json)
        });

        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = Mock.Of<ILogger<UserService>>();
        var options = CreateOptions();
        var service = new UserService(client, options, cache, logger);

        var users = (await service.GetAllUsersAsync() ?? []).ToList();

        Assert.Equal(2, users.Count);
        Assert.Equal("a@b.com", users[0].Email);
    }

    [Fact]
    public async Task GetAllUsersAsync_Should_Return_From_Cache()
    {
        var users = new List<UserDto>
        {
            new(1, "cache@a.com", "Cache", "One", "1.png")
        };

        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set(ServiceSettings.AllUsersCacheKey, users);

        var client = CreateMockHttpClient(_ => new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
        var logger = Mock.Of<ILogger<UserService>>();
        var options = CreateOptions();
        var service = new UserService(client, options, cache, logger);

        var result = (await service.GetAllUsersAsync() ?? []).ToList();

        Assert.Single(result);
        Assert.Equal("cache@a.com", result[0].Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_Should_Use_PollyRetry_On_TransientFailure()
    {
        var json = JsonConvert.SerializeObject(new
        {
            data = new
            {
                id = 42,
                email = "retry@reqres.in",
                first_name = "Retry",
                last_name = "Success",
                avatar = "avatar.png"
            }
        });

        var callCount = 0;
        var client = CreateMockHttpClient(_ =>
        {
            callCount++;
            if (callCount <= 2)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.RequestTimeout,
                    Content = new StringContent("Request timeout")
                };
            }

            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            };
        });

        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = Mock.Of<ILogger<UserService>>();
        var options = CreateOptions();
        var service = new UserService(client, options, cache, logger);

        var result = await service.GetUserByIdAsync(42);

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("retry@reqres.in", result.Email);
        Assert.Equal(3, callCount); // Verify that the request was retried twice
    }
}