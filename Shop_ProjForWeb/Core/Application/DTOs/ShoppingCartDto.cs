namespace Shop_ProjForWeb.Core.Application.DTOs;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO for creating a new cart item
/// </summary>
public class AddCartItemDto
{
    [Required]
    public Guid ProductId { get; set; }
    
    [Required]
    [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000")]
    public int Quantity { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for updating cart item quantity
/// </summary>
public class UpdateCartItemDto
{
    [Required]
    public Guid ItemId { get; set; }
    
    [Required]
    [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000")]
    public int Quantity { get; set; }
}

/// <summary>
/// DTO for cart item response
/// </summary>
public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal OriginalUnitPrice { get; set; }
    public int ProductDiscountPercent { get; set; }
    public decimal ProductDiscountAmount { get; set; }
    public decimal FinalUnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public DateTime AddedAt { get; set; }
}

/// <summary>
/// DTO for cart response
/// </summary>
public class CartDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public int VipDiscountPercent { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for applying coupon to cart
/// </summary>
public class ApplyCouponDto
{
    [Required]
    [MaxLength(50)]
    public string CouponCode { get; set; } = string.Empty;
}

/// <summary>
/// DTO for coupon response
/// </summary>
public class CouponResponseDto
{
    public string CouponCode { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public bool IsValid { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// DTO for merge carts request
/// </summary>
public class MergeCartsDto
{
    [Required]
    public Guid SourceUserId { get; set; }
    
    [Required]
    public Guid TargetUserId { get; set; }
}

/// <summary>
/// DTO for cart summary (mini cart)
/// </summary>
public class CartSummaryDto
{
    public int TotalItems { get; set; }
    public decimal TotalPrice { get; set; }
    public int TotalItemsCount => ItemsCount;
    
    [JsonIgnore]
    public int ItemsCount { get; set; }
    public List<CartItemSummaryDto> Items { get; set; } = new();
}

/// <summary>
/// DTO for cart item summary
/// </summary>
public class CartItemSummaryDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string? ProductImageUrl { get; set; }
}

/// <summary>
/// DTO for move items from cart to order
/// </summary>
public class MoveToOrderDto
{
    [Required]
    public Guid CartId { get; set; }
    
    public List<Guid>? SpecificItemIds { get; set; }
}
