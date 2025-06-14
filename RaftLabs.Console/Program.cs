using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RaftLabs.Client.Configuration;
using RaftLabs.Client.Services;
using RaftLabs.Client.Services.Interfaces;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<UserServiceOptions>(
            context.Configuration.GetSection("UserService"));
        services.AddMemoryCache();
        services.AddHttpClient<IUserService, UserService>();
    })
    .Build();

var userService = host.Services.GetRequiredService<IUserService>();

try
{
    // Test 1: Get all users (paginated)
    Console.WriteLine("Test 1: Getting all users (paginated)...");
    var users = await userService.GetAllUsersAsync();
    if (users != null)
    {
        Console.WriteLine($"Found {users.Count()} users:");
        foreach (var user in users)
        {
            Console.WriteLine($"- {user.FirstName} {user.LastName} ({user.Email})");
            Console.WriteLine($"  Avatar: {user.Avatar}");
            Console.WriteLine();
        }
    }

    // Test 2: Get user by ID
    Console.WriteLine("\nTest 2: Getting user by ID...");
    var userId = 1; 
    var specificUser = await userService.GetUserByIdAsync(userId);
    if (specificUser != null)
    {
        Console.WriteLine($"User {userId} details:");
        Console.WriteLine($"- Name: {specificUser.FirstName} {specificUser.LastName}");
        Console.WriteLine($"- Email: {specificUser.Email}");
        Console.WriteLine($"- Avatar: {specificUser.Avatar}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}