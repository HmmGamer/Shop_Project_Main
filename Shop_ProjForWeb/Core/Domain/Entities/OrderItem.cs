namespace Shop_ProjForWeb.Core.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int ProductDiscountPercent { get; set; }
    public int VipDiscountPercent { get; set; }
    
    // Navigation Properties
    public Order? Order { get; set; }
    public Product? Product { get; set; }

    public override void ValidateEntity()
    {
        base.ValidateEntity();
        
        if (OrderId == Guid.Empty)
            throw new ArgumentException("OrderId cannot be empty");
        
        if (ProductId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty");
        
        ValidateDecimalProperty(UnitPrice, nameof(UnitPrice), minValue: 0);
        ValidateIntProperty(Quantity, nameof(Quantity), minValue: 1);
        ValidateIntProperty(ProductDiscountPercent, nameof(ProductDiscountPercent), minValue: 0, maxValue: 100);
        ValidateIntProperty(VipDiscountPercent, nameof(VipDiscountPercent), minValue: 0, maxValue: 100);
    }
}
