using Microsoft.Extensions.Options;
using RestHW03.Configuration;
using RestHW03.Formatting;
using RestHW03.Services;

var builder = WebApplication.CreateBuilder(args);

var renderPort = Environment.GetEnvironmentVariable("PORT");
var aspnetcoreUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (!string.IsNullOrWhiteSpace(renderPort) && string.IsNullOrWhiteSpace(aspnetcoreUrls))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

builder.Services.Configure<AlphaVantageOptions>(builder.Configuration.GetSection(AlphaVantageOptions.SectionName));
builder.Services.AddSingleton<ExpressionEvaluator>();
builder.Services.AddHttpClient<AirportTemperatureService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("RestHW03/1.0");
});
builder.Services.AddHttpClient<StockPriceService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("RestHW03/1.0");
});

var app = builder.Build();

app.MapGet("/", async Task<IResult> (
    HttpContext context,
    AirportTemperatureService airportTemperatureService,
    StockPriceService stockPriceService,
    ExpressionEvaluator expressionEvaluator,
    IOptions<AlphaVantageOptions> alphaVantageOptions,
    CancellationToken cancellationToken) =>
{
    var airportCode = context.Request.Query["queryAirportTemp"].ToString();
    var stockSymbol = context.Request.Query["queryStockPrice"].ToString();
    var rawExpression = GetRawQueryParameterValue(context.Request, "queryEval");

    var suppliedParameters = CountSuppliedParameters(airportCode, stockSymbol, rawExpression);
    if (suppliedParameters != 1)
    {
        return Results.BadRequest("Exactly one of queryAirportTemp, queryStockPrice, or queryEval must be present.");
    }

    try
    {
        double value;

        if (!string.IsNullOrWhiteSpace(airportCode))
        {
            value = await airportTemperatureService.GetTemperatureAsync(airportCode, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(stockSymbol))
        {
            value = await stockPriceService.GetPriceAsync(
                stockSymbol,
                alphaVantageOptions.Value.GetApiKey(),
                cancellationToken);
        }
        else
        {
            value = expressionEvaluator.Evaluate(rawExpression!);
        }

        return NumericResultWriter.Write(context.Request, value);
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(exception.Message);
    }
    catch (InvalidOperationException exception)
    {
        return Results.Problem(
            detail: exception.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (HttpRequestException exception)
    {
        return Results.Problem(
            detail: exception.Message,
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.Run();

static int CountSuppliedParameters(string airportCode, string stockSymbol, string? rawExpression)
{
    var count = 0;

    if (!string.IsNullOrWhiteSpace(airportCode))
    {
        count++;
    }

    if (!string.IsNullOrWhiteSpace(stockSymbol))
    {
        count++;
    }

    if (!string.IsNullOrWhiteSpace(rawExpression))
    {
        count++;
    }

    return count;
}

static string? GetRawQueryParameterValue(HttpRequest request, string parameterName)
{
    var rawQuery = request.QueryString.Value;
    if (string.IsNullOrWhiteSpace(rawQuery))
    {
        return null;
    }

    foreach (var pair in rawQuery.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
        var parts = pair.Split('=', 2);
        var key = Uri.UnescapeDataString(parts[0]);
        if (!string.Equals(key, parameterName, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (parts.Length == 1)
        {
            return string.Empty;
        }

        return Uri.UnescapeDataString(parts[1].Replace("+", "%2B", StringComparison.Ordinal));
    }

    return null;
}
