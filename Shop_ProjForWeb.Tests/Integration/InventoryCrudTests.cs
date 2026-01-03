namespace Shop_ProjForWeb.Tests.Integration;

using FluentAssertions;
using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

public class InventoryCrudTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateInventory_WithValidData_ReturnsCreatedInventory()
    {
        // Arrange
        var product = await CreateTestProductAsync();
        var createDto = TestDataGenerator.GenerateValidInventory(product.Id);

        // Act
        var response = await Client.PostAsJsonAsync("/api/inventory", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdInventory = await ApiTestHelper.GetResponseAsync<InventoryDto>(response);
        createdInventory.Should().NotBeNull();
        createdInventory!.ProductId.Should().Be(product.Id);
        createdInventory.Quantity.Should().Be(createDto.Quantity);
    }

    [Fact]
    public async Task GetAllInventory_ReturnsAllInventory()
    {
        // Arrange
        var product1 = await CreateTestProductAsync();
        var product2 = await CreateTestProductAsync();
        
        var inventory1 = TestDataGenerator.GenerateValidInventory(product1.Id);
        var inventory2 = TestDataGenerator.GenerateValidInventory(product2.Id);
        
        await Client.PostAsJsonAsync("/api/inventory", inventory1);
        await Client.PostAsJsonAsync("/api/inventory", inventory2);

        // Act
        var response = await Client.GetAsync("/api/inventory");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var inventories = await ApiTestHelper.GetResponseAsync<List<InventoryDto>>(response);
        inventories.Should().NotBeNull();
        inventories!.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GetInventoryStatus_WithValidProductId_ReturnsInventoryStatus()
    {
        // Arrange
        var product = await CreateTestProductAsync();
        var createDto = TestDataGenerator.GenerateValidInventory(product.Id);
        await Client.PostAsJsonAsync("/api/inventory", createDto);

        // Act
        var response = await Client.GetAsync($"/api/inventory/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var inventory = await ApiTestHelper.GetResponseAsync<InventoryStatusDto>(response);
        inventory.Should().NotBeNull();
        inventory!.ProductId.Should().Be(product.Id);
    }

    [Fact]
    public async Task GetLowStockItems_ReturnsOnlyLowStockItems()
    {
        // Arrange
        var product1 = await CreateTestProductAsync();
        var product2 = await CreateTestProductAsync();
        
        // Create low stock inventory (quantity < 10)
        var lowStockInventory = new CreateInventoryDto
        {
            ProductId = product1.Id,
            Quantity = 5
        };
        
        // Create normal stock inventory
        var normalStockInventory = new CreateInventoryDto
        {
            ProductId = product2.Id,
            Quantity = 50
        };
        
        await Client.PostAsJsonAsync("/api/inventory", lowStockInventory);
        await Client.PostAsJsonAsync("/api/inventory", normalStockInventory);

        // Act
        var response = await Client.GetAsync("/api/inventory/low-stock");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var inventories = await ApiTestHelper.GetResponseAsync<List<InventoryDto>>(response);
        inventories.Should().NotBeNull();
        inventories!.Should().Contain(i => i.ProductId == product1.Id);
        inventories.Should().OnlyContain(i => i.LowStockFlag);
    }

    [Fact]
    public async Task UpdateInventory_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var product = await CreateTestProductAsync();
        var createDto = TestDataGenerator.GenerateValidInventory(product.Id);
        await Client.PostAsJsonAsync("/api/inventory", createDto);

        var updateDto = TestDataGenerator.GenerateValidInventoryUpdate();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/inventory/{product.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify update
        var getResponse = await Client.GetAsync($"/api/inventory/{product.Id}");
        var updatedInventory = await ApiTestHelper.GetResponseAsync<InventoryStatusDto>(getResponse);
        updatedInventory!.StockQuantity.Should().Be(updateDto.Quantity);
    }

    // Helper method
    private async Task<ProductDto> CreateTestProductAsync()
    {
        var productDto = TestDataGenerator.GenerateValidProduct();
        var response = await Client.PostAsJsonAsync("/api/products", productDto);
        return (await ApiTestHelper.GetResponseAsync<ProductDto>(response))!;
    }
}
