namespace Shop_ProjForWeb.Core.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public class CreateProductDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
    public required string Name { get; set; }
    
    [Required(ErrorMessage = "BasePrice is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "BasePrice must be greater than 0")]
    public decimal BasePrice { get; set; }
    
    [Required(ErrorMessage = "DiscountPercent is required")]
    [Range(0, 100, ErrorMessage = "DiscountPercent must be between 0 and 100")]
    public int DiscountPercent { get; set; }
    
    [Required(ErrorMessage = "IsActive is required")]
    public bool IsActive { get; set; }
    
    [Required(ErrorMessage = "InitialStock is required")]
    [Range(0, int.MaxValue, ErrorMessage = "InitialStock must be 0 or greater")]
    public int InitialStock { get; set; }
}
