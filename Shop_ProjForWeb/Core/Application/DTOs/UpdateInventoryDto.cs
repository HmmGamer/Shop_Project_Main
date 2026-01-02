namespace Shop_ProjForWeb.Core.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public class UpdateInventoryDto
{
    [Required(ErrorMessage = "Quantity is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or greater")]
    public int Quantity { get; set; }
}
