namespace RestHW03.Configuration;

public sealed class MarketstackOptions
{
    public const string SectionName = "Marketstack";

    public string BaseUrl { get; init; } = "https://api.marketstack.com";

    public string? ApiKey { get; init; }

    public string GetApiKey()
    {
        if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            return ApiKey;
        }

        var environmentApiKey = Environment.GetEnvironmentVariable("MARKETSTACK_API_KEY");
        if (!string.IsNullOrWhiteSpace(environmentApiKey))
        {
            return environmentApiKey;
        }

        throw new InvalidOperationException(
            "Marketstack API key is missing. Set Marketstack:ApiKey in configuration or MARKETSTACK_API_KEY in the environment.");
    }
}
