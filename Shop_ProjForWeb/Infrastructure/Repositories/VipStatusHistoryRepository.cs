namespace Shop_ProjForWeb.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Infrastructure.Persistent.DbContext;

public class VipStatusHistoryRepository(SupermarketDbContext context) : IVipStatusHistoryRepository
{
    private readonly SupermarketDbContext _context = context;

    public async Task<VipStatusHistory?> GetByIdAsync(Guid id)
    {
        return await _context.VipStatusHistories
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<List<VipStatusHistory>> GetAllAsync()
    {
        return await _context.VipStatusHistories
            .Include(v => v.User)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<VipStatusHistory>> GetByUserIdAsync(Guid userId)
    {
        return await _context.VipStatusHistories
            .Include(v => v.User)
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(VipStatusHistory vipStatusHistory)
    {
        await _context.VipStatusHistories.AddAsync(vipStatusHistory);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(VipStatusHistory vipStatusHistory)
    {
        _context.VipStatusHistories.Update(vipStatusHistory);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var vipStatusHistory = await _context.VipStatusHistories.FindAsync(id);
        if (vipStatusHistory != null)
        {
            _context.VipStatusHistories.Remove(vipStatusHistory);
            await _context.SaveChangesAsync();
        }
    }
}