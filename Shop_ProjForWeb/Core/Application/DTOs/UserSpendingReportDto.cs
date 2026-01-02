namespace Shop_ProjForWeb.Core.Application.DTOs;

public class UserSpendingReportDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool IsVip { get; set; }
    public int TotalOrders { get; set; }
    public int PaidOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageOrderValue { get; set; }
}
