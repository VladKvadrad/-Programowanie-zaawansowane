using System.Net;
using System.Text;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class CurrencyServiceTests
{
    private static string UsdJson(decimal rate) => $$"""
        {
          "table": "A",
          "currency": "dolar amerykański",
          "code": "USD",
          "rates": [{ "no": "001/A/NBP/2026", "effectiveDate": "2026-01-01", "mid": {{rate}} }]
        }
        """;

    private static (CurrencyService Service, TestHttpMessageHandler Handler) MakeService(
        string json, HttpStatusCode code = HttpStatusCode.OK)
    {
        var handler = new TestHttpMessageHandler(_ =>
            new HttpResponseMessage(code)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        var client = new HttpClient(handler);
        var service = new CurrencyService(client);
        return (service, handler);
    }

    [Fact]
    public async Task GetRateAsync_ValidCurrency_ReturnsRate()
    {
        var (svc, _) = MakeService(UsdJson(4.05m));

        var rate = await svc.GetRateAsync("USD");

        Assert.Equal(4.05m, rate);
    }

    [Fact]
    public async Task GetRateAsync_PLN_ReturnsOneWithoutCallingApi()
    {
        var (svc, handler) = MakeService("");

        var rate = await svc.GetRateAsync("PLN");

        Assert.Equal(1.0m, rate);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetRateAsync_UnknownCurrency_ReturnsNull()
    {
        var (svc, _) = MakeService("", HttpStatusCode.NotFound);

        var rate = await svc.GetRateAsync("XYZ");

        Assert.Null(rate);
    }

    [Fact]
    public async Task GetRateAsync_ServerError_ThrowsCurrencyServiceException()
    {
        var (svc, _) = MakeService("", HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<CurrencyServiceException>(
            () => svc.GetRateAsync("USD"));
    }

    [Fact]
    public async Task ConvertAsync_UsdToEur_ReturnsCorrectAmount()
    {
        var handler = new TestHttpMessageHandler(req =>
        {
            var url = req.RequestUri!.ToString();
            var json = url.Contains("USD") ? UsdJson(4.0m) : UsdJson(4.5m);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        var svc = new CurrencyService(new HttpClient(handler));

        var result = await svc.ConvertAsync(100m, "USD", "EUR");

        Assert.Equal(Math.Round(100m * 4.0m / 4.5m, 2), result);
    }

    [Fact]
    public async Task GetRateAsync_CallsCorrectNbpUrl()
    {
        var (svc, handler) = MakeService(UsdJson(4.0m));

        await svc.GetRateAsync("USD");

        var url = handler.Requests[0].RequestUri!.ToString();
        Assert.Contains("/api/exchangerates/rates/A/USD/", url);
    }

    [Fact]
    public async Task GetRateAsync_CalledTwice_ApiCalledOnlyOnce()
    {
        var (svc, handler) = MakeService(UsdJson(4.0m));

        await svc.GetRateAsync("USD");
        await svc.GetRateAsync("USD");

        Assert.Single(handler.Requests);
    }
}