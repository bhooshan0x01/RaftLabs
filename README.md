# RaftLabs User Service

A .NET library that provides a service for interacting with the reqres.in API to fetch and manage user data. The library includes features like caching, retry policies, and proper error handling.

## Features

- Fetch paginated lists of users
- Get user details by ID
- Built-in caching mechanism
- Retry policy for transient failures
- Proper error handling and logging
- Configurable through appsettings.json

## Prerequisites

- .NET 8.0 SDK or later
- Rider or Visual Studio Code

## Project Structure

```
RaftLabs/
├── RaftLabs.Client/           # Main class library
│   ├── Configuration/         # Configuration classes
│   ├── Dtos/                 # Data Transfer Objects
│   ├── Exceptions/           # Custom exceptions
│   └── Services/             # Service implementations
├── RaftLabs.Tests/           # Unit tests
└── RaftLabs.Console/         # Demo console application
```

## Getting Started

1. Clone the repository:

```bash
git clone <repository-url>
cd RaftLabs
```

2. Restore dependencies:

```bash
dotnet restore
```

3. Build the solution:

```bash
dotnet build
```

4. Run the tests:

```bash
dotnet test
```

## Configuration

The service can be configured through `appsettings.json`:

```json
{
  "UserService": {
    "BaseUrl": "https://reqres.in/api",
    "RetryCount": 3,
    "RetryDelayMilliseconds": 1000
  }
}
```

### Configuration Options

- `BaseUrl`: The base URL for the API
- `RetryCount`: Number of retry attempts for failed requests
- `RetryDelayMilliseconds`: Delay between retry attempts

## Usage

### Basic Usage

```csharp
// Configure services
services.Configure<UserServiceOptions>(
    configuration.GetSection("UserService"));
services.AddMemoryCache();
services.AddHttpClient<IUserService, UserService>();

// Get service instance
var userService = serviceProvider.GetRequiredService<IUserService>();

// Get all users
var users = await userService.GetAllUsersAsync();

// Get user by ID
var user = await userService.GetUserByIdAsync(1);
```

### Running the Demo Console Application

1. Navigate to the console project:

```bash
cd RaftLabs.Console
```

2. Run the application:

```bash
dotnet run
```

The console application demonstrates:

- Fetching all users (paginated)
- Getting a specific user by ID
- Error handling
- Caching behavior

## API Endpoints

The service interacts with the following reqres.in API endpoints:

1. Get All Users (Paginated):

```
GET https://reqres.in/api/users?page={pageNumber}
```

2. Get User by ID:

```
GET https://reqres.in/api/users/{userId}
```

Note: This endpoint requires an API key header: `x-api-key: reqres-free-v1`

## Error Handling

The service includes comprehensive error handling for:

- Network failures
- API errors
- Deserialization issues
- Invalid inputs

Errors are wrapped in `UserServiceException` with descriptive messages.

## Caching

The service implements in-memory caching for:

- All users list
- Individual user details

Cache duration is configurable through `CacheExpirationMinutes` in the configuration.

## Retry Policy

The service uses Polly to implement a retry policy for:

- Network failures
- Timeouts
- Transient errors

Retry count and delay are configurable through the configuration.
