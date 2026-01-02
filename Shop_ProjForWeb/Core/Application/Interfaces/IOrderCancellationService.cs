namespace Shop_ProjForWeb.Core.Application.Interfaces;

public interface IOrderCancellationService
{
    Task CancelOrderAsync(Guid orderId);
}
