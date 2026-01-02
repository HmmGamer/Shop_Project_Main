namespace Shop_ProjForWeb.Core.Application.DTOs;

public class OrderStatusDistributionDto
{
    public int CreatedCount { get; set; }
    public int PaidCount { get; set; }
    public int ShippedCount { get; set; }
    public int DeliveredCount { get; set; }
    public int CancelledCount { get; set; }
    public int TotalOrders { get; set; }
}
