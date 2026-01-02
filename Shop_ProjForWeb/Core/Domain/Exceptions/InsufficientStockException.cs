namespace Shop_ProjForWeb.Core.Domain.Exceptions;

public class InsufficientStockException : Exception
{
    public InsufficientStockException(string message) : base(message) { }
    
    public InsufficientStockException(string message, Exception innerException) 
        : base(message, innerException) { }
}
