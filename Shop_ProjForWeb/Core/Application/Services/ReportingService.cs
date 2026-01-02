using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Enums;

namespace Shop_ProjForWeb.Core.Application.Services;

public class ReportingService(
    IOrderRepository orderRepository,
    IInventoryRepository inventoryRepository,
    IUserRepository userRepository)
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IInventoryRepository _inventoryRepository = inventoryRepository;
    private readonly IUserRepository _userRepository = userRepository;

    /// <summary>
    /// Get sales summary report
    /// </summary>
    public async Task<SalesSummaryDto> GetSalesSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var paidOrders = await _orderRepository.GetOrdersByStatusAsync(OrderStatus.Paid);
        
        // Filter by date range if provided
        if (startDate.HasValue)
        {
            paidOrders = paidOrders.Where(o => o.CreatedAt >= startDate.Value).ToList();
        }
        
        if (endDate.HasValue)
        {
            paidOrders = paidOrders.Where(o => o.CreatedAt <= endDate.Value).ToList();
        }

        var summary = new SalesSummaryDto
        {
            TotalOrders = paidOrders.Count,
            PaidOrders = paidOrders.Count,
            TotalRevenue = paidOrders.Sum(o => o.TotalPrice),
            AverageOrderValue = paidOrders.Count > 0 ? paidOrders.Sum(o => o.TotalPrice) / paidOrders.Count : 0,
            StartDate = startDate,
            EndDate = endDate
        };

        return summary;
    }

    /// <summary>
    /// Get inventory report
    /// </summary>
    public async Task<InventoryReportDto> GetInventoryReportAsync()
    {
        var inventories = await _inventoryRepository.GetAllAsync();
        var lowStockItems = await _inventoryRepository.GetLowStockItemsAsync();

        var report = new InventoryReportDto
        {
            TotalItems = inventories.Count,
            TotalQuantity = inventories.Sum(i => i.Quantity),
            LowStockCount = lowStockItems.Count,
            LowStockItems = lowStockItems.Select(i => new InventoryDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? "Unknown",
                Quantity = i.Quantity,
                LowStockFlag = i.LowStockFlag,
                LastUpdatedAt = i.LastUpdatedAt
            }).ToList()
        };

        return report;
    }

    /// <summary>
    /// Get user spending report
    /// </summary>
    public async Task<UserSpendingReportDto> GetUserSpendingReportAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new Exception($"User with ID {userId} not found");
        }

        var orders = await _orderRepository.GetUserOrdersAsync(userId);
        var paidOrders = orders.Where(o => o.Status == OrderStatus.Paid).ToList();

        var report = new UserSpendingReportDto
        {
            UserId = userId,
            UserName = user.FullName,
            IsVip = user.IsVip,
            TotalOrders = orders.Count,
            PaidOrders = paidOrders.Count,
            TotalSpent = paidOrders.Sum(o => o.TotalPrice),
            AverageOrderValue = paidOrders.Count > 0 ? paidOrders.Sum(o => o.TotalPrice) / paidOrders.Count : 0
        };

        return report;
    }

    /// <summary>
    /// Get top products by sales
    /// </summary>
    public async Task<List<TopProductDto>> GetTopProductsAsync(int limit = 10)
    {
        var paidOrders = await _orderRepository.GetOrdersByStatusAsync(OrderStatus.Paid);

        var topProducts = paidOrders
            .SelectMany(o => o.OrderItems)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new TopProductDto
            {
                ProductId = g.Key,
                ProductName = g.First().Product?.Name ?? "Unknown",
                TotalQuantitySold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
            })
            .OrderByDescending(p => p.TotalRevenue)
            .Take(limit)
            .ToList();

        return topProducts;
    }

    /// <summary>
    /// Get all users spending report
    /// </summary>
    public async Task<List<UserSpendingReportDto>> GetAllUsersSpendingReportAsync()
    {
        var users = await _userRepository.GetAllAsync();
        var reports = new List<UserSpendingReportDto>();

        foreach (var user in users)
        {
            var orders = await _orderRepository.GetUserOrdersAsync(user.Id);
            var paidOrders = orders.Where(o => o.Status == OrderStatus.Paid).ToList();

            var report = new UserSpendingReportDto
            {
                UserId = user.Id,
                UserName = user.FullName,
                IsVip = user.IsVip,
                TotalOrders = orders.Count,
                PaidOrders = paidOrders.Count,
                TotalSpent = paidOrders.Sum(o => o.TotalPrice),
                AverageOrderValue = paidOrders.Count > 0 ? paidOrders.Sum(o => o.TotalPrice) / paidOrders.Count : 0
            };

            reports.Add(report);
        }

        return reports.OrderByDescending(r => r.TotalSpent).ToList();
    }

    /// <summary>
    /// Get order status distribution
    /// </summary>
    public async Task<OrderStatusDistributionDto> GetOrderStatusDistributionAsync()
    {
        var createdOrders = await _orderRepository.GetOrdersByStatusAsync(OrderStatus.Created);
        var paidOrders = await _orderRepository.GetOrdersByStatusAsync(OrderStatus.Paid);
        var shippedOrders = await _orderRepository.GetOrdersByStatusAsync(OrderStatus.Shipped);
        var deliveredOrders = await _orderRepository.GetOrdersByStatusAsync(OrderStatus.Delivered);
        var cancelledOrders = await _orderRepository.GetOrdersByStatusAsync(OrderStatus.Cancelled);

        var distribution = new OrderStatusDistributionDto
        {
            CreatedCount = createdOrders.Count,
            PaidCount = paidOrders.Count,
            ShippedCount = shippedOrders.Count,
            DeliveredCount = deliveredOrders.Count,
            CancelledCount = cancelledOrders.Count,
            TotalOrders = createdOrders.Count + paidOrders.Count + shippedOrders.Count + deliveredOrders.Count + cancelledOrders.Count
        };

        return distribution;
    }
}
