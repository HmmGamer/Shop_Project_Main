namespace Shop_ProjForWeb.Core.Application.Interfaces;

using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Core.Domain.Enums;

public interface IOrderService
{
    Task<OrderResponseDto> CreateOrderAsync(Guid userId, List<CreateOrderItemDto> items);
    Task PayOrderAsync(Guid orderId);
    Task<OrderDetailDto> GetOrderAsync(Guid orderId);
    Task<List<OrderDetailDto>> GetUserOrdersAsync(Guid userId);
    Task<List<OrderDetailDto>> GetAllOrdersAsync();
    Task<List<OrderDetailDto>> GetOrdersByStatusAsync(OrderStatus status);
}
