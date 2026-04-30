using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Models;
using OrderFlow.Console.Persistence;
using OrderFlow.Console.Services;

await using var db = new OrderFlowContext();
await db.Database.MigrateAsync();
System.Console.WriteLine("  [DB] Migrations applied.");

var seeder = new DatabaseSeeder();
await seeder.SeedAsync(db);

var svc = new OrderDbService();

System.Console.WriteLine("\n========== LAB 4 TASK 1: DbContext ==========");
var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
System.Console.WriteLine("  Applied migrations:");
foreach (var m in appliedMigrations)
    System.Console.WriteLine($"    ✓ {m}");
if (!pendingMigrations.Any())
    System.Console.WriteLine("  No pending migrations.");

System.Console.WriteLine("\n========== LAB 4 TASK 2: CRUD ==========");

await using var db2 = new OrderFlowContext();

System.Console.WriteLine("\n-- Create --");
var firstCustomer = await db2.Customers.FirstAsync();
await svc.CreateOrderAsync(db2, firstCustomer.Id);

System.Console.WriteLine("\n-- Read --");
await svc.ReadOrdersAsync(db2);

System.Console.WriteLine("\n-- Update --");
var newOrder = await db2.Orders.FirstOrDefaultAsync(o => o.Status == OrderStatus.New);
if (newOrder != null)
    await svc.UpdateOrderAsync(db2, newOrder.Id);

System.Console.WriteLine("\n-- Delete (Cancelled order) --");
await svc.DeleteCancelledOrderAsync(db2);

System.Console.WriteLine("\n-- Delete (Customer with orders → Restrict) --");
await svc.TryDeleteCustomerWithOrdersAsync(db2);

System.Console.WriteLine("\n========== LAB 4 TASK 3: Queries ==========");

await using var db3 = new OrderFlowContext();

System.Console.WriteLine();
await svc.Query1VipOrdersAboveThresholdAsync(db3, 500m);

System.Console.WriteLine();
await svc.Query2CustomerRankingAsync(db3);

System.Console.WriteLine();
await svc.Query3AvgByCity(db3);

System.Console.WriteLine();
await svc.Query4NeverOrderedProductsAsync(db3);

System.Console.WriteLine();
await svc.Query5DynamicFilterAsync(db3, OrderStatus.Completed, 500m);

System.Console.WriteLine("\n========== LAB 4 TASK 3: Transactions ==========");
await using var db4 = new OrderFlowContext();

var orderForSuccess = await db4.Orders
    .Include(o => o.Items).ThenInclude(i => i.Product)
    .FirstOrDefaultAsync(o => o.Status == OrderStatus.New);

if (orderForSuccess != null)
{
    System.Console.WriteLine($"\n-- Transaction success: Order #{orderForSuccess.Id} --");
    await svc.ProcessOrderTransactionAsync(db4, orderForSuccess.Id);
}

await using var db5 = new OrderFlowContext();
var productToBreak = await db5.Products.FirstAsync();
productToBreak.Stock = 0;
await db5.SaveChangesAsync();

var orderForFail = await db5.Orders
    .Include(o => o.Items).ThenInclude(i => i.Product)
    .FirstOrDefaultAsync(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Validated);

if (orderForFail != null)
{
    System.Console.WriteLine($"\n-- Transaction rollback: Order #{orderForFail.Id} (stock=0) --");
    await svc.ProcessOrderTransactionAsync(db5, orderForFail.Id);
}