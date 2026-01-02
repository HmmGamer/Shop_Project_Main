namespace Shop_ProjForWeb.Core.Domain.Entities;

public class Product : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal BasePrice { get; set; }
    public int DiscountPercent { get; set; }
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
    
    // Navigation Properties
    public Inventory? Inventory { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = [];

    public override void ValidateEntity()
    {
        base.ValidateEntity();
        
        ValidateStringProperty(Name, nameof(Name), minLength: 1, maxLength: 200);
        
        if (!string.IsNullOrEmpty(Description))
        {
            ValidateStringProperty(Description, nameof(Description), maxLength: 1000);
        }
        
        if (!string.IsNullOrEmpty(Category))
        {
            ValidateStringProperty(Category, nameof(Category), maxLength: 100);
        }
        
        ValidateDecimalProperty(BasePrice, nameof(BasePrice), minValue: 0);
        ValidateIntProperty(DiscountPercent, nameof(DiscountPercent), minValue: 0, maxValue: 100);
        
        if (!string.IsNullOrEmpty(ImageUrl))
        {
            ValidateStringProperty(ImageUrl, nameof(ImageUrl), maxLength: 500);
            if (!IsValidUrl(ImageUrl))
                throw new ArgumentException("ImageUrl format is invalid");
        }
    }

    private static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out _);
    }
}
