namespace Shop_ProjForWeb.Core.Application.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public decimal BasePrice { get; set; }
    public int DiscountPercent { get; set; }
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
