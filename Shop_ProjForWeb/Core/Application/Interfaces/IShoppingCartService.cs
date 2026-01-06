namespace Shop_ProjForWeb.Core.Application.Interfaces;

using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Core.Domain.Entities;

/// <summary>
/// Service interface for shopping cart operations
/// </summary>
public interface IShoppingCartService
{
    /// <summary>
    /// Gets or creates a cart for a user
    /// </summary>
    Task<CartDto> GetCartAsync(Guid userId);
    
    /// <summary>
    /// Gets cart summary (mini cart) for a user
    /// </summary>
    Task<CartSummaryDto> GetCartSummaryAsync(Guid userId);
    
    /// <summary>
    /// Adds an item to the cart
    /// </summary>
    Task<CartItemDto> AddItemAsync(Guid userId, AddCartItemDto dto);
    
    /// <summary>
    /// Updates an item quantity in the cart
    /// </summary>
    Task<CartItemDto> UpdateItemQuantityAsync(Guid userId, UpdateCartItemDto dto);
    
    /// <summary>
    /// Removes an item from the cart
    /// </summary>
    Task<bool> RemoveItemAsync(Guid userId, Guid itemId);
    
    /// <summary>
    /// Clears all items from the cart
    /// </summary>
    Task<bool> ClearCartAsync(Guid userId);
    
    /// <summary>
    /// Merges a guest cart into a user cart
    /// </summary>
    Task<CartDto> MergeCartsAsync(Guid guestUserId, Guid targetUserId);
    
    /// <summary>
    /// Applies VIP discount to cart
    /// </summary>
    Task<CartDto> ApplyVipDiscountAsync(Guid userId, int vipDiscountPercent);
    
    /// <summary>
    /// Validates cart items (stock, pricing, etc.)
    /// </summary>
    Task<CartValidationResult> ValidateCartAsync(Guid userId);
    
    /// <summary>
    /// Converts cart to order items
    /// </summary>
    Task<List<OrderItemDto>> ConvertToOrderItemsAsync(Guid userId);
    
    /// <summary>
    /// Deletes a cart permanently
    /// </summary>
    Task<bool> DeleteCartAsync(Guid cartId);
}

/// <summary>
/// Cart validation result
/// </summary>
public class CartValidationResult
{
    public bool IsValid { get; set; }
    public List<CartValidationError> Errors { get; set; } = new();
    public List<CartValidationWarning> Warnings { get; set; } = new();
}

/// <summary>
/// Validation error for cart item
/// </summary>
public class CartValidationError
{
    public Guid? ItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Validation warning for cart item
/// </summary>
public class CartValidationWarning
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string WarningType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
