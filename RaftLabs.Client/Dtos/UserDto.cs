using Newtonsoft.Json;

namespace RaftLabs.Client.Dtos;

public sealed class UserDto
{
    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonProperty("first_name")]
    public string FirstName { get; set; }
    
    [JsonProperty("last_name")]
    public string LastName { get; set; }
    
    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("avatar")]
    public string Avatar { get; set; }

    public UserDto(int id, string email, string firstName, string lastName, string avatar)
    {
        Id = id;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Avatar = avatar;
    }
}

public sealed class PaginatedUserDto
{
    [JsonProperty("page")]
    public int Page { get; set; }
    
    [JsonProperty("total_pages")]
    public int TotalPage { get; set; }
    
    [JsonProperty("data")]
    public List<UserDto> Data { get; set; } = new();
}