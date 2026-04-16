using OrderFlow.Console.Data;
using OrderFlow.Console.Events;
using OrderFlow.Console.Models;
using OrderFlow.Console.Persistence;
using OrderFlow.Console.Services;
using OrderFlow.Console.Watchers;
using System.Diagnostics;

var orders = SampleData.Orders;
var customers = SampleData.Customers;

System.Console.WriteLine("========== LAB 1: Domain Model ==========");
foreach (var o in orders)
    System.Console.WriteLine($"  Order #{o.Id} | {o.Customer.Name} | {o.Status} | {o.TotalAmount:C}");

System.Console.WriteLine("\n========== LAB 2: Events ==========");
var pipeline = new OrderPipeline();
pipeline.StatusChanged += (_, e) =>
    System.Console.WriteLine($"  [LOG] Order #{e.Order.Id}: {e.OldStatus} → {e.NewStatus}");
pipeline.StatusChanged += (_, e) =>
{
    if (e.NewStatus == OrderStatus.Completed)
        System.Console.WriteLine($"  [EMAIL] Confirmation → {e.Order.Customer.Email}");
};
pipeline.ValidationCompleted += (_, e) =>
    System.Console.WriteLine($"  [VALID] Order #{e.Order.Id}: {(e.IsValid ? "OK" : "FAILED")}");

pipeline.ProcessOrder(orders[0]);

System.Console.WriteLine("\n========== LAB 3 TASK 1: Repository ==========");

var repo = new OrderRepository();
const string jsonPath = "data/orders.json";
const string xmlPath  = "data/orders.xml";

await repo.SaveToJsonAsync(orders, jsonPath);
System.Console.WriteLine($"  Saved {orders.Count} orders → {jsonPath}");

await repo.SaveToXmlAsync(orders, xmlPath);
System.Console.WriteLine($"  Saved {orders.Count} orders → {xmlPath}");

var loadedJson = await repo.LoadFromJsonAsync(jsonPath);
System.Console.WriteLine($"\n  JSON round-trip: {loadedJson.Count} orders loaded");
System.Console.WriteLine($"  Original total:  {orders.Sum(o => o.TotalAmount):C}");
System.Console.WriteLine($"  Loaded total:    {loadedJson.Sum(o => o.TotalAmount):C}");
System.Console.WriteLine($"  Match: {orders.Sum(o => o.TotalAmount) == loadedJson.Sum(o => o.TotalAmount)}");

var loadedXml = await repo.LoadFromXmlAsync(xmlPath);
System.Console.WriteLine($"\n  XML round-trip: {loadedXml.Count} orders loaded");
System.Console.WriteLine($"  Original total: {orders.Sum(o => o.TotalAmount):C}");
System.Console.WriteLine($"  Loaded total:   {loadedXml.Sum(o => o.TotalAmount):C}");
System.Console.WriteLine($"  Match: {orders.Sum(o => o.TotalAmount) == loadedXml.Sum(o => o.TotalAmount)}");

var missing = await repo.LoadFromJsonAsync("data/nonexistent.json");
System.Console.WriteLine($"\n  Missing file → returned {missing.Count} orders (expected 0)");

System.Console.WriteLine("\n========== LAB 3 TASK 2: XML Report ==========");

var builder = new XmlReportBuilder();
const string reportPath = "data/report.xml";

var report = builder.BuildReport(orders);
await builder.SaveReportAsync(report, reportPath);
System.Console.WriteLine($"  Report saved → {reportPath}");
System.Console.WriteLine($"  Preview:\n{report}");

var highValueIds = await builder.FindHighValueOrderIdsAsync(reportPath, 1000m);
System.Console.WriteLine($"\n  Orders with total > 1000:");
foreach (var id in highValueIds)
    System.Console.WriteLine($"    Order #{id}");

System.Console.WriteLine("\n========== LAB 3 TASK 3: InboxWatcher ==========");

const string inboxPath = "inbox";
var watcherPipeline = new OrderPipeline();

watcherPipeline.StatusChanged += (_, e) =>
    System.Console.WriteLine($"  [PIPELINE] Order #{e.Order.Id}: {e.OldStatus} → {e.NewStatus}");
watcherPipeline.ValidationCompleted += (_, e) =>
    System.Console.WriteLine($"  [PIPELINE] Validation #{e.Order.Id}: {(e.IsValid ? "OK" : "FAILED")}");

using var watcher = new InboxWatcher(inboxPath, watcherPipeline);

System.Console.WriteLine("  [DEMO] Dropping test files into inbox/ every 4 seconds...\n");

for (int wave = 1; wave <= 2; wave++)
{
    var testOrders = new List<Order>
    {
        new Order
        {
            Id = 100 + wave,
            Customer = customers[wave % customers.Count],
            Status = OrderStatus.New,
            CreatedAt = DateTime.Now,
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = wave,
                    Product = SampleData.Products[wave % SampleData.Products.Count],
                    Quantity = 1,
                    UnitPrice = SampleData.Products[wave % SampleData.Products.Count].Price
                }
            }
        }
    };

    var inboxFile = Path.Combine(inboxPath, $"import_wave{wave}_{DateTime.Now:HHmmss}.json");
    await repo.SaveToJsonAsync(testOrders, inboxFile);
    System.Console.WriteLine($"  [DEMO] Dropped: {Path.GetFileName(inboxFile)}");

    await Task.Delay(4000);
}

System.Console.WriteLine("\n  [DEMO] Done. Press Enter to exit.");
System.Console.ReadLine();