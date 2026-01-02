namespace Shop_ProjForWeb.Core.Application.Services;

using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Enums;
using Shop_ProjForWeb.Core.Domain.Exceptions;
using Shop_ProjForWeb.Core.Domain.Interfaces;

public class OrderCancellationService(
    IUnitOfWork unitOfWork,
    IInventoryService inventoryService,
    VipUpgradeService vipUpgradeService,
    IOrderStateMachine orderStateMachine) : IOrderCancellationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IInventoryService _inventoryService = inventoryService;
    private readonly VipUpgradeService _vipUpgradeService = vipUpgradeService;
    private readonly IOrderStateMachine _orderStateMachine = orderStateMachine;

    public async Task CancelOrderAsync(Guid orderId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var order = await _unitOfWork.Orders.GetOrderWithItemsAsync(orderId);
            if (order == null)
            {
                throw new OrderNotFoundException($"Order not found with id {orderId}");
            }

            // Validate cancellation using state machine
            if (!_orderStateMachine.CanBeCancelled(order.Status))
            {
                var validTransitions = string.Join(", ", _orderStateMachine.GetValidTransitions(order.Status));
                throw new InvalidOperationException(
                    $"Cannot cancel order with status '{order.Status}' ({_orderStateMachine.GetStatusDescription(order.Status)}). " +
                    $"Valid transitions: {(validTransitions.Length > 0 ? validTransitions : "None (terminal state)")}");
            }

            // Store original status for audit and VIP recalculation logic
            var originalStatus = order.Status;
            var originalTotal = order.TotalPrice;

            // Restore inventory for all items
            var restoredItems = new List<(Guid ProductId, int Quantity, string ProductName)>();
            foreach (var item in order.OrderItems)
            {
                try
                {
                    await _inventoryService.ReleaseStockAsync(item.ProductId, item.Quantity);
                    restoredItems.Add((item.ProductId, item.Quantity, item.Product?.Name ?? "Unknown"));
                }
                catch (Exception ex)
                {
                    // Log the error but continue with cancellation
                    Console.WriteLine($"Warning: Failed to restore inventory for product {item.ProductId}: {ex.Message}");
                    // In a production system, you might want to create a compensation record
                }
            }

            // Clear PaidAt when cancelling a paid order (must be done before status change to pass validation)
            if (originalStatus == OrderStatus.Paid)
            {
                order.PaidAt = null;
                order.PaymentStatus = PaymentStatus.Pending;
            }
            
            // Use state machine to change status
            order.ChangeStatus(OrderStatus.Cancelled, _orderStateMachine);
            await _unitOfWork.Orders.UpdateAsync(order);

            // Recalculate VIP status if this was a paid order (affects total paid amount)
            if (originalStatus == OrderStatus.Paid)
            {
                try
                {
                    await _vipUpgradeService.RecalculateVipStatusAfterCancellation(order.UserId);
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the cancellation
                    Console.WriteLine($"Warning: Failed to recalculate VIP status for user {order.UserId}: {ex.Message}");
                }
            }

            Console.WriteLine($"Order {orderId} cancelled successfully. Restored inventory for products: " +
                            string.Join(", ", restoredItems.Select(r => $"{r.ProductName} ({r.Quantity})")));
        });
    }
}
