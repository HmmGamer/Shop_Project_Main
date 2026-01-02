using System.ComponentModel.DataAnnotations.Schema;

namespace Shop_ProjForWeb.Core.Domain.Entities;

public class User : BaseEntity
{
    public required string FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    
    /// <summary>
    /// Computed property - not stored in DB. VIP status is derived from VipTier.
    /// </summary>
    [NotMapped]
    public bool IsVip => VipTier > 0;
    
    public decimal TotalSpending { get; set; }
    public DateTime? VipUpgradedAt { get; set; }
    public int VipTier { get; set; } = 0; // 0 = Regular, 1+ = VIP
    
    // Navigation Properties
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<VipStatusHistory> VipHistory { get; set; } = [];

    public override void ValidateEntity()
    {
        base.ValidateEntity();
        
        ValidateStringProperty(FullName, nameof(FullName), minLength: 2, maxLength: 100);
        
        if (!string.IsNullOrEmpty(Email))
        {
            ValidateStringProperty(Email, nameof(Email), maxLength: 255);
            if (!IsValidEmail(Email))
                throw new ArgumentException("Email format is invalid");
        }
        
        if (!string.IsNullOrEmpty(Phone))
        {
            ValidateStringProperty(Phone, nameof(Phone), maxLength: 20);
        }
        
        if (!string.IsNullOrEmpty(Address))
        {
            ValidateStringProperty(Address, nameof(Address), maxLength: 500);
        }
        
        ValidateDecimalProperty(TotalSpending, nameof(TotalSpending), minValue: 0);
        ValidateIntProperty(VipTier, nameof(VipTier), minValue: 0, maxValue: 10);
        
        // VIP users (tier > 0) must have an upgrade date
        if (VipTier > 0 && VipUpgradedAt == null)
            throw new InvalidOperationException("VIP users must have a VipUpgradedAt date");
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
