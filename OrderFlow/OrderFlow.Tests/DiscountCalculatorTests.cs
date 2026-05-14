using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class DiscountCalculatorTests
{
    private readonly DiscountCalculator _calc = new();

    private static Order MakeOrder(bool isVip, decimal unitPrice, int quantity = 1) => new()
    {
        Id = 1,
        Customer = new Customer { Id = 1, Name = "Test", IsVip = isVip, City = "X", Email = "x@x.com" },
        Status = OrderStatus.New,
        CreatedAt = DateTime.Now,
        Items = new List<OrderItem>
        {
            new()
            {
                Product = new Product { Id = 1, Name = "P", Category = "C", Price = unitPrice },
                Quantity = quantity,
                UnitPrice = unitPrice
            }
        }
    };

    [Fact]
    public void CalculateDiscount_StandardCustomerSmallAmount_ReturnsZero()
    {
        var order = MakeOrder(isVip: false, unitPrice: 500m);

        var discount = _calc.CalculateDiscount(order);

        Assert.Equal(0m, discount);
    }

    [Fact]
    public void CalculateDiscount_VipCustomerSmallAmount_ReturnsTenPercent()
    {
        var order = MakeOrder(isVip: true, unitPrice: 500m);

        var discount = _calc.CalculateDiscount(order);

        Assert.Equal(50m, discount);
    }

    [Fact]
    public void CalculateDiscount_StandardCustomerHighValue_ReturnsFivePercent()
    {
        var order = MakeOrder(isVip: false, unitPrice: 2000m);

        var discount = _calc.CalculateDiscount(order);

        Assert.Equal(100m, discount);
    }

    [Fact]
    public void CalculateDiscount_VipCustomerHighValue_ReturnsFifteenPercent()
    {
        var order = MakeOrder(isVip: true, unitPrice: 2000m);

        var discount = _calc.CalculateDiscount(order);

        Assert.Equal(300m, discount);
    }

    [Fact]
    public void CalculateDiscount_VipCustomerVeryHighValue_ReturnsTwentyPercent()
    {
        var order = MakeOrder(isVip: true, unitPrice: 6000m);

        var discount = _calc.CalculateDiscount(order);

        Assert.Equal(1200m, discount);
    }

    [Fact]
    public void CalculateDiscount_CapAt25Percent_NeverExceedsMax()
    {
        var order = MakeOrder(isVip: true, unitPrice: 10000m);

        var discount = _calc.CalculateDiscount(order);

        Assert.True(discount <= 10000m * 0.25m);
        Assert.Equal(2000m, discount);
    }

    [Theory]
    [InlineData(false, 999,  0)]
    [InlineData(false, 1001, 50.05)]
    [InlineData(true,  999,  99.9)]
    [InlineData(true,  1001, 150.15)]
    public void CalculateDiscount_ThresholdEdgeCases_ReturnsCorrectDiscount(
        bool isVip, decimal price, decimal expectedDiscount)
    {
        var order = MakeOrder(isVip, price);

        var discount = _calc.CalculateDiscount(order);

        Assert.Equal(expectedDiscount, discount);
    }

    [Fact]
    public void CalculateDiscount_AnyOrder_NeverNegative()
    {
        var order = MakeOrder(isVip: false, unitPrice: 0m);

        var discount = _calc.CalculateDiscount(order);

        Assert.True(discount >= 0m);
    }
}