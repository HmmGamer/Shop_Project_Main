namespace Shop_ProjForWeb.Core.Domain.Entities;

public class VipStatusHistory : BaseEntity
{
    public Guid UserId { get; set; }
    public int PreviousTier { get; set; }
    public int NewTier { get; set; }
    public decimal TriggeringOrderTotal { get; set; }
    public decimal TotalSpendingAtUpgrade { get; set; }
    public string Reason { get; set; } = string.Empty;
    
    // Navigation
    public User User { get; set; } = null!;

    public override void ValidateEntity()
    {
        base.ValidateEntity();
        
        if (UserId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty");
        
        // Validate tier values are within valid range (0-3)
        ValidateIntProperty(PreviousTier, nameof(PreviousTier), minValue: 0, maxValue: 3);
        ValidateIntProperty(NewTier, nameof(NewTier), minValue: 0, maxValue: 3);
        ValidateDecimalProperty(TriggeringOrderTotal, nameof(TriggeringOrderTotal), minValue: 0);
        ValidateDecimalProperty(TotalSpendingAtUpgrade, nameof(TotalSpendingAtUpgrade), minValue: 0);
        ValidateStringProperty(Reason, nameof(Reason), minLength: 1, maxLength: 500);
        
        // Tier must change for a valid history entry
        if (PreviousTier == NewTier)
            throw new InvalidOperationException("VIP tier must change for a valid status history entry");
        
        // Multi-tier jumps are now supported (e.g., 0 → 3 when spending jumps to 30000+)
        // Tier downgrades are now supported (e.g., 2 → 1 when spending decreases)
    }
}
