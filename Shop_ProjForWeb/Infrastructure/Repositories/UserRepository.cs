namespace Shop_ProjForWeb.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Infrastructure.Persistent.DbContext;

public class UserRepository(SupermarketDbContext context) : IUserRepository
{
    private readonly SupermarketDbContext _context = context;

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            user.SoftDelete();
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<User>> GetAllIncludingDeletedAsync()
    {
        return await _context.Users.IgnoreQueryFilters().ToListAsync();
    }

    public async Task<User?> GetByIdIncludingDeletedAsync(Guid id)
    {
        return await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task RestoreAsync(Guid id)
    {
        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
        if (user != null && user.IsDeleted)
        {
            user.IsDeleted = false;
            user.DeletedAt = null;
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
