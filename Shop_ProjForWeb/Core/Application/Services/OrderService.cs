namespace Shop_ProjForWeb.Core.Application.Services;

using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Core.Domain.Enums;
using Shop_ProjForWeb.Core.Domain.Exceptions;
using Shop_ProjForWeb.Core.Domain.Interfaces;

public class OrderService(
    IUnitOfWork unitOfWork,
    PricingService pricingService,
    IInventoryService inventoryService,
    VipUpgradeService vipUpgradeService,
    IOrderStateMachine orderStateMachine) : IOrderService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly PricingService _pricingService = pricingService;
    private readonly IInventoryService _inventoryService = inventoryService;
    private readonly VipUpgradeService _vipUpgradeService = vipUpgradeService;
    private readonly IOrderStateMachine _orderStateMachine = orderStateMachine;

    public async Task<OrderResponseDto> CreateOrderAsync(Guid userId, List<CreateOrderItemDto> items)
    {
        if (items == null || items.Count == 0)
        {
            throw new InvalidOperationException("Order must contain at least one item");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new UserNotFoundException($"User not found with id {userId}");
        }

        // Validate products exist before transaction
        var products = new Dictionary<Guid, Product>();
        foreach (var item in items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
            if (product == null)
            {
                throw new ProductNotFoundException($"Product not found with id {item.ProductId}");
            }
            products[item.ProductId] = product;
        }

        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.Created,
            TotalPrice = 0
        };

        decimal totalPrice = 0;
        var orderItems = new List<OrderItem>();
        var reservations = new List<(Guid ProductId, int Quantity)>();

        // Use UnitOfWork transaction for atomicity
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            try
            {
                // Step 1: Reserve stock for all items first
                foreach (var item in items)
                {
                    var reserved = await _inventoryService.ReserveStockAsync(item.ProductId, item.Quantity);
                    if (!reserved)
                    {
                        throw new InsufficientStockException($"Insufficient stock for product {item.ProductId}");
                    }
                    reservations.Add((item.ProductId, item.Quantity));
                }

                // Step 2: Calculate prices and create order items
                foreach (var item in items)
                {
                    var product = products[item.ProductId];
                    // Use tier-based pricing for accurate VIP discounts
                    var (unitPrice, productDiscount, vipDiscount) = _pricingService.CalculateFinalPriceWithDiscounts(
                        product.BasePrice, product.DiscountPercent, user.VipTier);

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        UnitPrice = unitPrice,
                        Quantity = item.Quantity,
                        ProductDiscountPercent = productDiscount,
                        VipDiscountPercent = vipDiscount
                    };

                    orderItems.Add(orderItem);
                    totalPrice += unitPrice * item.Quantity;
                }

                order.TotalPrice = totalPrice;

                // Step 3: Save order and order items
                await _unitOfWork.Orders.AddAsync(order);
                await _unitOfWork.OrderItems.AddRangeAsync(orderItems);
                await _unitOfWork.SaveChangesAsync(); // Save changes to persist order and items

                // Step 4: Commit reservations (convert to actual stock decrease)
                foreach (var (productId, quantity) in reservations)
                {
                    await _inventoryService.CommitReservationAsync(productId, quantity);
                }

                return new OrderResponseDto
                {
                    OrderId = order.Id,
                    TotalPrice = order.TotalPrice,
                    Status = order.Status,
                    UpdatedUserData = new UserVipDataDto
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        IsVip = user.IsVip,
                        VipTier = user.VipTier,
                        TotalSpending = user.TotalSpending
                    }
                };
            }
            catch
            {
                // Release all reservations on failure
                foreach (var (productId, quantity) in reservations)
                {
                    try
                    {
                        await _inventoryService.ReleaseStockAsync(productId, quantity);
                    }
                    catch
                    {
                        // Log but don't throw - we're already in error handling
                        Console.WriteLine($"Failed to release reservation for product {productId}");
                    }
                }
                throw;
            }
        });
    }

    public async Task<UserVipDataDto?> PayOrderAsync(Guid orderId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new OrderNotFoundException($"Order not found with id {orderId}");
        }

        // Use state machine to validate and change status
        order.ChangeStatus(OrderStatus.Paid, _orderStateMachine);

        await _unitOfWork.Orders.UpdateAsync(order);

        // Check and upgrade VIP status with the order total
        await _vipUpgradeService.CheckAndUpgradeAsync(order.UserId, order.TotalPrice);
        
        // Save changes again to ensure VIP status is persisted
        await _unitOfWork.SaveChangesAsync();
        
        // Return updated user data
        var user = await _unitOfWork.Users.GetByIdAsync(order.UserId);
        if (user != null)
        {
            return new UserVipDataDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsVip = user.IsVip,
                VipTier = user.VipTier,
                TotalSpending = user.TotalSpending
            };
        }
        
        return null;
    }

    public async Task<OrderDetailDto> GetOrderAsync(Guid orderId)
    {
        var order = await _unitOfWork.Orders.GetOrderWithItemsAsync(orderId);
        if (order == null)
        {
            throw new OrderNotFoundException($"Order not found with id {orderId}");
        }

        return MapToOrderDetailDto(order);
    }

    public async Task<List<OrderDetailDto>> GetUserOrdersAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new UserNotFoundException($"User not found with id {userId}");
        }

        var orders = await _unitOfWork.Orders.GetUserOrdersAsync(userId);
        return orders.Select(MapToOrderDetailDto).ToList();
    }

    public async Task<List<OrderDetailDto>> GetAllOrdersAsync()
    {
        var orders = await _unitOfWork.Orders.GetAllOrdersAsync();
        return orders.Select(MapToOrderDetailDto).ToList();
    }

    public async Task<List<OrderDetailDto>> GetOrdersByStatusAsync(OrderStatus status)
    {
        var orders = await _unitOfWork.Orders.GetOrdersByStatusAsync(status);
        return orders.Select(MapToOrderDetailDto).ToList();
    }

    private static OrderDetailDto MapToOrderDetailDto(Order order)
    {
        return new OrderDetailDto
        {
            OrderId = order.Id,
            UserId = order.UserId,
            TotalPrice = order.TotalPrice,
            Status = order.Status,
            PaidAt = order.PaidAt,
            Items = order.OrderItems.Select(oi => new OrderItemDto
            {
                ProductId = oi.ProductId,
                ProductName = oi.Product?.Name ?? "Unknown",
                UnitPrice = oi.UnitPrice,
                Quantity = oi.Quantity,
                ProductDiscountPercent = oi.ProductDiscountPercent,
                VipDiscountPercent = oi.VipDiscountPercent
            }).ToList()
        };
    }
}
