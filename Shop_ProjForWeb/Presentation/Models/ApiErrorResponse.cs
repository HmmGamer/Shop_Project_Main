namespace Shop_ProjForWeb.Presentation.Models;

public class ApiErrorResponse
{
    public int StatusCode { get; set; }
    public required string Message { get; set; }
    public string? Details { get; set; }
}
