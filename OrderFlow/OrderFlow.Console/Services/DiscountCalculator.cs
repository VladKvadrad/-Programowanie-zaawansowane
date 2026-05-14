using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class DiscountCalculator
{
    private const decimal VipDiscountRate = 0.10m;
    private const decimal HighValueDiscountRate = 0.05m;
    private const decimal VipHighValueBonusRate = 0.05m;
    private const decimal HighValueThreshold = 1000m;
    private const decimal VipHighValueThreshold = 5000m;
    private const decimal MaxDiscountRate = 0.25m;

    public decimal CalculateDiscount(Order order)
    {
        var total = order.TotalAmount;
        var rate = CalculateRate(order.Customer.IsVip, total);
        return Math.Round(total * rate, 2);
    }

    private decimal CalculateRate(bool isVip, decimal total)
    {
        var rate = 0m;

        if (isVip)
            rate += VipDiscountRate;

        if (total > HighValueThreshold)
            rate += HighValueDiscountRate;

        if (isVip && total > VipHighValueThreshold)
            rate += VipHighValueBonusRate;

        return Math.Min(rate, MaxDiscountRate);
    }
}