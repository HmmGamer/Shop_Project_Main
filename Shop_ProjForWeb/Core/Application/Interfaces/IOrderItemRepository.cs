namespace Shop_ProjForWeb.Core.Application.Interfaces;

using Shop_ProjForWeb.Core.Domain.Entities;

public interface IOrderItemRepository
{
    Task AddAsync(OrderItem item);
    Task AddRangeAsync(List<OrderItem> items);
    Task<OrderItem?> GetByIdAsync(Guid id);
    Task<List<OrderItem>> GetByOrderIdAsync(Guid orderId);
    Task UpdateAsync(OrderItem item);
    Task DeleteAsync(Guid id);
}
