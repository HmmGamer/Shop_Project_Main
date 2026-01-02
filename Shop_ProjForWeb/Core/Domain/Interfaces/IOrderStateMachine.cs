using Shop_ProjForWeb.Core.Domain.Enums;

namespace Shop_ProjForWeb.Core.Domain.Interfaces;

public interface IOrderStateMachine
{
    bool CanTransition(OrderStatus from, OrderStatus to);
    OrderStatus[] GetValidTransitions(OrderStatus current);
    void ValidateTransition(OrderStatus from, OrderStatus to);
    void ValidateBusinessRules(OrderStatus from, OrderStatus to, decimal orderTotal);
    bool IsTerminalState(OrderStatus status);
    bool CanBeCancelled(OrderStatus status);
    string GetStatusDescription(OrderStatus status);
}