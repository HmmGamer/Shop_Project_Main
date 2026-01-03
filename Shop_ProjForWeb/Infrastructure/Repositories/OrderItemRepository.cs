namespace Shop_ProjForWeb.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Infrastructure.Persistent.DbContext;

public class OrderItemRepository(SupermarketDbContext context) : IOrderItemRepository
{
    private readonly SupermarketDbContext _context = context;

    public async Task AddAsync(OrderItem item)
    {
        await _context.OrderItems.AddAsync(item);
        // Don't save here - let the caller manage the transaction
    }

    public async Task AddRangeAsync(List<OrderItem> items)
    {
        await _context.OrderItems.AddRangeAsync(items);
        // Don't save here - let the caller manage the transaction
    }

    public async Task<OrderItem?> GetByIdAsync(Guid id)
    {
        return await _context.OrderItems
            .Include(oi => oi.Product)
            .Include(oi => oi.Order)
            .FirstOrDefaultAsync(oi => oi.Id == id);
    }

    public async Task<List<OrderItem>> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .Include(oi => oi.Product)
            .ToListAsync();
    }

    public async Task UpdateAsync(OrderItem item)
    {
        _context.OrderItems.Update(item);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _context.OrderItems.FindAsync(id);
        if (item != null)
        {
            _context.OrderItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
