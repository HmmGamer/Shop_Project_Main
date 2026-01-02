using Shop_ProjForWeb.Core.Domain.Enums;
using Shop_ProjForWeb.Core.Domain.Interfaces;

namespace Shop_ProjForWeb.Core.Application.Services;

public class OrderStateMachine : IOrderStateMachine
{
    private readonly Dictionary<OrderStatus, OrderStatus[]> _validTransitions;
    private readonly Dictionary<OrderStatus, string> _statusDescriptions;

    public OrderStateMachine()
    {
        _validTransitions = new Dictionary<OrderStatus, OrderStatus[]>
        {
            { OrderStatus.Created, new[] { OrderStatus.Paid, OrderStatus.Cancelled } },
            { OrderStatus.Pending, new[] { OrderStatus.Paid, OrderStatus.Cancelled } }, // Legacy support
            { OrderStatus.Paid, new[] { OrderStatus.Shipped, OrderStatus.Cancelled } },
            { OrderStatus.Shipped, new[] { OrderStatus.Delivered } },
            { OrderStatus.Delivered, new OrderStatus[0] }, // Terminal state
            { OrderStatus.Cancelled, new OrderStatus[0] }  // Terminal state
        };

        _statusDescriptions = new Dictionary<OrderStatus, string>
        {
            { OrderStatus.Created, "Order has been created and is awaiting payment" },
            { OrderStatus.Pending, "Order is pending (legacy status)" },
            { OrderStatus.Paid, "Order has been paid and is ready for processing" },
            { OrderStatus.Shipped, "Order has been shipped to customer" },
            { OrderStatus.Delivered, "Order has been delivered to customer" },
            { OrderStatus.Cancelled, "Order has been cancelled" }
        };
    }

    public bool CanTransition(OrderStatus from, OrderStatus to)
    {
        return _validTransitions.ContainsKey(from) && _validTransitions[from].Contains(to);
    }

    public OrderStatus[] GetValidTransitions(OrderStatus current)
    {
        return _validTransitions.ContainsKey(current) ? _validTransitions[current] : new OrderStatus[0];
    }

    public void ValidateTransition(OrderStatus from, OrderStatus to)
    {
        if (!CanTransition(from, to))
        {
            var validTransitions = string.Join(", ", GetValidTransitions(from));
            var fromDescription = _statusDescriptions.GetValueOrDefault(from, from.ToString());
            var toDescription = _statusDescriptions.GetValueOrDefault(to, to.ToString());
            
            throw new InvalidOperationException(
                $"Invalid order status transition from '{from}' ({fromDescription}) to '{to}' ({toDescription}). " +
                $"Valid transitions from {from}: {(validTransitions.Any() ? validTransitions : "None (terminal state)")}");
        }
    }

    public bool IsTerminalState(OrderStatus status)
    {
        return status == OrderStatus.Delivered || status == OrderStatus.Cancelled;
    }

    public bool CanBeCancelled(OrderStatus status)
    {
        return CanTransition(status, OrderStatus.Cancelled);
    }

    public string GetStatusDescription(OrderStatus status)
    {
        return _statusDescriptions.GetValueOrDefault(status, status.ToString());
    }

    public void ValidateBusinessRules(OrderStatus from, OrderStatus to, decimal orderTotal)
    {
        // First validate the basic transition
        ValidateTransition(from, to);

        // Additional business rule validations
        switch (to)
        {
            case OrderStatus.Paid:
                if (orderTotal <= 0)
                {
                    throw new InvalidOperationException("Cannot mark order as paid when total is zero or negative");
                }
                break;

            case OrderStatus.Shipped:
                if (from != OrderStatus.Paid)
                {
                    throw new InvalidOperationException("Can only ship orders that have been paid");
                }
                break;

            case OrderStatus.Delivered:
                if (from != OrderStatus.Shipped)
                {
                    throw new InvalidOperationException("Can only deliver orders that have been shipped");
                }
                break;
        }
    }
}