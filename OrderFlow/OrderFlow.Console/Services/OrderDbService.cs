using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Models;
using OrderFlow.Console.Persistence;

namespace OrderFlow.Console.Services;

public class OrderDbService
{

    public async Task CreateOrderAsync(OrderFlowContext db, int customerId)
    {
        var product1 = await db.Products.FirstAsync(p => p.Name == "Mouse");
        var product2 = await db.Products.FirstAsync(p => p.Name == "C# in Depth");

        var order = new Order
        {
            CustomerId = customerId,
            Status = OrderStatus.New,
            CreatedAt = DateTime.Now,
            Notes = "Created via CRUD demo",
            Items = new List<OrderItem>
            {
                new() { ProductId = product1.Id, Quantity = 2, UnitPrice = product1.Price },
                new() { ProductId = product2.Id, Quantity = 1, UnitPrice = product2.Price },
            }
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        System.Console.WriteLine($"  [CREATE] Added Order #{order.Id} with {order.Items.Count} items for Customer #{customerId}");
    }

    public async Task ReadOrdersAsync(OrderFlowContext db)
    {
        var orders = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .OrderBy(o => o.Id)
            .ToListAsync();

        System.Console.WriteLine($"  [READ] {orders.Count} orders loaded:");
        foreach (var o in orders)
        {
            var total = o.Items.Sum(i => i.UnitPrice * i.Quantity);
            System.Console.WriteLine($"    #{o.Id} | {o.Customer.Name} | {o.Status} | {total:C} | {o.Items.Count} items");
        }
    }

    public async Task UpdateOrderAsync(OrderFlowContext db, int orderId)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) { System.Console.WriteLine("  [UPDATE] Order not found"); return; }

        order.Status = OrderStatus.Processing;
        order.Notes = $"Updated at {DateTime.Now:HH:mm:ss}";
        await db.SaveChangesAsync();
        System.Console.WriteLine($"  [UPDATE] Order #{order.Id} → {order.Status}, Notes: {order.Notes}");
    }

    public async Task DeleteCancelledOrderAsync(OrderFlowContext db)
    {
        var cancelled = await db.Orders
            .Where(o => o.Status == OrderStatus.Cancelled)
            .FirstOrDefaultAsync();

        if (cancelled is null) { System.Console.WriteLine("  [DELETE] No cancelled orders found"); return; }

        db.Orders.Remove(cancelled);
        await db.SaveChangesAsync();
        System.Console.WriteLine($"  [DELETE] Deleted Order #{cancelled.Id} (Cancelled)");
    }

    public async Task TryDeleteCustomerWithOrdersAsync(OrderFlowContext db)
    {
        var customer = await db.Customers
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Orders.Any());

        if (customer is null) return;

        try
        {
            db.Customers.Remove(customer);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  [DELETE] Cannot delete Customer #{customer.Id} — {ex.InnerException?.Message ?? ex.Message}");
            db.ChangeTracker.Clear();
        }
    }

    public async Task Query1VipOrdersAboveThresholdAsync(OrderFlowContext db, decimal threshold)
    {
        var result = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Where(o => o.Customer.IsVip)
            .ToListAsync();

        var filtered = result
            .Where(o => o.Items.Sum(i => i.UnitPrice * i.Quantity) > threshold)
            .Select(o => new
            {
                o.Id,
                o.Customer.Name,
                Total = o.Items.Sum(i => i.UnitPrice * i.Quantity)
            });

        System.Console.WriteLine($"  [Q1] VIP orders above {threshold:C}:");
        foreach (var r in filtered)
            System.Console.WriteLine($"    Order #{r.Id} | {r.Name} | {r.Total:C}");
    }

    public async Task Query2CustomerRankingAsync(OrderFlowContext db)
    {
        var result = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .ToListAsync();

        var ranking = result
            .GroupBy(o => o.Customer.Name)
            .Select(g => new
            {
                Name = g.Key,
                Total = g.SelectMany(o => o.Items).Sum(i => i.UnitPrice * i.Quantity),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Total);

        System.Console.WriteLine("  [Q2] Customer ranking by total spend:");
        foreach (var r in ranking)
            System.Console.WriteLine($"    {r.Name}: {r.Total:C} ({r.Count} orders)");
    }

    public async Task Query3AvgByCity(OrderFlowContext db)
    {
        var result = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .ToListAsync();

        var byCity = result
            .GroupBy(o => o.Customer.City)
            .Select(g => new
            {
                City = g.Key,
                Avg = g.SelectMany(o => o.Items).Sum(i => i.UnitPrice * i.Quantity) / g.Count()
            });

        System.Console.WriteLine("  [Q3] Average order value per city:");
        foreach (var r in byCity)
            System.Console.WriteLine($"    {r.City}: {r.Avg:C}");
    }

    public async Task Query4NeverOrderedProductsAsync(OrderFlowContext db)
    {
        var result = await db.Products
            .Where(p => !p.OrderItems.Any())
            .ToListAsync();

        System.Console.WriteLine("  [Q4] Products never ordered:");
        if (result.Count == 0) System.Console.WriteLine("    (none)");
        foreach (var p in result)
            System.Console.WriteLine($"    {p.Name} ({p.Category})");
    }

    public async Task Query5DynamicFilterAsync(OrderFlowContext db, OrderStatus? status, decimal? minAmount)
    {
        var query = db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var result = await query.ToListAsync();

        if (minAmount.HasValue)
            result = result.Where(o => o.Items.Sum(i => i.UnitPrice * i.Quantity) >= minAmount.Value).ToList();

        System.Console.WriteLine($"  [Q5] Dynamic filter — status={status}, minAmount={minAmount:C}:");
        foreach (var o in result)
            System.Console.WriteLine($"    #{o.Id} | {o.Customer.Name} | {o.Status} | {o.Items.Sum(i => i.UnitPrice * i.Quantity):C}");
    }

    public async Task ProcessOrderTransactionAsync(OrderFlowContext db, int orderId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var order = await db.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order is null) throw new Exception($"Order #{orderId} not found");
            if (order.Status != OrderStatus.New)
                throw new Exception($"Order #{orderId} is not in New status");

            order.Status = OrderStatus.Processing;
            await db.SaveChangesAsync();

            foreach (var item in order.Items)
            {
                if (item.Product.Stock < item.Quantity)
                    throw new Exception($"Insufficient stock for '{item.Product.Name}': need {item.Quantity}, have {item.Product.Stock}");

                item.Product.Stock -= item.Quantity;
            }

            order.Status = OrderStatus.Completed;
            order.Notes = $"Processed at {DateTime.Now:HH:mm:ss}";
            await db.SaveChangesAsync();

            await tx.CommitAsync();
            System.Console.WriteLine($"  [TX] Order #{orderId} → Completed. Stock updated.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            System.Console.WriteLine($"  [TX] ROLLBACK Order #{orderId}: {ex.Message}");
            db.ChangeTracker.Clear();
        }
    }
}