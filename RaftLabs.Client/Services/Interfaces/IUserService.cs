using RaftLabs.Client.Dtos;

namespace RaftLabs.Client.Services.Interfaces;

public interface IUserService
{

    public Task<IEnumerable<UserDto>?> GetAllUsersAsync();
    
    public Task<UserDto?> GetUserByIdAsync(int userId);
}