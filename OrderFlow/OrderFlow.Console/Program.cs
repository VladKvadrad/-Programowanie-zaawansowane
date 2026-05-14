using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Models;
using OrderFlow.Console.Persistence;
using OrderFlow.Console.Services;

await using var db = new OrderFlowContext();
await db.Database.MigrateAsync();

var seeder = new DatabaseSeeder();
await seeder.SeedAsync(db);

var svc = new OrderDbService();

System.Console.WriteLine("========== OrderFlow — Lab 1-4 summary ==========");
var orders = await db.Orders
    .Include(o => o.Customer)
    .Include(o => o.Items).ThenInclude(i => i.Product)
    .ToListAsync();

foreach (var o in orders)
    System.Console.WriteLine($"  #{o.Id} | {o.Customer.Name} | {o.Status} | {o.Items.Sum(i => i.UnitPrice * i.Quantity):C}");

System.Console.WriteLine("\n========== LAB 5: CurrencyService (NBP API) ==========");

var httpClient = new HttpClient();
var currencyService = new CurrencyService(httpClient);
var converter = new OrderCurrencyConverter(currencyService);

var sampleOrders = orders.Take(3).ToList();

foreach (var order in sampleOrders)
{
    var total = order.Items.Sum(i => i.UnitPrice * i.Quantity);
    try
    {
        var usd = await converter.ConvertOrderTotalAsync(order, "USD");
        var eur = await converter.ConvertOrderTotalAsync(order, "EUR");
        System.Console.WriteLine($"  Order #{order.Id} | PLN {total:F2} | USD {usd:F2} | EUR {eur:F2}");
    }
    catch (Exception ex)
    {
        System.Console.WriteLine($"  Order #{order.Id} | PLN {total:F2} | [API error: {ex.Message}]");
    }
}

System.Console.WriteLine("\n========== LAB 5: DiscountCalculator ==========");

var calc = new DiscountCalculator();
foreach (var order in sampleOrders)
{
    var discount = calc.CalculateDiscount(order);
    var total = order.Items.Sum(i => i.UnitPrice * i.Quantity);
    System.Console.WriteLine($"  Order #{order.Id} | {order.Customer.Name} {(order.Customer.IsVip ? "[VIP]" : "")} | Total: {total:C} | Discount: {discount:C}");
}