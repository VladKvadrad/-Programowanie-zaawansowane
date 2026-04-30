using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Persistence;

public class OrderFlowContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options
            .UseSqlite("Data Source=orderflow.db")
            .LogTo(msg =>
            {
                if (msg.Contains("SELECT") || msg.Contains("INSERT") ||
                    msg.Contains("UPDATE") || msg.Contains("DELETE"))
                    System.Console.WriteLine($"  [SQL] {msg.Trim()}");
            }, Microsoft.Extensions.Logging.LogLevel.Information);
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Order>().Ignore(o => o.TotalAmount);
        model.Entity<OrderItem>().Ignore(i => i.TotalPrice);

        model.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        model.Entity<OrderItem>()
            .Property(i => i.UnitPrice)
            .HasPrecision(18, 2);

        model.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<OrderItem>()
            .HasOne(i => i.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        model.Entity<OrderItem>()
            .HasOne(i => i.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<Customer>()
            .HasIndex(c => c.Name)
            .HasDatabaseName("IX_Customer_Name");

        model.Entity<Order>()
            .HasIndex(o => o.Status)
            .HasDatabaseName("IX_Order_Status");
    }
}