namespace Shop_ProjForWeb.Core.Application.DTOs;

using Shop_ProjForWeb.Core.Domain.Enums;

public class OrderDetailDto
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal TotalPrice { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}
