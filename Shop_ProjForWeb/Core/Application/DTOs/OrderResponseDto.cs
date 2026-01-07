namespace Shop_ProjForWeb.Core.Application.DTOs;

using Shop_ProjForWeb.Core.Domain.Enums;

public class OrderResponseDto
{
    public Guid OrderId { get; set; }
    public decimal TotalPrice { get; set; }
    public OrderStatus Status { get; set; }
    
    // Updated user VIP data after order
    public UserVipDataDto? UpdatedUserData { get; set; }
}

public class UserVipDataDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsVip { get; set; }
    public int VipTier { get; set; }
    public decimal TotalSpending { get; set; }
}
