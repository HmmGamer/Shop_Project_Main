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

public class OrderCrudTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateOrder_WithValidData_ReturnsCreatedOrder()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync();

        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 2)
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/orders", orderRequest);

        // Debug: Print error if not successful
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error creating order: {response.StatusCode}");
            Console.WriteLine($"Error details: {errorContent}");
        }

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdOrder = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(response);
        createdOrder.Should().NotBeNull();
        createdOrder!.OrderId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsPagedOrders()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync();

        var orderRequest1 = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };
        var orderRequest2 = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };

        await Client.PostAsJsonAsync("/api/orders", orderRequest1);
        await Client.PostAsJsonAsync("/api/orders", orderRequest2);

        // Act
        var response = await Client.GetAsync("/api/orders?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ApiTestHelper.GetResponseAsync<PaginatedResponse<OrderDetailDto>>(response);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GetOrder_WithValidId_ReturnsOrder()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync();

        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };

        var createResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var createdOrder = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(createResponse);

        // Act
        var response = await Client.GetAsync($"/api/orders/{createdOrder!.OrderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await ApiTestHelper.GetResponseAsync<OrderDetailDto>(response);
        order.Should().NotBeNull();
        order!.OrderId.Should().Be(createdOrder.OrderId);
        order.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUserOrders_WithValidUserId_ReturnsUserOrders()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync();

        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };

        await Client.PostAsJsonAsync("/api/orders", orderRequest);

        // Act
        var response = await Client.GetAsync($"/api/orders/user/{user.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await ApiTestHelper.GetResponseAsync<List<OrderDetailDto>>(response);
        orders.Should().NotBeNull();
        orders!.Should().HaveCountGreaterOrEqualTo(1);
        orders.Should().OnlyContain(o => o.UserId == user.Id);
    }

    [Fact]
    public async Task CancelOrder_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync();

        var orderRequest = new CreateOrderRequest
        {
            UserId = user.Id,
            Items = TestDataGenerator.GenerateValidOrderItems(new List<Guid> { product.Id }, 1)
        };

        var createResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var createdOrder = await ApiTestHelper.GetResponseAsync<OrderResponseDto>(createResponse);

        // Act
        var response = await Client.DeleteAsync($"/api/orders/{createdOrder!.OrderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify cancellation
        var getResponse = await Client.GetAsync($"/api/orders/{createdOrder.OrderId}");
        var cancelledOrder = await ApiTestHelper.GetResponseAsync<OrderDetailDto>(getResponse);
        cancelledOrder!.Status.Should().Be(OrderStatus.Cancelled);
    }

    // Helper methods
    private async Task<UserDto> CreateTestUserAsync()
    {
        var userDto = TestDataGenerator.GenerateValidUser();
        var response = await Client.PostAsJsonAsync("/api/users", userDto);
        return (await ApiTestHelper.GetResponseAsync<UserDto>(response))!;
    }

    private async Task<ProductDto> CreateTestProductAsync()
    {
        var productDto = TestDataGenerator.GenerateValidProduct();
        var response = await Client.PostAsJsonAsync("/api/products", productDto);
        return (await ApiTestHelper.GetResponseAsync<ProductDto>(response))!;
    }
}
