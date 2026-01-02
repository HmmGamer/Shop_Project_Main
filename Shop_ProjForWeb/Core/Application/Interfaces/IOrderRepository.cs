namespace Shop_ProjForWeb.Core.Application.Interfaces;

using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Core.Domain.Enums;

public interface IOrderRepository
{
    Task AddAsync(Order order);
    Task<Order?> GetByIdAsync(Guid id);
    Task UpdateAsync(Order order);
    Task<decimal> GetTotalPaidAmountForUserAsync(Guid userId);
    Task AddOrderItemsAsync(List<OrderItem> items);
    Task<Order?> GetOrderWithItemsAsync(Guid id);
    Task<List<Order>> GetAllOrdersAsync();
    Task<List<Order>> GetUserOrdersAsync(Guid userId);
    Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status);
    Task DeleteAsync(Guid id);
    Task<List<Order>> GetAllIncludingDeletedAsync();
    Task<Order?> GetByIdIncludingDeletedAsync(Guid id);
    Task RestoreAsync(Guid id);
}
