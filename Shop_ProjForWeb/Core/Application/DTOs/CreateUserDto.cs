namespace Shop_ProjForWeb.Core.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public class CreateUserDto
{
    [Required(ErrorMessage = "FullName is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "FullName must be between 1 and 100 characters")]
    public required string FullName { get; set; }
}
