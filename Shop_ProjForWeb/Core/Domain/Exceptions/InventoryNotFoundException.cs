namespace Shop_ProjForWeb.Core.Domain.Exceptions;

public class InventoryNotFoundException : Exception
{
    public InventoryNotFoundException(Guid productId)
        : base($"Inventory for product {productId} not found") { }

    public InventoryNotFoundException(string message)
        : base(message) { }

    public InventoryNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }
}
