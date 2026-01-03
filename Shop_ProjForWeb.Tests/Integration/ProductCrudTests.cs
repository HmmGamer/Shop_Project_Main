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

public class ProductCrudTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsCreatedProduct()
    {
        // Arrange
        var createDto = TestDataGenerator.GenerateValidProduct();

        // Act
        var response = await Client.PostAsJsonAsync("/api/products", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdProduct = await ApiTestHelper.GetResponseAsync<ProductDto>(response);
        createdProduct.Should().NotBeNull();
        createdProduct!.Name.Should().Be(createDto.Name);
        createdProduct.BasePrice.Should().Be(createDto.BasePrice);
        createdProduct.DiscountPercent.Should().Be(createDto.DiscountPercent);
        createdProduct.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetAllProducts_ReturnsPagedProducts()
    {
        // Arrange
        var product1 = TestDataGenerator.GenerateValidProduct();
        var product2 = TestDataGenerator.GenerateValidProduct();
        await Client.PostAsJsonAsync("/api/products", product1);
        await Client.PostAsJsonAsync("/api/products", product2);

        // Act
        var response = await Client.GetAsync("/api/products?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ApiTestHelper.GetResponseAsync<PaginatedResponse<ProductDto>>(response);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCountGreaterOrEqualTo(2);
        result.TotalCount.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GetProduct_WithValidId_ReturnsProduct()
    {
        // Arrange
        var createDto = TestDataGenerator.GenerateValidProduct();
        var createResponse = await Client.PostAsJsonAsync("/api/products", createDto);
        var createdProduct = await ApiTestHelper.GetResponseAsync<ProductDto>(createResponse);

        // Act
        var response = await Client.GetAsync($"/api/products/{createdProduct!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await ApiTestHelper.GetResponseAsync<ProductDto>(response);
        product.Should().NotBeNull();
        product!.Id.Should().Be(createdProduct.Id);
        product.Name.Should().Be(createDto.Name);
    }

    [Fact]
    public async Task GetActiveProducts_ReturnsOnlyActiveProducts()
    {
        // Arrange
        var activeProduct = TestDataGenerator.GenerateValidProduct();
        activeProduct.IsActive = true;
        var inactiveProduct = TestDataGenerator.GenerateValidProduct();
        inactiveProduct.IsActive = false;

        await Client.PostAsJsonAsync("/api/products", activeProduct);
        var inactiveResponse = await Client.PostAsJsonAsync("/api/products", inactiveProduct);
        var inactiveCreated = await ApiTestHelper.GetResponseAsync<ProductDto>(inactiveResponse);

        // Deactivate the inactive product
        var updateDto = new UpdateProductDto { IsActive = false };
        await Client.PutAsJsonAsync($"/api/products/{inactiveCreated!.Id}", updateDto);

        // Act
        var response = await Client.GetAsync("/api/products/active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await ApiTestHelper.GetResponseAsync<List<ProductDto>>(response);
        products.Should().NotBeNull();
        products!.Should().OnlyContain(p => p.IsActive);
    }

    [Fact]
    public async Task UpdateProduct_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var createDto = TestDataGenerator.GenerateValidProduct();
        var createResponse = await Client.PostAsJsonAsync("/api/products", createDto);
        var createdProduct = await ApiTestHelper.GetResponseAsync<ProductDto>(createResponse);

        var updateDto = TestDataGenerator.GenerateValidProductUpdate();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/products/{createdProduct!.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify update
        var getResponse = await Client.GetAsync($"/api/products/{createdProduct.Id}");
        var updatedProduct = await ApiTestHelper.GetResponseAsync<ProductDto>(getResponse);
        updatedProduct!.Name.Should().Be(updateDto.Name);
        updatedProduct.BasePrice.Should().Be(updateDto.BasePrice!.Value);
    }

    [Fact]
    public async Task DeleteProduct_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var createDto = TestDataGenerator.GenerateValidProduct();
        var createResponse = await Client.PostAsJsonAsync("/api/products", createDto);
        var createdProduct = await ApiTestHelper.GetResponseAsync<ProductDto>(createResponse);

        // Act
        var response = await Client.DeleteAsync($"/api/products/{createdProduct!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await Client.GetAsync($"/api/products/{createdProduct.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
