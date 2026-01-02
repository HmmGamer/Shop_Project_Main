namespace Shop_ProjForWeb.Core.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public class CreateOrderItemDto
{
    [Required(ErrorMessage = "ProductId is required")]
    public Guid ProductId { get; set; }
    
    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }
}
