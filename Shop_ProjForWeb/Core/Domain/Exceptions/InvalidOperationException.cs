namespace Shop_ProjForWeb.Core.Domain.Exceptions;

public class InvalidOperationException : Exception
{
    public InvalidOperationException(string message) : base(message) { }
    
    public InvalidOperationException(string message, Exception innerException) 
        : base(message, innerException) { }
}
