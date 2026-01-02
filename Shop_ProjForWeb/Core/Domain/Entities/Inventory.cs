namespace Shop_ProjForWeb.Core.Domain.Entities;

public class Inventory : BaseEntity
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 10; // Configurable per product
    public bool LowStockFlag { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    
    // Navigation Properties
    public Product? Product { get; set; }

    public int AvailableQuantity => Quantity - ReservedQuantity;

    public override void ValidateEntity()
    {
        base.ValidateEntity();
        
        if (ProductId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty");
        
        ValidateIntProperty(Quantity, nameof(Quantity), minValue: 0);
        ValidateIntProperty(ReservedQuantity, nameof(ReservedQuantity), minValue: 0);
        ValidateIntProperty(LowStockThreshold, nameof(LowStockThreshold), minValue: 0);
        
        if (ReservedQuantity > Quantity)
            throw new InvalidOperationException("Reserved quantity cannot exceed total quantity");
        
        if (LastUpdatedAt == default)
            throw new ArgumentException("LastUpdatedAt must be set");
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive");
        if (Quantity < quantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {Quantity}, Requested: {quantity}");
        
        Quantity -= quantity;
        LastUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        UpdateLowStockFlag();
        ValidateEntity();
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive");
        
        Quantity += quantity;
        LastUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        UpdateLowStockFlag();
        ValidateEntity();
    }

    public bool CanReserve(int quantity)
    {
        if (quantity <= 0)
            return false;
        
        return AvailableQuantity >= quantity;
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive");
        if (!CanReserve(quantity))
            throw new InvalidOperationException($"Cannot reserve {quantity} items. Available: {AvailableQuantity}");
        
        ReservedQuantity += quantity;
        LastUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        UpdateLowStockFlag();
        ValidateEntity();
    }

    public void ReleaseReservation(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive");
        if (ReservedQuantity < quantity)
            throw new InvalidOperationException($"Cannot release {quantity} items. Reserved: {ReservedQuantity}");
        
        ReservedQuantity -= quantity;
        LastUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        UpdateLowStockFlag();
        ValidateEntity();
    }

    public void CommitReservation(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive");
        if (ReservedQuantity < quantity)
            throw new InvalidOperationException($"Cannot commit {quantity} items. Reserved: {ReservedQuantity}");
        
        ReservedQuantity -= quantity;
        Quantity -= quantity;
        LastUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        UpdateLowStockFlag();
        ValidateEntity();
    }

    public void UpdateLowStockFlagManually()
    {
        UpdateLowStockFlag();
        UpdatedAt = DateTime.UtcNow;
        ValidateEntity();
    }

    private void UpdateLowStockFlag()
    {
        // Use available quantity (total - reserved) for low stock calculation
        LowStockFlag = AvailableQuantity <= LowStockThreshold;
    }
}
