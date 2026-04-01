using OrderFlow.Console.Data;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

var orders = SampleData.Orders;
var customers = SampleData.Customers;

Console.WriteLine("========== TASK 1: Domain Model ==========");
foreach (var order in orders)
    Console.WriteLine($"Order #{order.Id} | {order.Customer.Name} | {order.Status} | {order.TotalAmount:C}");

Console.WriteLine("\n========== TASK 2: Validation ==========");
var validator = new OrderValidator();

var validOrder = orders[0];
var (isValid, errors) = validator.ValidateAll(validOrder);
Console.WriteLine($"\nOrder #{validOrder.Id} — {(isValid ? "VALID" : "INVALID")}");
if (!isValid) errors.ForEach(e => Console.WriteLine($"  [ERROR] {e}"));

var invalidOrder = new Order
{
    Id = 99,
    Customer = customers[0],
    Status = OrderStatus.Cancelled,
    CreatedAt = DateTime.Now.AddDays(5),
    Items = new List<OrderItem>()
};
var (isValid2, errors2) = validator.ValidateAll(invalidOrder);
Console.WriteLine($"\nOrder #{invalidOrder.Id} — {(isValid2 ? "VALID" : "INVALID")}");
if (!isValid2) errors2.ForEach(e => Console.WriteLine($"  [ERROR] {e}"));

Console.WriteLine("\n========== TASK 3: Action, Func, Predicate ==========");
var processor = new OrderProcessor(orders);

Console.WriteLine("\n-- High value orders (> 1000) --");
processor.Filter(o => o.TotalAmount > 1000)
         .ForEach(o => Console.WriteLine($"  #{o.Id} {o.Customer.Name} {o.TotalAmount:C}"));

Console.WriteLine("\n-- VIP customer orders --");
processor.Filter(o => o.Customer.IsVip)
         .ForEach(o => Console.WriteLine($"  #{o.Id} {o.Customer.Name} {o.TotalAmount:C}"));

Console.WriteLine("\n-- Completed orders --");
processor.Filter(o => o.Status == OrderStatus.Completed)
         .ForEach(o => Console.WriteLine($"  #{o.Id} {o.Customer.Name} {o.TotalAmount:C}"));

Console.WriteLine("\n-- Action: print all orders --");
processor.ForEach(o => Console.WriteLine($"  #{o.Id} | {o.Status} | {o.TotalAmount:C}"));

Console.WriteLine("\n-- Action: mark New orders as Validated --");
processor.ForEach(o =>
{
    if (o.Status == OrderStatus.New)
    {
        o.Status = OrderStatus.Validated;
        Console.WriteLine($"  Order #{o.Id} status changed to Validated");
    }
});

Console.WriteLine("\n-- Func projection to anonymous type --");
var projected = processor.Project(o => new
{
    Id = o.Id,
    Customer = o.Customer.Name,
    Total = o.TotalAmount,
    ItemCount = o.Items.Count
});
projected.ForEach(p => Console.WriteLine($"  #{p.Id} {p.Customer} | {p.ItemCount} items | {p.Total:C}"));

Console.WriteLine("\n-- Aggregations --");
Console.WriteLine($"  Sum:     {processor.Aggregate(os => os.Sum(o => o.TotalAmount)):C}");
Console.WriteLine($"  Average: {processor.Aggregate(os => os.Average(o => o.TotalAmount)):C}");
Console.WriteLine($"  Max:     {processor.Aggregate(os => os.Max(o => o.TotalAmount)):C}");

Console.WriteLine("\n-- Chain: top 3 high-value non-cancelled orders --");
processor.ProcessTopN(
    filter:  o => o.Status != OrderStatus.Cancelled,
    sortKey: o => o.TotalAmount,
    topN:    3,
    print:   o => Console.WriteLine($"  #{o.Id} {o.Customer.Name} | {o.Status} | {o.TotalAmount:C}")
);

LinqQueries.RunAll(orders, customers);
