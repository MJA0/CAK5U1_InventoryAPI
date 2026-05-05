using CloudNativeInventory.Api.Controllers;
using CloudNativeInventory.Api.Data;
using CloudNativeInventory.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CloudNativeInventory.Tests;

public class InventoryControllerTests
{
    private DbContextOptions<InventoryDbContext> CreateNewContextOptions()
    {
        // Skapar en unik in-memory databas för varje test
        return new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetProducts_ReturnsAllProducts()
    {
        // Arrange
        var options = CreateNewContextOptions();
        using var context = new InventoryDbContext(options);
        context.Products.Add(new Product { Id = 1, Name = "Test Item", Price = 100, StockQuantity = 5 });
        await context.SaveChangesAsync();

        var mockConfig = new Mock<IConfiguration>();
        var controller = new InventoryController(context, mockConfig.Object);

        // Act
        var actionResult = await controller.GetProducts();

        // Assert
        var result = actionResult.Value;
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public void VerifyIntegration_Returns500_WhenSecretIsLocalDefault()
    {
        // Arrange
        var options = CreateNewContextOptions();
        using var context = new InventoryDbContext(options);

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["ExternalServices:VendorApiKey"])
                  .Returns("LOCAL_DEV_SECRET_12345_DO_NOT_DEPLOY");

        var controller = new InventoryController(context, mockConfig.Object);

        // Act
        var result = controller.VerifyExternalIntegration() as ObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(500, result.StatusCode);
    }
}