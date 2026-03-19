using OrderFlow.Console.Data;
using OrderFlow.Console.Models;

var orders = SampleData.Orders;

Console.WriteLine("=== OrderFlow ===");
Console.WriteLine($"Products: {SampleData.Products.Count}");
Console.WriteLine($"Customers: {SampleData.Customers.Count}");
Console.WriteLine($"Orders: {orders.Count}");

foreach (var order in orders)
{
    Console.WriteLine($"Order #{order.Id} | {order.Customer.Name} | {order.Status} | {order.TotalAmount:C}");
}