namespace Shop_ProjForWeb.Core.Application.DTOs;

public class InventoryReportDto
{
    public int TotalItems { get; set; }
    public int TotalQuantity { get; set; }
    public int LowStockCount { get; set; }
    public List<InventoryDto> LowStockItems { get; set; } = new();
}
