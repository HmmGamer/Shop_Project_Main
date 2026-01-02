namespace Shop_ProjForWeb.Core.Domain.Exceptions;

public class InvalidDiscountException : Exception
{
    public InvalidDiscountException(int discountPercent)
        : base($"Discount percent {discountPercent} is invalid. Must be between 0 and 100") { }

    public InvalidDiscountException(string message)
        : base(message) { }

    public InvalidDiscountException(string message, Exception innerException)
        : base(message, innerException) { }
}
