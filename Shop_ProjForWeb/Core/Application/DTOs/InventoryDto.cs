namespace Shop_ProjForWeb.Core.Application.DTOs;

public class InventoryDto
{
    public Guid ProductId { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public bool LowStockFlag { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}
