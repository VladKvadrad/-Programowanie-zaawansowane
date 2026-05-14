using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class OrderValidatorTests
{
    private readonly OrderValidator _validator = new();

    private static Order MakeValidOrder() => new()
    {
        Id = 1,
        Customer = new Customer { Id = 1, Name = "Test", City = "Warsaw", Email = "t@t.com" },
        Status = OrderStatus.New,
        CreatedAt = DateTime.Now.AddDays(-1),
        Items = new List<OrderItem>
        {
            new()
            {
                Product = new Product { Id = 1, Name = "P", Category = "C", Price = 100m },
                Quantity = 1,
                UnitPrice = 100m
            }
        }
    };

    [Fact]
    public void ValidateAll_OrderWithNoItems_ReturnsInvalidWithError()
    {
        var order = MakeValidOrder();
        order.Items.Clear();

        var (isValid, errors) = _validator.ValidateAll(order);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("item"));
    }

    [Fact]
    public void ValidateAll_OrderWithItems_PassesItemsRule()
    {
        var order = MakeValidOrder();

        var (isValid, _) = _validator.ValidateAll(order);

        Assert.True(isValid);
    }

    [Fact]
    public void ValidateAll_OrderExceedsAmountLimit_ReturnsInvalid()
    {
        var order = MakeValidOrder();
        order.Items[0].UnitPrice = 60000m;

        var (isValid, errors) = _validator.ValidateAll(order);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("50000"));
    }

    [Fact]
    public void ValidateAll_ItemWithZeroQuantity_ReturnsInvalid()
    {
        var order = MakeValidOrder();
        order.Items[0].Quantity = 0;
        
        var (isValid, errors) = _validator.ValidateAll(order);
        
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("quantit"));
    }
    

    [Fact]
    public void ValidateAll_OrderWithFutureDate_ReturnsInvalid()
    {
        var order = MakeValidOrder();
        order.CreatedAt = DateTime.Now.AddDays(5);

        var (isValid, errors) = _validator.ValidateAll(order);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("future"));
    }
    

    [Fact]
    public void ValidateAll_CancelledOrder_ReturnsInvalid()
    {
        var order = MakeValidOrder();
        order.Status = OrderStatus.Cancelled;
        
        var (isValid, errors) = _validator.ValidateAll(order);
        
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("cancelled"));
    }
    

    [Fact]
    public void ValidateAll_MultipleViolations_ReturnsAllErrors()
    {
        var order = MakeValidOrder();
        order.Items.Clear();
        order.Status = OrderStatus.Cancelled;
        order.CreatedAt = DateTime.Now.AddDays(3);

        var (isValid, errors) = _validator.ValidateAll(order);

        Assert.False(isValid);
        Assert.True(errors.Count >= 3);
    }
    

    [Theory]
    [InlineData(OrderStatus.New,        true)]
    [InlineData(OrderStatus.Validated,  true)]
    [InlineData(OrderStatus.Processing, true)]
    [InlineData(OrderStatus.Completed,  true)]
    [InlineData(OrderStatus.Cancelled,  false)]
    public void ValidateAll_VariousStatuses_ReturnsExpectedResult(
        OrderStatus status, bool expectedValid)
    {
        var order = MakeValidOrder();
        order.Status = status;

        var (isValid, _) = _validator.ValidateAll(order);

        Assert.Equal(expectedValid, isValid);
    }
}