using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RestHW03.Configuration;

namespace RestHW03.Services;

public sealed class StockPriceService(HttpClient httpClient, IOptions<AlphaVantageOptions> options)
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
            $"{baseUrl}/query?function=GLOBAL_QUOTE&symbol={Uri.EscapeDataString(normalizedSymbol)}&apikey={Uri.EscapeDataString(apiKey)}";

        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var quote = await JsonSerializer.DeserializeAsync<AlphaVantageQuoteResponse>(responseStream, SerializerOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(quote?.ErrorMessage))
        {
            throw new ArgumentException(quote.ErrorMessage);
        }

        if (!string.IsNullOrWhiteSpace(quote?.Information))
        {
            throw new InvalidOperationException(quote.Information);
        }

        if (!string.IsNullOrWhiteSpace(quote?.Note))
        {
            throw new InvalidOperationException(quote.Note);
        }

        if (quote?.GlobalQuote is null || !quote.GlobalQuote.TryGetValue("05. price", out var rawPrice))
        {
            throw new InvalidOperationException($"Stock provider returned no quote for {normalizedSymbol}.");
        }

        if (!double.TryParse(rawPrice, NumberStyles.Float, CultureInfo.InvariantCulture, out var price))
        {
            throw new InvalidOperationException($"Stock provider returned an invalid price for {normalizedSymbol}.");
        }

        return price;
    }

    private sealed class AlphaVantageQuoteResponse
    {
        [JsonPropertyName("Global Quote")]
        public Dictionary<string, string>? GlobalQuote { get; init; }

        [JsonPropertyName("Information")]
        public string? Information { get; init; }

        [JsonPropertyName("Note")]
        public string? Note { get; init; }

        [JsonPropertyName("Error Message")]
        public string? ErrorMessage { get; init; }
    }
}
