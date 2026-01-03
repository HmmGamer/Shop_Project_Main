namespace Shop_ProjForWeb.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Infrastructure.Persistent.DbContext;

public class InventoryRepository(SupermarketDbContext context) : IInventoryRepository
{
    private readonly SupermarketDbContext _context = context;

    public async Task<Inventory?> GetByProductIdAsync(Guid productId)
    {
        return await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
    }

    public async Task UpdateAsync(Inventory inventory)
    {
        _context.Inventories.Update(inventory);
        await _context.SaveChangesAsync();
    }

    public async Task AddAsync(Inventory inventory)
    {
        await _context.Inventories.AddAsync(inventory);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var inventory = await _context.Inventories.FindAsync(id);
        if (inventory != null)
        {
            inventory.SoftDelete();
            _context.Inventories.Update(inventory);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Inventory>> GetAllAsync()
    {
        return await _context.Inventories
            .Include(i => i.Product)
            .ToListAsync();
    }

    public async Task<List<Inventory>> GetLowStockItemsAsync(int threshold = 10)
    {
        return await _context.Inventories
            .Where(i => i.Quantity < i.LowStockThreshold)
            .Include(i => i.Product)
            .ToListAsync();
    }

    public async Task<bool> CheckStockAvailabilityAsync(Guid productId, int quantity)
    {
        var inventory = await _context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == productId);
        
        return inventory != null && inventory.Quantity >= quantity;
    }
}
