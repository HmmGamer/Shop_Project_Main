namespace Shop_ProjForWeb.Tests.Integration;

using FluentAssertions;
using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Core.Domain.Enums;
using Shop_ProjForWeb.Presentation.Controllers;
using Shop_ProjForWeb.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

/// <summary>
/// Tests for business logic scenarios including:
/// - VIP upgrade logic
/// - Discount calculations
/// - Inventory management
/// - Order state transitions
/// - Stock reservation and concurrency
/// </summary>
public class BusinessLogicTests : IntegrationTestBase
{
    #region VIP Upgrade Logic Tests

    [Fact]
    public async Task VipUpgrade_WhenUserSpends1000_ShouldBecomeVip()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync(basePrice: 500m, discountPercent: 0);

        // Create first order for 500
        var order1Request = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var order1Response = await Client.PostAsJsonAsync("/api/orders", order1Request);
        var order1 = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(order1Response);

        // Pay first order
        await Client.PostAsync($"/api/orders/{order1!.OrderId}/pay", null);

        // Create second order for 500
        var order2Request = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var order2Response = await Client.PostAsJsonAsync("/api/orders", order2Request);
        var order2 = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(order2Response);

        // Act - Pay second order (total should be >= 1000)
        await Client.PostAsync($"/api/orders/{order2!.OrderId}/pay", null);

        // Assert - User should now be VIP
        var userResponse = await Client.GetAsync($"/api/users/{user.Id}");
        var updatedUser = await ApiTestHelper.GetResponseAsync<UserDto>(userResponse);
        updatedUser!.IsVip.Should().BeTrue("user spent >= 1000 and should be upgraded to VIP");
    }

    [Fact]
    public async Task VipUpgrade_WhenUserSpends999_ShouldNotBecomeVip()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync(basePrice: 999m);

        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(orderResponse);

        // Act - Pay order
        await Client.PostAsync($"/api/orders/{order!.OrderId}/pay", null);

        // Assert - User should NOT be VIP
        var userResponse = await Client.GetAsync($"/api/users/{user.Id}");
        var updatedUser = await ApiTestHelper.GetResponseAsync<UserDto>(userResponse);
        updatedUser!.IsVip.Should().BeFalse("user spent < 1000 and should not be VIP");
    }

    #endregion

    #region Discount Calculation Tests

    [Fact]
    public async Task Discount_VipUser_ShouldReceiveAdditionalDiscount()
    {
        // Arrange - Create VIP user by spending 1000
        var user = await CreateTestUserAsync();
        var product1 = await CreateTestProductAsync(basePrice: 1000m, discountPercent: 0);
        
        var order1Request = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product1.Id }, 1)
        };
        var order1Response = await Client.PostAsJsonAsync("/api/orders", order1Request);
        var order1 = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(order1Response);
        await Client.PostAsync($"/api/orders/{order1!.OrderId}/pay", null);

        // Create product with 10% discount
        var product2 = await CreateTestProductAsync(basePrice: 100m, discountPercent: 10);

        // Act - Create order as VIP user
        var order2Request = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product2.Id }, 1)
        };
        var order2Response = await Client.PostAsJsonAsync("/api/orders", order2Request);
        var order2 = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(order2Response);

        // Assert - Should have both product discount (10%) and VIP discount (5%)
        // Base: 100, Product discount: -10 (90), VIP discount: -5 (85.5)
        order2!.TotalPrice.Should().BeLessThan(90m, "VIP users should get additional discount on top of product discount");
    }

    [Fact]
    public async Task Discount_NonVipUser_ShouldOnlyReceiveProductDiscount()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync(basePrice: 100m, discountPercent: 10);

        // Act
        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(orderResponse);

        // Assert - Should only have product discount (10%)
        // Base: 100, Product discount: -10 = 90
        order!.TotalPrice.Should().Be(90m, "non-VIP users should only get product discount");
    }

    #endregion

    #region Inventory Management Tests

    [Fact]
    public async Task Inventory_WhenOrderCreated_ShouldDecreaseStock()
    {
        // Arrange
        var product = await CreateTestProductAsync(initialStock: 100);
        var user = await CreateTestUserAsync();

        // Get initial inventory
        var initialInventoryResponse = await Client.GetAsync($"/api/inventory/{product.Id}");
        var initialInventory = await ApiTestHelper.GetResponseAsync<InventoryStatusDto>(initialInventoryResponse);

        // Act - Create order for 5 items
        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 5)
        };
        await Client.PostAsJsonAsync("/api/orders", orderRequest);

        // Assert - Stock should decrease by 5
        var finalInventoryResponse = await Client.GetAsync($"/api/inventory/{product.Id}");
        var finalInventory = await ApiTestHelper.GetResponseAsync<InventoryStatusDto>(finalInventoryResponse);
        
        finalInventory!.StockQuantity.Should().Be(initialInventory!.StockQuantity - 5, "stock should decrease by order quantity");
    }

    [Fact]
    public async Task Inventory_WhenOrderCancelled_ShouldRestoreStock()
    {
        // Arrange
        var product = await CreateTestProductAsync(initialStock: 100);
        var user = await CreateTestUserAsync();

        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 5)
        };
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(orderResponse);

        // Get inventory after order
        var inventoryAfterOrderResponse = await Client.GetAsync($"/api/inventory/{product.Id}");
        var inventoryAfterOrder = await ApiTestHelper.GetResponseAsync<InventoryStatusDto>(inventoryAfterOrderResponse);

        // Act - Cancel order
        await Client.DeleteAsync($"/api/orders/{order!.OrderId}");

        // Assert - Stock should be restored
        var finalInventoryResponse = await Client.GetAsync($"/api/inventory/{product.Id}");
        var finalInventory = await ApiTestHelper.GetResponseAsync<InventoryStatusDto>(finalInventoryResponse);
        
        finalInventory!.StockQuantity.Should().Be(inventoryAfterOrder!.StockQuantity + 5, "stock should be restored after cancellation");
    }

    [Fact]
    public async Task Inventory_WhenInsufficientStock_ShouldRejectOrder()
    {
        // Arrange
        var product = await CreateTestProductAsync(initialStock: 5);
        var user = await CreateTestUserAsync();

        // Act - Try to order 10 items when only 5 available
        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 10)
        };
        var response = await Client.PostAsJsonAsync("/api/orders", orderRequest);

        // Assert - Should return BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "order should be rejected when insufficient stock");
        
        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("Insufficient stock", "error message should indicate stock issue");
    }

    [Fact]
    public async Task Inventory_LowStockFlag_ShouldBeSetWhenBelowThreshold()
    {
        // Arrange
        var product = await CreateTestProductAsync(initialStock: 15); // Threshold is 10
        var user = await CreateTestUserAsync();

        // Act - Order 10 items, leaving 5 (below threshold of 10)
        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 10)
        };
        await Client.PostAsJsonAsync("/api/orders", orderRequest);

        // Assert - Low stock flag should be set
        var inventoryResponse = await Client.GetAsync($"/api/inventory/{product.Id}");
        var inventory = await ApiTestHelper.GetResponseAsync<InventoryStatusDto>(inventoryResponse);
        
        inventory!.IsLowStock.Should().BeTrue("low stock flag should be set when quantity falls below threshold");
    }

    #endregion

    #region Order State Transition Tests

    [Fact]
    public async Task OrderState_NewOrder_ShouldBeInCreatedState()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync();

        // Act
        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var response = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(response);

        // Assert
        order!.Status.Should().Be(OrderStatus.Created, "new orders should start in Created state");
    }

    [Fact]
    public async Task OrderState_WhenPaid_ShouldTransitionToPaidState()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync();

        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(orderResponse);

        // Act - Pay order
        await Client.PostAsync($"/api/orders/{order!.OrderId}/pay", null);

        // Assert - Order should be in Paid state
        var paidOrderResponse = await Client.GetAsync($"/api/orders/{order.OrderId}");
        var paidOrder = await ApiTestHelper.GetResponseAsync<OrderDetailDto>(paidOrderResponse);
        
        paidOrder!.Status.Should().Be(OrderStatus.Paid, "order should transition to Paid after payment");
        paidOrder.PaidAt.Should().NotBeNull("paid orders should have PaidAt timestamp");
    }

    [Fact]
    public async Task OrderState_WhenCancelled_ShouldTransitionToCancelledState()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync();

        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(orderResponse);

        // Act - Cancel order
        await Client.DeleteAsync($"/api/orders/{order!.OrderId}");

        // Assert - Order should be in Cancelled state
        var cancelledOrderResponse = await Client.GetAsync($"/api/orders/{order.OrderId}");
        var cancelledOrder = await ApiTestHelper.GetResponseAsync<OrderDetailDto>(cancelledOrderResponse);
        
        cancelledOrder!.Status.Should().Be(OrderStatus.Cancelled, "order should transition to Cancelled after cancellation");
    }

    [Fact]
    public async Task OrderState_PaidOrderCanBeCancelled()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync();

        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(orderResponse);

        // Pay the order
        await Client.PostAsync($"/api/orders/{order!.OrderId}/pay", null);

        // Act - Cancel paid order
        var cancelResponse = await Client.DeleteAsync($"/api/orders/{order.OrderId}");

        // Assert - Should succeed
        if (cancelResponse.StatusCode != HttpStatusCode.NoContent)
        {
            var errorContent = await cancelResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Cancellation failed with status {cancelResponse.StatusCode}: {errorContent}");
        }
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, "paid orders can be cancelled");
    }

    #endregion

    #region Multi-Product Order Tests

    [Fact]
    public async Task Order_WithMultipleProducts_ShouldCalculateCorrectTotal()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product1 = await CreateTestProductAsync(basePrice: 100m, discountPercent: 0);
        var product2 = await CreateTestProductAsync(basePrice: 200m, discountPercent: 10);
        var product3 = await CreateTestProductAsync(basePrice: 50m, discountPercent: 0);

        // Act - Order 2 of product1, 1 of product2, 3 of product3
        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = product1.Id, Quantity = 2 },
                new CreateOrderItemDto { ProductId = product2.Id, Quantity = 1 },
                new CreateOrderItemDto { ProductId = product3.Id, Quantity = 3 }
            }
        };
        var response = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(response);

        // Assert - Total should be: (100*2) + (200*0.9*1) + (50*3) = 200 + 180 + 150 = 530
        order!.TotalPrice.Should().Be(530m, "order total should sum all items with their discounts");
    }

    [Fact]
    public async Task Order_WithMultipleProducts_ShouldDecreaseAllInventories()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product1 = await CreateTestProductAsync(initialStock: 100);
        var product2 = await CreateTestProductAsync(initialStock: 50);

        // Get initial inventories
        var inv1Response = await Client.GetAsync($"/api/inventory/{product1.Id}");
        var inv1Initial = await ApiTestHelper.GetResponseAsync<InventoryStatusDto>(inv1Response);
        var inv2Response = await Client.GetAsync($"/api/inventory/{product2.Id}");
        var inv2Initial = await ApiTestHelper.GetResponseAsync<InventoryStatusDto>(inv2Response);

        // Act - Order from both products
        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = product1.Id, Quantity = 5 },
                new CreateOrderItemDto { ProductId = product2.Id, Quantity = 3 }
            }
        };
        await Client.PostAsJsonAsync("/api/orders", orderRequest);

        // Assert - Both inventories should decrease
        var inv1FinalResponse = await Client.GetAsync($"/api/inventory/{product1.Id}");
        var inv1Final = await ApiTestHelper.GetResponseAsync<InventoryStatusDto>(inv1FinalResponse);
        var inv2FinalResponse = await Client.GetAsync($"/api/inventory/{product2.Id}");
        var inv2Final = await ApiTestHelper.GetResponseAsync<InventoryStatusDto>(inv2FinalResponse);

        inv1Final!.StockQuantity.Should().Be(inv1Initial!.StockQuantity - 5);
        inv2Final!.StockQuantity.Should().Be(inv2Initial!.StockQuantity - 3);
    }

    #endregion

    #region VIP Downgrade Tests

    [Fact]
    public async Task VipDowngrade_WhenPaidOrderCancelled_ShouldRecalculateVipStatus()
    {
        // Arrange - Create user and make them VIP by spending exactly $1000
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync(basePrice: 1000m, discountPercent: 0);

        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(orderResponse);

        // Pay order - user becomes VIP
        await Client.PostAsync($"/api/orders/{order!.OrderId}/pay", null);

        // Verify user is VIP
        var vipUserResponse = await Client.GetAsync($"/api/users/{user.Id}");
        var vipUser = await ApiTestHelper.GetResponseAsync<UserDto>(vipUserResponse);
        vipUser!.IsVip.Should().BeTrue("user should be VIP after spending $1000");

        // Act - Cancel the paid order
        var cancelResponse = await Client.DeleteAsync($"/api/orders/{order.OrderId}");
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, "cancellation should succeed");

        // Assert - User should no longer be VIP (total spending is now $0)
        var downgradeUserResponse = await Client.GetAsync($"/api/users/{user.Id}");
        var downgradedUser = await ApiTestHelper.GetResponseAsync<UserDto>(downgradeUserResponse);
        downgradedUser!.IsVip.Should().BeFalse("user should lose VIP status after cancelling the order that made them VIP");
    }

    [Fact]
    public async Task VipDowngrade_WhenPartialOrdersCancelled_ShouldMaintainVipIfAboveThreshold()
    {
        // Arrange - Create user and make them VIP by spending $1500 across two orders
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync(basePrice: 750m, discountPercent: 0);

        // First order - $750
        var order1Request = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var order1Response = await Client.PostAsJsonAsync("/api/orders", order1Request);
        var order1 = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(order1Response);
        await Client.PostAsync($"/api/orders/{order1!.OrderId}/pay", null);

        // Second order - $750 (total now $1500)
        var order2Request = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var order2Response = await Client.PostAsJsonAsync("/api/orders", order2Request);
        var order2 = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(order2Response);
        await Client.PostAsync($"/api/orders/{order2!.OrderId}/pay", null);

        // Verify user is VIP
        var vipUserResponse = await Client.GetAsync($"/api/users/{user.Id}");
        var vipUser = await ApiTestHelper.GetResponseAsync<UserDto>(vipUserResponse);
        vipUser!.IsVip.Should().BeTrue("user should be VIP after spending $1500");

        // Act - Cancel one order (remaining $750 is below $1000 threshold)
        await Client.DeleteAsync($"/api/orders/{order1.OrderId}");

        // Assert - User should lose VIP status
        var afterCancelResponse = await Client.GetAsync($"/api/users/{user.Id}");
        var afterCancelUser = await ApiTestHelper.GetResponseAsync<UserDto>(afterCancelResponse);
        afterCancelUser!.IsVip.Should().BeFalse("user should lose VIP after cancellation drops spending below $1000");
    }

    #endregion

    #region Higher VIP Tier Tests

    [Fact]
    public async Task VipUpgrade_WhenUserSpends5000_ShouldBecomeTier2()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync(basePrice: 5000m, discountPercent: 0);

        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(orderResponse);

        // Act - Pay order
        await Client.PostAsync($"/api/orders/{order!.OrderId}/pay", null);

        // Assert - User should be VIP (Tier 2)
        var userResponse = await Client.GetAsync($"/api/users/{user.Id}");
        var updatedUser = await ApiTestHelper.GetResponseAsync<UserDto>(userResponse);
        updatedUser!.IsVip.Should().BeTrue("user spent $5000 and should be VIP Tier 2");

        // Verify Tier 2 discount (15%) is applied on next order
        var product2 = await CreateTestProductAsync(basePrice: 100m, discountPercent: 0);
        var order2Request = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product2.Id }, 1)
        };
        var order2Response = await Client.PostAsJsonAsync("/api/orders", order2Request);
        var order2 = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(order2Response);
        
        // Tier 2 = 15% discount, so $100 - 15% = $85
        order2!.TotalPrice.Should().Be(85m, "Tier 2 VIP should get 15% discount");
    }

    [Fact]
    public async Task VipUpgrade_WhenUserSpends30000_ShouldBecomeTier3()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync(basePrice: 30000m, discountPercent: 0);

        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(orderResponse);

        // Act - Pay order
        await Client.PostAsync($"/api/orders/{order!.OrderId}/pay", null);

        // Assert - User should be VIP (Tier 3)
        var userResponse = await Client.GetAsync($"/api/users/{user.Id}");
        var updatedUser = await ApiTestHelper.GetResponseAsync<UserDto>(userResponse);
        updatedUser!.IsVip.Should().BeTrue("user spent $30000 and should be VIP Tier 3");

        // Verify Tier 3 discount (20%) is applied on next order
        var product2 = await CreateTestProductAsync(basePrice: 100m, discountPercent: 0);
        var order2Request = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product2.Id }, 1)
        };
        var order2Response = await Client.PostAsJsonAsync("/api/orders", order2Request);
        var order2 = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(order2Response);
        
        // Tier 3 = 20% discount, so $100 - 20% = $80
        order2!.TotalPrice.Should().Be(80m, "Tier 3 VIP should get 20% discount");
    }

    [Fact]
    public async Task VipUpgrade_MultiTierJump_ShouldJumpDirectlyToTier3()
    {
        // Arrange - User makes single large purchase that exceeds Tier 3 threshold
        var user = await CreateTestUserAsync();
        
        // Verify user starts as non-VIP
        var initialUserResponse = await Client.GetAsync($"/api/users/{user.Id}");
        var initialUser = await ApiTestHelper.GetResponseAsync<UserDto>(initialUserResponse);
        initialUser!.IsVip.Should().BeFalse("new user should not be VIP");

        var product = await CreateTestProductAsync(basePrice: 35000m, discountPercent: 0);

        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(orderResponse);

        // Act - Pay order (single purchase of $35000)
        await Client.PostAsync($"/api/orders/{order!.OrderId}/pay", null);

        // Assert - User should jump directly to Tier 3
        var userResponse = await Client.GetAsync($"/api/users/{user.Id}");
        var updatedUser = await ApiTestHelper.GetResponseAsync<UserDto>(userResponse);
        updatedUser!.IsVip.Should().BeTrue("user should be VIP after $35000 purchase");

        // Verify Tier 3 discount (20%) is applied
        var product2 = await CreateTestProductAsync(basePrice: 100m, discountPercent: 0);
        var order2Request = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product2.Id }, 1)
        };
        var order2Response = await Client.PostAsJsonAsync("/api/orders", order2Request);
        var order2 = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(order2Response);
        
        order2!.TotalPrice.Should().Be(80m, "user should have Tier 3 discount (20%) after multi-tier jump");
    }

    #endregion

    #region Invalid State Transition Tests

    [Fact]
    public async Task OrderState_PayAlreadyPaidOrder_ShouldFail()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync();

        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(orderResponse);

        // Pay the order first time
        var firstPayResponse = await Client.PostAsync($"/api/orders/{order!.OrderId}/pay", null);
        firstPayResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, "first payment should succeed");

        // Act - Try to pay again
        var secondPayResponse = await Client.PostAsync($"/api/orders/{order.OrderId}/pay", null);

        // Assert - Should fail (already paid)
        secondPayResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError, "paying already paid order should fail");
    }

    [Fact]
    public async Task OrderState_CancelAlreadyCancelledOrder_ShouldFail()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync();

        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(orderResponse);

        // Cancel the order first time
        var firstCancelResponse = await Client.DeleteAsync($"/api/orders/{order!.OrderId}");
        firstCancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, "first cancellation should succeed");

        // Act - Try to cancel again
        var secondCancelResponse = await Client.DeleteAsync($"/api/orders/{order.OrderId}");

        // Assert - Should fail (already cancelled - terminal state)
        secondCancelResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "cancelling already cancelled order should fail");
    }

    #endregion

    #region Reports Integration Tests

    [Fact]
    public async Task Reports_SalesSummary_ReturnsValidData()
    {
        // Arrange - Create some paid orders
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync(basePrice: 100m, discountPercent: 0);

        // Create and pay two orders
        for (int i = 0; i < 2; i++)
        {
            var orderRequest = new CreateOrderRequest
            {
                UserId = user.Id,
                Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
            };
            var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
            var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(orderResponse);
            await Client.PostAsync($"/api/orders/{order!.OrderId}/pay", null);
        }

        // Act
        var response = await Client.GetAsync("/api/reports/sales");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "sales report should return OK");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("totalRevenue", "sales report should contain total revenue");
        content.Should().Contain("totalOrders", "sales report should contain total orders");
    }

    [Fact]
    public async Task Reports_InventoryReport_ReturnsValidData()
    {
        // Arrange - Create product with inventory
        var product = await CreateTestProductAsync(initialStock: 50);

        // Act
        var response = await Client.GetAsync("/api/reports/inventory");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "inventory report should return OK");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("totalItems", "inventory report should contain total items");
        content.Should().Contain("totalQuantity", "inventory report should contain total quantity");
    }

    [Fact]
    public async Task Reports_TopProducts_ReturnsValidData()
    {
        // Arrange - Create orders for products
        var user = await CreateTestUserAsync();
        var product1 = await CreateTestProductAsync(basePrice: 100m, discountPercent: 0);
        var product2 = await CreateTestProductAsync(basePrice: 200m, discountPercent: 0);

        // Order product1 twice, product2 once
        for (int i = 0; i < 2; i++)
        {
            var orderRequest = new CreateOrderRequest
            {
                UserId = user.Id,
                Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product1.Id }, 1)
            };
            var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
            var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(orderResponse);
            await Client.PostAsync($"/api/orders/{order!.OrderId}/pay", null);
        }

        var order2Request = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product2.Id }, 1)
        };
        var order2Response = await Client.PostAsJsonAsync("/api/orders", order2Request);
        var order2 = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(order2Response);
        await Client.PostAsync($"/api/orders/{order2!.OrderId}/pay", null);

        // Act
        var response = await Client.GetAsync("/api/reports/top-products?limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "top products report should return OK");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("productId", "top products should contain product IDs");
        content.Should().Contain("totalQuantitySold", "top products should contain quantity sold");
    }

    [Fact]
    public async Task Reports_OrderStatusDistribution_ReturnsValidData()
    {
        // Arrange - Create orders in different states
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync(basePrice: 100m, discountPercent: 0);

        // Create order (Created state)
        var order1Request = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        await Client.PostAsJsonAsync("/api/orders", order1Request);

        // Create and pay order (Paid state)
        var order2Request = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var order2Response = await Client.PostAsJsonAsync("/api/orders", order2Request);
        var order2 = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(order2Response);
        await Client.PostAsync($"/api/orders/{order2!.OrderId}/pay", null);

        // Create and cancel order (Cancelled state)
        var order3Request = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var order3Response = await Client.PostAsJsonAsync("/api/orders", order3Request);
        var order3 = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(order3Response);
        await Client.DeleteAsync($"/api/orders/{order3!.OrderId}");

        // Act
        var response = await Client.GetAsync("/api/reports/order-status-distribution");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "order status distribution should return OK");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("createdCount", "distribution should contain created count");
        content.Should().Contain("paidCount", "distribution should contain paid count");
        content.Should().Contain("cancelledCount", "distribution should contain cancelled count");
    }

    #endregion

    #region Product Deletion Business Rules Tests

    [Fact]
    public async Task Product_WithOrders_CannotBeDeleted()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync();

        // Create order with this product
        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        await Client.PostAsJsonAsync("/api/orders", orderRequest);

        // Act - Try to delete product
        var response = await Client.DeleteAsync($"/api/products/{product.Id}");

        // Assert - Should be rejected
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "products with orders cannot be deleted");
        
        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("ordered", "error should mention the product has been ordered");
    }

    [Fact]
    public async Task Product_WithoutOrders_CanBeDeleted()
    {
        // Arrange
        var product = await CreateTestProductAsync();

        // Act - Delete product (no orders exist)
        var response = await Client.DeleteAsync($"/api/products/{product.Id}");

        // Assert - Should succeed
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "products without orders can be deleted");
    }

    [Fact]
    public async Task Product_WithInventory_CanBeDeleted()
    {
        // Arrange
        var product = await CreateTestProductAsync(initialStock: 10);

        // Act - Delete product with inventory (should succeed since no orders exist)
        var response = await Client.DeleteAsync($"/api/products/{product.Id}");

        // Assert - Should succeed (inventory is deleted along with product)
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "products without orders can be deleted even if they have inventory");
    }

    #endregion

    #region End-to-End Scenario Tests

    [Fact]
    public async Task CompleteScenario_CreateUserAndProductsThenPurchaseForVIP_ShouldSucceed()
    {
        // Step 1: Create user named Taha
        var createUserRequest = new CreateUserDto { FullName = "Taha" };
        var userResponse = await Client.PostAsJsonAsync("/api/users", createUserRequest);
        userResponse.StatusCode.Should().Be(HttpStatusCode.Created, "user creation should succeed");
        var user = await ApiTestHelper.GetResponseAsync<UserDto>(userResponse);
        user.Should().NotBeNull();
        user!.FullName.Should().Be("Taha");
        user.IsVip.Should().BeFalse("new user should not be VIP initially");
        Console.WriteLine($"✓ Step 1: Created user 'Taha' with ID: {user.Id}");

        // Step 2: Create product named "moz" with price $600
        var createMozRequest = new CreateProductDto
        {
            Name = "moz",
            BasePrice = 600m,
            DiscountPercent = 0,
            IsActive = true,
            InitialStock = 100
        };
        var mozResponse = await Client.PostAsJsonAsync("/api/products", createMozRequest);
        mozResponse.StatusCode.Should().Be(HttpStatusCode.Created, "moz product creation should succeed");
        var moz = await ApiTestHelper.GetResponseAsync<ProductDto>(mozResponse);
        moz.Should().NotBeNull();
        moz!.Name.Should().Be("moz");
        moz.BasePrice.Should().Be(600m);
        Console.WriteLine($"✓ Step 2a: Created product 'moz' with price $600, ID: {moz.Id}");

        // Step 3: Create product named "sib" with price $500
        var createSibRequest = new CreateProductDto
        {
            Name = "sib",
            BasePrice = 500m,
            DiscountPercent = 0,
            IsActive = true,
            InitialStock = 100
        };
        var sibResponse = await Client.PostAsJsonAsync("/api/products", createSibRequest);
        sibResponse.StatusCode.Should().Be(HttpStatusCode.Created, "sib product creation should succeed");
        var sib = await ApiTestHelper.GetResponseAsync<ProductDto>(sibResponse);
        sib.Should().NotBeNull();
        sib!.Name.Should().Be("sib");
        sib.BasePrice.Should().Be(500m);
        Console.WriteLine($"✓ Step 2b: Created product 'sib' with price $500, ID: {sib.Id}");

        // Step 4: Create order with both products (total = $1100, enough for VIP)
        var createOrderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = moz.Id, Quantity = 1 },
                new CreateOrderItemDto { ProductId = sib.Id, Quantity = 1 }
            }
        };
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", createOrderRequest);
        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created, "order creation should succeed");
        var order = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(orderResponse);
        order.Should().NotBeNull();
        order!.TotalPrice.Should().Be(1100m, "order total should be $1100 (moz $600 + sib $500)");
        order.Status.Should().Be(OrderStatus.Created);
        Console.WriteLine($"✓ Step 3: Created order with both products, Total: ${order.TotalPrice}, Order ID: {order.OrderId}");

        // Step 5: Purchase the products (pay for the order)
        var payResponse = await Client.PostAsync($"/api/orders/{order.OrderId}/pay", null);
        payResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, "payment should succeed");
        Console.WriteLine($"✓ Step 4: Payment processed successfully");

        // Step 6: Verify order is paid
        var paidOrderResponse = await Client.GetAsync($"/api/orders/{order.OrderId}");
        var paidOrder = await ApiTestHelper.GetResponseAsync<OrderDetailDto>(paidOrderResponse);
        paidOrder.Should().NotBeNull();
        paidOrder!.Status.Should().Be(OrderStatus.Paid, "order should be in Paid status");
        paidOrder.PaidAt.Should().NotBeNull("paid order should have PaidAt timestamp");
        Console.WriteLine($"✓ Step 5: Order status confirmed as Paid");

        // Step 7: Verify user is now VIP
        var vipUserResponse = await Client.GetAsync($"/api/users/{user.Id}");
        var vipUser = await ApiTestHelper.GetResponseAsync<UserDto>(vipUserResponse);
        vipUser.Should().NotBeNull();
        vipUser!.IsVip.Should().BeTrue("user should be VIP after spending $1100 (>= $1000 threshold)");
        Console.WriteLine($"✓ Step 6: User 'Taha' is now VIP!");

        // Final verification
        Console.WriteLine("\n========================================");
        Console.WriteLine("✓✓✓ COMPLETE SCENARIO TEST PASSED ✓✓✓");
        Console.WriteLine("========================================");
        Console.WriteLine($"User: {vipUser.FullName}");
        Console.WriteLine($"VIP Status: {vipUser.IsVip}");
        Console.WriteLine($"Total Spent: ${order.TotalPrice}");
        Console.WriteLine($"Products Purchased: moz ($600) + sib ($500)");
        Console.WriteLine($"Order Status: {paidOrder.Status}");
        Console.WriteLine("========================================");
    }

    #endregion

    // Helper methods
    private async Task<UserDto> CreateTestUserAsync()
    {
        var userDto = TestDataGenerator.GenerateValidUser();
        var response = await Client.PostAsJsonAsync("/api/users", userDto);
        return (await ApiTestHelper.GetResponseAsync<UserDto>(response))!;
    }

    private async Task<ProductDto> CreateTestProductAsync(decimal? basePrice = null, int? discountPercent = null, int? initialStock = null)
    {
        var productDto = TestDataGenerator.GenerateValidProduct();
        if (basePrice.HasValue) productDto.BasePrice = basePrice.Value;
        if (discountPercent.HasValue) productDto.DiscountPercent = discountPercent.Value;
        if (initialStock.HasValue) productDto.InitialStock = initialStock.Value;
        
        var response = await Client.PostAsJsonAsync("/api/products", productDto);
        return (await ApiTestHelper.GetResponseAsync<ProductDto>(response))!;
    }
}
