using Microsoft.AspNetCore.Mvc;
using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Exceptions;

namespace Shop_ProjForWeb.Presentation.Controllers;

/// <summary>
/// Manages shopping cart operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShoppingCartController : ControllerBase
{
    private readonly IShoppingCartService _cartService;
    private readonly ILogger<ShoppingCartController> _logger;

    public ShoppingCartController(
        IShoppingCartService cartService,
        ILogger<ShoppingCartController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current user's cart
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var userId = GetCurrentUserId();
        var cart = await _cartService.GetCartAsync(userId);
        return Ok(cart);
    }

    /// <summary>
    /// Gets cart summary (mini cart)
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(CartSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartSummaryDto>> GetCartSummary()
    {
        var userId = GetCurrentUserId();
        var summary = await _cartService.GetCartSummaryAsync(userId);
        return Ok(summary);
    }

    /// <summary>
    /// Adds an item to the cart
    /// </summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartItemDto>> AddItem([FromBody] AddCartItemDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var item = await _cartService.AddItemAsync(userId, dto);
            return CreatedAtAction(nameof(GetCart), new { }, item);
        }
        catch (ProductNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an item quantity in the cart
    /// </summary>
    [HttpPut("items")]
    [ProducesResponseType(typeof(CartItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartItemDto>> UpdateItem([FromBody] UpdateCartItemDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var item = await _cartService.UpdateItemQuantityAsync(userId, dto);
            
            if (item == null)
            {
                return NoContent();
            }
            
            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Removes an item from the cart
    /// </summary>
    [HttpDelete("items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(Guid itemId)
    {
        var userId = GetCurrentUserId();
        var success = await _cartService.RemoveItemAsync(userId, itemId);
        
        if (!success)
        {
            return NotFound(new { error = "Item not found in cart" });
        }
        
        return NoContent();
    }

    /// <summary>
    /// Clears all items from the cart
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetCurrentUserId();
        await _cartService.ClearCartAsync(userId);
        return NoContent();
    }

    /// <summary>
    /// Validates cart items (stock, pricing)
    /// </summary>
    [HttpGet("validate")]
    [ProducesResponseType(typeof(CartValidationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartValidationResult>> ValidateCart()
    {
        var userId = GetCurrentUserId();
        var result = await _cartService.ValidateCartAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Merges a guest cart into the user's cart
    /// </summary>
    [HttpPost("merge")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartDto>> MergeCart([FromQuery] Guid guestUserId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var cart = await _cartService.MergeCartsAsync(guestUserId, userId);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Applies VIP discount to cart
    /// </summary>
    [HttpPost("apply-vip-discount")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartDto>> ApplyVipDiscount([FromQuery] int discountPercent)
    {
        var userId = GetCurrentUserId();
        var cart = await _cartService.ApplyVipDiscountAsync(userId, discountPercent);
        return Ok(cart);
    }

    /// <summary>
    /// Converts cart to order items (for checkout)
    /// </summary>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(List<OrderItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<OrderItemDto>>> Checkout()
    {
        try
        {
            var userId = GetCurrentUserId();
            var orderItems = await _cartService.ConvertToOrderItemsAsync(userId);
            return Ok(orderItems);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets cart item count
    /// </summary>
    [HttpGet("count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetCartCount()
    {
        var userId = GetCurrentUserId();
        var summary = await _cartService.GetCartSummaryAsync(userId);
        return Ok(new { totalItems = summary.TotalItems });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value 
            ?? User.FindFirst("userId")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("User ID not found in token");
        }
        
        return userId;
    }
}
