namespace Shop_ProjForWeb.Core.Application.Services;

using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Core.Domain.Exceptions;

public class UserService(
    IUserRepository userRepository,
    IOrderRepository orderRepository) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IOrderRepository _orderRepository = orderRepository;

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        // Note: IsVip is now a computed property (VipTier > 0), not set directly
        var user = new User
        {
            FullName = dto.FullName,
            VipTier = 0 // New users start as non-VIP
        };

        await _userRepository.AddAsync(user);

        return MapToDto(user);
    }

    public async Task UpdateUserAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new UserNotFoundException($"User not found with id {id}");
        }

        if (!string.IsNullOrEmpty(dto.FullName))
            user.FullName = dto.FullName;

        await _userRepository.UpdateAsync(user);
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new UserNotFoundException($"User not found with id {id}");
        }

        // Check for referential integrity - users with orders cannot be deleted
        var userOrders = await _orderRepository.GetUserOrdersAsync(id);
        if (userOrders.Count > 0)
        {
            throw new InvalidOperationException($"Cannot delete user with existing orders. User has {userOrders.Count} orders. Consider soft delete instead.");
        }

        // Perform soft delete instead of hard delete for audit trail
        user.SoftDelete();
        await _userRepository.UpdateAsync(user);
    }

    public async Task<UserDto> GetUserAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new UserNotFoundException($"User not found with id {id}");
        }

        return MapToDto(user);
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapToDto).ToList();
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            IsVip = user.IsVip,
            CreatedAt = user.CreatedAt
        };
    }
}
