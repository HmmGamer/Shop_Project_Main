namespace Shop_ProjForWeb.Core.Application.DTOs;

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public required string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int ProductDiscountPercent { get; set; }
    public int VipDiscountPercent { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}
