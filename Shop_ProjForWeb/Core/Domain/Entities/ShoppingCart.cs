namespace Shop_ProjForWeb.Core.Domain.Entities;

using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents a user's shopping cart
/// </summary>
public class ShoppingCart : BaseEntity
{
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Cart status
    /// </summary>
    public CartStatus Status { get; set; } = CartStatus.Active;
    
    /// <summary>
    /// Total items count
    /// </summary>
    public int TotalItems { get; set; }
    
    /// <summary>
    /// Total price before discounts
    /// </summary>
    public decimal Subtotal { get; set; }
    
    /// <summary>
    /// Total discount amount
    /// </summary>
    public decimal DiscountAmount { get; set; }
    
    /// <summary>
    /// Total price after discounts
    /// </summary>
    public decimal TotalPrice { get; set; }
    
    /// <summary>
    /// VIP discount percentage applied
    /// </summary>
    public int VipDiscountPercent { get; set; }
    
    // Navigation Properties
    public User? User { get; set; }
    public ICollection<ShoppingCartItem> Items { get; set; } = new List<ShoppingCartItem>();

    public override void ValidateEntity()
    {
        base.ValidateEntity();
        
        if (UserId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty");
        
        ValidateDecimalProperty(Subtotal, nameof(Subtotal), minValue: 0);
        ValidateDecimalProperty(DiscountAmount, nameof(DiscountAmount), minValue: 0);
        ValidateDecimalProperty(TotalPrice, nameof(TotalPrice), minValue: 0);
        ValidateIntProperty(VipDiscountPercent, nameof(VipDiscountPercent), minValue: 0, maxValue: 100);
    }

    /// <summary>
    /// Recalculates cart totals
    /// </summary>
    public void RecalculateTotals()
    {
        TotalItems = Items.Sum(i => i.Quantity);
        Subtotal = Items.Sum(i => i.OriginalUnitPrice * i.Quantity);
        
        var productDiscount = Items.Sum(i => i.ProductDiscountAmount);
        var vipDiscount = Subtotal * VipDiscountPercent / 100m;
        
        DiscountAmount = productDiscount + vipDiscount;
        TotalPrice = Math.Max(0, Subtotal - DiscountAmount);
    }
}

/// <summary>
/// Represents an item in the shopping cart
/// </summary>
public class ShoppingCartItem : BaseEntity
{
    public Guid ShoppingCartId { get; set; }
    public Guid ProductId { get; set; }
    
    /// <summary>
    /// Product name at time of adding to cart
    /// </summary>
    public string ProductName { get; set; } = string.Empty;
    
    /// <summary>
    /// Product image URL at time of adding
    /// </summary>
    public string? ProductImageUrl { get; set; }
    
    /// <summary>
    /// Quantity of the product
    /// </summary>
    public int Quantity { get; set; }
    
    /// <summary>
    /// Original unit price (without discounts)
    /// </summary>
    public decimal OriginalUnitPrice { get; set; }
    
    /// <summary>
    /// Discount percentage on product
    /// </summary>
    public int ProductDiscountPercent { get; set; }
    
    /// <summary>
    /// Discount amount for this product
    /// </summary>
    public decimal ProductDiscountAmount { get; set; }
    
    /// <summary>
    /// Final unit price (after discounts)
    /// </summary>
    public decimal FinalUnitPrice { get; set; }
    
    /// <summary>
    /// Total for this item
    /// </summary>
    public decimal TotalPrice { get; set; }
    
    /// <summary>
    /// Notes or special instructions
    /// </summary>
    public string? Notes { get; set; }

    // Navigation Properties
    public ShoppingCart? ShoppingCart { get; set; }
    public Product? Product { get; set; }

    public override void ValidateEntity()
    {
        base.ValidateEntity();
        
        if (ShoppingCartId == Guid.Empty)
            throw new ArgumentException("ShoppingCartId cannot be empty");
        
        if (ProductId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty");
        
        ValidateIntProperty(Quantity, nameof(Quantity), minValue: 1, maxValue: 1000);
        ValidateDecimalProperty(OriginalUnitPrice, nameof(OriginalUnitPrice), minValue: 0);
        ValidateDecimalProperty(FinalUnitPrice, nameof(FinalUnitPrice), minValue: 0);
        ValidateDecimalProperty(TotalPrice, nameof(TotalPrice), minValue: 0);
    }

    /// <summary>
    /// Calculates item totals
    /// </summary>
    public void CalculateTotals()
    {
        ProductDiscountAmount = OriginalUnitPrice * ProductDiscountPercent / 100m;
        FinalUnitPrice = Math.Max(0, OriginalUnitPrice - ProductDiscountAmount);
        TotalPrice = FinalUnitPrice * Quantity;
    }
}

/// <summary>
/// Cart status enum
/// </summary>
public enum CartStatus
{
    Active = 0,
    ConvertedToOrder = 1,
    Abandoned = 2,
    Expired = 3
}
