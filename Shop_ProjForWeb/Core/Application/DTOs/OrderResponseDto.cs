namespace Shop_ProjForWeb.Core.Application.DTOs;

using Shop_ProjForWeb.Core.Domain.Enums;

public class OrderResponseDto
{
    public Guid OrderId { get; set; }
    public decimal TotalPrice { get; set; }
    public OrderStatus Status { get; set; }
}
