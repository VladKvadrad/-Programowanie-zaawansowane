using System.Text.Json;

namespace OrderFlow.Console.Services;

public class CurrencyServiceException : Exception
{
    public CurrencyServiceException(string message) : base(message) { }
}

public interface ICurrencyService
{
    Task<decimal?> GetRateAsync(string currencyCode);
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency);
}

public class CurrencyService : ICurrencyService
{
    private readonly HttpClient _http;
    private readonly Dictionary<string, decimal> _cache = new();

    public CurrencyService(HttpClient http)
    {
        _http = http;
    }

    public async Task<decimal?> GetRateAsync(string currencyCode)
    {
        if (currencyCode.ToUpper() == "PLN")
            return 1.0m;

        var key = currencyCode.ToUpper();
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        var url = $"https://api.nbp.pl/api/exchangerates/rates/A/{key}/?format=json";
        var response = await _http.GetAsync(url);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw new CurrencyServiceException($"NBP API error: {(int)response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var rate = doc.RootElement
            .GetProperty("rates")[0]
            .GetProperty("mid")
            .GetDecimal();

        _cache[key] = rate;
        return rate;
    }

    public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency)
    {
        var fromRate = await GetRateAsync(fromCurrency);
        var toRate = await GetRateAsync(toCurrency);

        if (fromRate is null) throw new CurrencyServiceException($"Unknown currency: {fromCurrency}");
        if (toRate is null) throw new CurrencyServiceException($"Unknown currency: {toCurrency}");

        var inPln = amount * fromRate.Value;
        return Math.Round(inPln / toRate.Value, 2);
    }
}