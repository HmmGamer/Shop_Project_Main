namespace Shop_ProjForWeb.Core.Application.DTOs;

public class InventoryStatusDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool IsLowStock { get; set; }
    public int ReorderLevel { get; set; }
}