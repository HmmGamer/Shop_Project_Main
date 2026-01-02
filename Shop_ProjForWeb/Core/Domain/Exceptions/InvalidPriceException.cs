namespace Shop_ProjForWeb.Core.Domain.Exceptions;

public class InvalidPriceException : Exception
{
    public InvalidPriceException(decimal price)
        : base($"Price {price} is invalid. Must be greater than 0") { }

    public InvalidPriceException(string message)
        : base(message) { }

    public InvalidPriceException(string message, Exception innerException)
        : base(message, innerException) { }
}
