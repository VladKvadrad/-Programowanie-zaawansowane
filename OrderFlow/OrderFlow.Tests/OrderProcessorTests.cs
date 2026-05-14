using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class OrderProcessorTests
{
    private static List<Order> MakeOrders() => new()
    {
        new Order
        {
            Id = 1,
            Customer = new Customer { Id = 1, Name = "Anna", IsVip = true, City = "Warsaw", Email = "a@a.com" },
            Status = OrderStatus.Completed,
            CreatedAt = DateTime.Now.AddDays(-5),
            Items = new List<OrderItem>
            {
                new() { Product = new Product { Id = 1, Name = "Laptop", Category = "Electronics", Price = 3500m }, Quantity = 1, UnitPrice = 3500m }
            }
        },
        new Order
        {
            Id = 2,
            Customer = new Customer { Id = 2, Name = "Piotr", IsVip = false, City = "Krakow", Email = "p@p.com" },
            Status = OrderStatus.New,
            CreatedAt = DateTime.Now.AddDays(-1),
            Items = new List<OrderItem>
            {
                new() { Product = new Product { Id = 2, Name = "Mouse", Category = "Electronics", Price = 120m }, Quantity = 2, UnitPrice = 120m }
            }
        },
        new Order
        {
            Id = 3,
            Customer = new Customer { Id = 1, Name = "Anna", IsVip = true, City = "Warsaw", Email = "a@a.com" },
            Status = OrderStatus.Cancelled,
            CreatedAt = DateTime.Now.AddDays(-10),
            Items = new List<OrderItem>
            {
                new() { Product = new Product { Id = 1, Name = "Laptop", Category = "Electronics", Price = 3500m }, Quantity = 1, UnitPrice = 3500m }
            }
        }
    };

    [Fact]
    public void Filter_ByCompletedStatus_ReturnsOnlyCompleted()
    {
        var processor = new OrderProcessor(MakeOrders());

        var result = processor.Filter(o => o.Status == OrderStatus.Completed);

        Assert.All(result, o => Assert.Equal(OrderStatus.Completed, o.Status));
        Assert.Single(result);
    }

    [Fact]
    public void Aggregate_SumOfAllOrders_ReturnsCorrectTotal()
    {
        var orders = MakeOrders();
        var processor = new OrderProcessor(orders);
        var expected = orders.Sum(o => o.TotalAmount);

        var result = processor.Aggregate(os => os.Sum(o => o.TotalAmount));

        Assert.Equal(expected, result);
    }
}