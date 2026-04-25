using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RestHW03.Configuration;

namespace RestHW03.Services;

public sealed class StockPriceService(HttpClient httpClient, IOptions<MarketstackOptions> options)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<double> GetPriceAsync(string symbol, string apiKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Length is < 1 or > 4 || !symbol.All(char.IsLetter))
        {
            throw new ArgumentException("queryStockPrice must be a 1-4 letter stock symbol.");
        }

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var baseUrl = options.Value.BaseUrl.TrimEnd('/');
        var requestUri =
            $"{baseUrl}/v1/eod/latest?symbols={Uri.EscapeDataString(normalizedSymbol)}&access_key={Uri.EscapeDataString(apiKey)}";

        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var quote = await JsonSerializer.DeserializeAsync<MarketstackQuoteResponse>(responseStream, SerializerOptions, cancellationToken);

        if (quote?.Error is not null)
        {
            throw new InvalidOperationException(quote.Error.Message);
        }

        var item = quote?.Data?.FirstOrDefault(static x => x.Close is not null);
        if (item?.Close is null)
        {
            throw new InvalidOperationException($"Stock provider returned no quote for {normalizedSymbol}.");
        }

        return item.Close.Value;
    }

    private sealed class MarketstackQuoteResponse
    {
        [JsonPropertyName("data")]
        public List<MarketstackQuoteItem>? Data { get; init; }

        [JsonPropertyName("error")]
        public MarketstackError? Error { get; init; }
    }

    private sealed class MarketstackQuoteItem
    {
        [JsonPropertyName("symbol")]
        public string? Symbol { get; init; }

        [JsonPropertyName("close")]
        public double? Close { get; init; }
    }

    private sealed class MarketstackError
    {
        [JsonPropertyName("message")]
        public string Message { get; init; } = "Unknown stock provider error.";
    }
}
