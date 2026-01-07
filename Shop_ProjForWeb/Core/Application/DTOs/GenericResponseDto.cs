namespace Shop_ProjForWeb.Core.Application.DTOs;

public class GenericResponseDto<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}
