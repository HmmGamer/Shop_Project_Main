namespace Shop_ProjForWeb.Core.Application.DTOs;

public class UpdateProductDto
{
    public string? Name { get; set; }
    public decimal? BasePrice { get; set; }
    public int? DiscountPercent { get; set; }
    public bool? IsActive { get; set; }
}
