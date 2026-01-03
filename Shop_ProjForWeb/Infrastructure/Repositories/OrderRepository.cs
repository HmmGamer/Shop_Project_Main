namespace Shop_ProjForWeb.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Core.Domain.Enums;
using Shop_ProjForWeb.Infrastructure.Persistent.DbContext;

public class OrderRepository(SupermarketDbContext context) : IOrderRepository
{
    private readonly SupermarketDbContext _context = context;

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
        // Don't save here - let the caller manage the transaction
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }

    public async Task<decimal> GetTotalPaidAmountForUserAsync(Guid userId)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Paid)
            .SumAsync(o => o.TotalPrice);
    }

    public async Task AddOrderItemsAsync(List<OrderItem> items)
    {
        await _context.OrderItems.AddRangeAsync(items);
        await _context.SaveChangesAsync();
    }

    public async Task<Order?> GetOrderWithItemsAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<Order>> GetUserOrdersAsync(Guid userId)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Order>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status)
    {
        return await _context.Orders
            .Where(o => o.Status == status)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order != null)
        {
            order.SoftDelete();
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Order>> GetAllIncludingDeletedAsync()
    {
        return await _context.Orders
            .IgnoreQueryFilters()
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdIncludingDeletedAsync(Guid id)
    {
        return await _context.Orders
            .IgnoreQueryFilters()
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task RestoreAsync(Guid id)
    {
        var order = await _context.Orders.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == id);
        if (order != null && order.IsDeleted)
        {
            order.IsDeleted = false;
            order.DeletedAt = null;
            order.UpdatedAt = DateTime.UtcNow;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }
    }
}
