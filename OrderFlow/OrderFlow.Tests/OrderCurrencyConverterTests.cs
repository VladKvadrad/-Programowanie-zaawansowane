using Moq;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class OrderCurrencyConverterTests
{
    private static Order MakeOrder(decimal unitPrice) => new()
    {
        Id = 1,
        Customer = new Customer { Id = 1, Name = "Test", City = "X", Email = "x@x.com" },
        Status = OrderStatus.New,
        CreatedAt = DateTime.Now,
        Items = new List<OrderItem>
        {
            new()
            {
                Product = new Product { Id = 1, Name = "P", Category = "C", Price = unitPrice },
                Quantity = 1,
                UnitPrice = unitPrice
            }
        }
    };

    [Fact]
    public async Task ConvertOrderTotalAsync_ValidCurrency_ReturnsConvertedAmount()
    {
        var mockCurrency = new Mock<ICurrencyService>();
        mockCurrency
            .Setup(s => s.ConvertAsync(1000m, "PLN", "USD"))
            .ReturnsAsync(247.50m);

        var converter = new OrderCurrencyConverter(mockCurrency.Object);
        var order = MakeOrder(1000m);

        var result = await converter.ConvertOrderTotalAsync(order, "USD");

        Assert.Equal(247.50m, result);
        mockCurrency.Verify(s => s.ConvertAsync(1000m, "PLN", "USD"), Times.Once);
    }

    [Fact]
    public async Task ConvertOrderTotalAsync_CurrencyServiceReturnsValue_PassesTotalCorrectly()
    {
        var mockCurrency = new Mock<ICurrencyService>();
        mockCurrency
            .Setup(s => s.ConvertAsync(It.IsAny<decimal>(), "PLN", "EUR"))
            .ReturnsAsync(50m);

        var converter = new OrderCurrencyConverter(mockCurrency.Object);
        var order = MakeOrder(200m);

        var result = await converter.ConvertOrderTotalAsync(order, "EUR");

        Assert.Equal(50m, result);
        mockCurrency.Verify(
            s => s.ConvertAsync(200m, "PLN", "EUR"), Times.Once);
    }
}