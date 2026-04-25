namespace RestHW03.Configuration;

public sealed class YahooFinanceOptions
{
    public const string SectionName = "YahooFinance";

    public string BaseUrl { get; init; } = "https://apidojo-yahoo-finance-v1.p.rapidapi.com";

    public string Host { get; init; } = "apidojo-yahoo-finance-v1.p.rapidapi.com";

    public string? ApiKey { get; init; }

    public string GetApiKey()
    {
        if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            return ApiKey;
        }

        var environmentApiKey = Environment.GetEnvironmentVariable("YAHOO_FINANCE_RAPIDAPI_KEY");
        if (!string.IsNullOrWhiteSpace(environmentApiKey))
        {
            return environmentApiKey;
        }

        throw new InvalidOperationException(
            "Yahoo Finance RapidAPI key is missing. Set YahooFinance:ApiKey in configuration or YAHOO_FINANCE_RAPIDAPI_KEY in the environment.");
    }
}
