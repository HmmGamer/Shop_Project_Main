namespace Shop_ProjForWeb.Core.Application.DTOs;

public class SalesSummaryDto
{
    public int TotalOrders { get; set; }
    public int PaidOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
