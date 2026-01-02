namespace Shop_ProjForWeb.Core.Application.Interfaces;

using Shop_ProjForWeb.Core.Domain.Entities;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task UpdateAsync(User user);
    Task AddAsync(User user);
    Task<List<User>> GetAllAsync();
    Task DeleteAsync(Guid id);
    Task<List<User>> GetAllIncludingDeletedAsync();
    Task<User?> GetByIdIncludingDeletedAsync(Guid id);
    Task RestoreAsync(Guid id);
}
