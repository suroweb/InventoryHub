using System.Net;
using System.Net.Http.Json;
using InventoryHub.Shared.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ServerApp.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProductList_ReturnsSuccessStatusCode()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/productlist");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProductList_ReturnsProducts()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var products = await client.GetFromJsonAsync<List<Product>>("/api/productlist");

        // Assert
        Assert.NotNull(products);
        Assert.NotEmpty(products);
        Assert.True(products.Count >= 2);
    }

    [Fact]
    public async Task GetProductList_ProductsHaveCorrectStructure()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var products = await client.GetFromJsonAsync<List<Product>>("/api/productlist");

        // Assert
        Assert.NotNull(products);
        var firstProduct = products.First();

        Assert.True(firstProduct.Id > 0);
        Assert.False(string.IsNullOrEmpty(firstProduct.Name));
        Assert.True(firstProduct.Price > 0);
        Assert.NotNull(firstProduct.Category);
        Assert.False(string.IsNullOrEmpty(firstProduct.Category.Name));
        Assert.NotNull(firstProduct.Supplier);
        Assert.False(string.IsNullOrEmpty(firstProduct.Supplier.Name));
    }

    [Fact]
    public async Task GetVersion_ReturnsVersionInfo()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/version");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("version", content);
        Assert.Contains("environment", content);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheckReady_ReturnsHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheckLive_ReturnsHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProductList_ResponseHasSecurityHeaders()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/productlist");

        // Assert
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("X-XSS-Protection"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
    }
}
