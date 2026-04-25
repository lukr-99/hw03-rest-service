namespace RestHW03.Configuration;

public sealed class AlphaVantageOptions
{
    public const string SectionName = "AlphaVantage";

    public string BaseUrl { get; init; } = "https://www.alphavantage.co";

    public string? ApiKey { get; init; }

    public string GetApiKey()
    {
        if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            return ApiKey;
        }

        var environmentApiKey = Environment.GetEnvironmentVariable("ALPHA_VANTAGE_API_KEY");
        if (!string.IsNullOrWhiteSpace(environmentApiKey))
        {
            return environmentApiKey;
        }

        throw new InvalidOperationException(
            "Alpha Vantage API key is missing. Set AlphaVantage:ApiKey in configuration or ALPHA_VANTAGE_API_KEY in the environment.");
    }
}
