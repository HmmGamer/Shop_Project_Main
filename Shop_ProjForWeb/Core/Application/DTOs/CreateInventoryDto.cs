namespace Shop_ProjForWeb.Core.Application.DTOs;

public class CreateInventoryDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
