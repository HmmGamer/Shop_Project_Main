namespace Shop_ProjForWeb.Core.Application.Interfaces;

using Shop_ProjForWeb.Core.Application.DTOs;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(CreateUserDto dto);
    Task UpdateUserAsync(Guid id, UpdateUserDto dto);
    Task DeleteUserAsync(Guid id);
    Task<UserDto> GetUserAsync(Guid id);
    Task<List<UserDto>> GetAllUsersAsync();
}
