using System.ComponentModel.DataAnnotations;

namespace Shop_ProjForWeb.Core.Application.DTOs;

public class PaginatedRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page { get; set; } = 1;
    
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; set; } = 10;
    
    public string SortBy { get; set; } = "Id";
    
    public bool SortDescending { get; set; } = false;
}