namespace Shop_ProjForWeb.Presentation.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Enums;
using Shop_ProjForWeb.Core.Domain.Exceptions;

/// <summary>
/// Manages customer orders including creation, payment, and cancellation
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController(
    IOrderService orderService,
    IOrderCancellationService orderCancellationService) : ControllerBase
{
    private readonly IOrderService _orderService = orderService;
    private readonly IOrderCancellationService _orderCancellationService = orderCancellationService;

    /// <summary>
    /// Retrieves all orders with pagination and sorting
    /// </summary>
    /// <param name="request">Pagination parameters (page, pageSize, sortBy, sortDescending)</param>
    /// <returns>Paginated list of orders with details</returns>
    /// <response code="200">Returns the paginated list of orders</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedResponse<OrderDetailDto>>> GetAllOrders([FromQuery] PaginatedRequest request)
    {
        try
        {
            var orders = await _orderService.GetAllOrdersAsync();
            
            // Apply sorting
            var sortedOrders = request.SortBy.ToLower() switch
            {
                "totalprice" => request.SortDescending ? orders.OrderByDescending(o => o.TotalPrice) : orders.OrderBy(o => o.TotalPrice),
                "status" => request.SortDescending ? orders.OrderByDescending(o => o.Status) : orders.OrderBy(o => o.Status),
                "paidat" => request.SortDescending ? orders.OrderByDescending(o => o.PaidAt) : orders.OrderBy(o => o.PaidAt),
                _ => request.SortDescending ? orders.OrderByDescending(o => o.OrderId) : orders.OrderBy(o => o.OrderId)
            };

            // Apply pagination
            var totalCount = sortedOrders.Count();
            var pagedOrders = sortedOrders
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var response = new PaginatedResponse<OrderDetailDto>
            {
                Items = pagedOrders,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving orders", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a specific order by ID with full details
    /// </summary>
    /// <param name="id">The unique identifier of the order</param>
    /// <returns>Order details including items and pricing</returns>
    /// <response code="200">Returns the order details</response>
    /// <response code="404">Order not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDetailDto>> GetOrder(Guid id)
    {
        try
        {
            var order = await _orderService.GetOrderAsync(id);
            return Ok(order);
        }
        catch (OrderNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving the order", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves all orders for a specific user
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>List of user's orders</returns>
    /// <response code="200">Returns the list of user orders</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<OrderDetailDto>>> GetUserOrders(Guid userId)
    {
        try
        {
            var orders = await _orderService.GetUserOrdersAsync(userId);
            return Ok(orders);
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving user orders", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves orders filtered by status (Pending, Paid, Cancelled)
    /// </summary>
    /// <param name="status">Order status filter (0=Pending, 1=Paid, 2=Cancelled)</param>
    /// <returns>List of orders with the specified status</returns>
    /// <response code="200">Returns the list of filtered orders</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("status/{status}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<OrderDetailDto>>> GetOrdersByStatus(OrderStatus status)
    {
        try
        {
            var orders = await _orderService.GetOrdersByStatusAsync(status);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving orders by status", details = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new order with automatic pricing, discounts, and inventory management
    /// </summary>
    /// <param name="request">Order creation details (UserId and list of items with ProductId and Quantity)</param>
    /// <returns>Created order with calculated pricing and VIP discounts applied</returns>
    /// <response code="201">Order created successfully</response>
    /// <response code="400">Invalid input, insufficient stock, or validation failed</response>
    /// <response code="404">User or product not found</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/orders
    ///     {
    ///        "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///        "items": [
    ///          {
    ///            "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///            "quantity": 2
    ///          }
    ///        ]
    ///     }
    /// 
    /// </remarks>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            if (request.UserId == Guid.Empty)
            {
                return BadRequest(new { error = "User ID is required" });
            }

            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequest(new { error = "Order must contain at least one item" });
            }

            var result = await _orderService.CreateOrderAsync(request.UserId, request.Items);
            return CreatedAtAction(nameof(GetOrder), new { id = result.OrderId }, result);
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ProductNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InsufficientStockException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while creating the order", details = ex.Message });
        }
    }

    /// <summary>
    /// Processes payment for a pending order and updates user VIP status if eligible
    /// </summary>
    /// <param name="orderId">The unique identifier of the order</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Payment processed successfully</response>
    /// <response code="404">Order or user not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{orderId}/pay")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PayOrder(Guid orderId)
    {
        try
        {
            await _orderService.PayOrderAsync(orderId);
            return NoContent();
        }
        catch (OrderNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while processing payment", details = ex.Message });
        }
    }

    /// <summary>
    /// Cancels an order and restores inventory (only pending orders can be cancelled)
    /// </summary>
    /// <param name="id">The unique identifier of the order</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Order cancelled successfully</response>
    /// <response code="400">Order cannot be cancelled (already paid or cancelled)</response>
    /// <response code="404">Order not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        try
        {
            await _orderCancellationService.CancelOrderAsync(id);
            return NoContent();
        }
        catch (OrderNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while canceling the order", details = ex.Message });
        }
    }
}

public class CreateOrderRequest
{
    public Guid UserId { get; set; }
    public required List<CreateOrderItemDto> Items { get; set; }
}
