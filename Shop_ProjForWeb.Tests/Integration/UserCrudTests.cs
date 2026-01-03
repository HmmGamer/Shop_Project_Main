namespace Shop_ProjForWeb.Tests.Integration;

using FluentAssertions;
using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Tests.Helpers;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

public class UserCrudTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateUser_WithValidData_ReturnsCreatedUser()
    {
        // Arrange
        var createDto = TestDataGenerator.GenerateValidUser();

        // Act
        var response = await Client.PostAsJsonAsync("/api/users", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdUser = await ApiTestHelper.GetResponseAsync<UserDto>(response);
        createdUser.Should().NotBeNull();
        createdUser!.FullName.Should().Be(createDto.FullName);
        createdUser.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsPagedUsers()
    {
        // Arrange
        var user1 = TestDataGenerator.GenerateValidUser();
        var user2 = TestDataGenerator.GenerateValidUser();
        await Client.PostAsJsonAsync("/api/users", user1);
        await Client.PostAsJsonAsync("/api/users", user2);

        // Act
        var response = await Client.GetAsync("/api/users?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ApiTestHelper.GetResponseAsync<PaginatedResponse<UserDto>>(response);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCountGreaterOrEqualTo(2);
        result.TotalCount.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GetUser_WithValidId_ReturnsUser()
    {
        // Arrange
        var createDto = TestDataGenerator.GenerateValidUser();
        var createResponse = await Client.PostAsJsonAsync("/api/users", createDto);
        var createdUser = await ApiTestHelper.GetResponseAsync<UserDto>(createResponse);

        // Act
        var response = await Client.GetAsync($"/api/users/{createdUser!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await ApiTestHelper.GetResponseAsync<UserDto>(response);
        user.Should().NotBeNull();
        user!.Id.Should().Be(createdUser.Id);
        user.FullName.Should().Be(createDto.FullName);
    }

    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var createDto = TestDataGenerator.GenerateValidUser();
        var createResponse = await Client.PostAsJsonAsync("/api/users", createDto);
        var createdUser = await ApiTestHelper.GetResponseAsync<UserDto>(createResponse);

        var updateDto = TestDataGenerator.GenerateValidUserUpdate();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/users/{createdUser!.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify update
        var getResponse = await Client.GetAsync($"/api/users/{createdUser.Id}");
        var updatedUser = await ApiTestHelper.GetResponseAsync<UserDto>(getResponse);
        updatedUser!.FullName.Should().Be(updateDto.FullName);
    }

    [Fact]
    public async Task DeleteUser_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var createDto = TestDataGenerator.GenerateValidUser();
        var createResponse = await Client.PostAsJsonAsync("/api/users", createDto);
        var createdUser = await ApiTestHelper.GetResponseAsync<UserDto>(createResponse);

        // Act
        var response = await Client.DeleteAsync($"/api/users/{createdUser!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await Client.GetAsync($"/api/users/{createdUser.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
