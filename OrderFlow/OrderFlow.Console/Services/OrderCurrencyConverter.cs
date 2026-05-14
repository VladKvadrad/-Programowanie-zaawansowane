using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class OrderCurrencyConverter
{
    private readonly ICurrencyService _currency;

    public OrderCurrencyConverter(ICurrencyService currency)
    {
        _currency = currency;
    }

    public async Task<decimal?> ConvertOrderTotalAsync(Order order, string targetCurrency)
    {
        var total = order.TotalAmount;
        return await _currency.ConvertAsync(total, "PLN", targetCurrency);
    }
}