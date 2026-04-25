using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RestHW03.Configuration;

namespace RestHW03.Services;

public sealed class StockPriceService(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<double> GetPriceAsync(string symbol, YahooFinanceOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Length is < 1 or > 4 || !symbol.All(char.IsLetter))
        {
            throw new ArgumentException("queryStockPrice must be a 1-4 letter stock symbol.");
        }

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var baseUrl = options.BaseUrl.TrimEnd('/');
        var requestUri =
            $"{baseUrl}/market/v2/get-quotes?region=US&symbols={Uri.EscapeDataString(normalizedSymbol)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add("X-RapidAPI-Key", options.GetApiKey());
        request.Headers.Add("X-RapidAPI-Host", options.Host);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var quote = await JsonSerializer.DeserializeAsync<YahooFinanceQuoteResponse>(responseStream, SerializerOptions, cancellationToken);

        var item = quote?.QuoteResponse?.Result?.FirstOrDefault(static x => x.RegularMarketPrice is not null);
        if (item?.RegularMarketPrice is null)
        {
            throw new InvalidOperationException($"Stock provider returned no quote for {normalizedSymbol}.");
        }

        return item.RegularMarketPrice.Value;
    }

    private sealed class YahooFinanceQuoteResponse
    {
        [JsonPropertyName("quoteResponse")]
        public YahooFinanceQuoteEnvelope? QuoteResponse { get; init; }
    }

    private sealed class YahooFinanceQuoteEnvelope
    {
        [JsonPropertyName("result")]
        public List<YahooFinanceQuoteItem>? Result { get; init; }
    }

    private sealed class YahooFinanceQuoteItem
    {
        [JsonPropertyName("symbol")]
        public string? Symbol { get; init; }

        [JsonPropertyName("regularMarketPrice")]
        public double? RegularMarketPrice { get; init; }
    }
}
