namespace Shop_ProjForWeb.Core.Domain.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(string message) : base(message) { }
    
    public UserNotFoundException(string message, Exception innerException) 
        : base(message, innerException) { }
}
