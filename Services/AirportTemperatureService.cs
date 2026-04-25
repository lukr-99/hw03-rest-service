using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RestHW03.Services;

public sealed class AirportTemperatureService(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<double> GetTemperatureAsync(string iataCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(iataCode) || iataCode.Length != 3 || !iataCode.All(char.IsLetter))
        {
            throw new ArgumentException("queryAirportTemp must be a three-letter IATA code.");
        }

        var normalizedIataCode = iataCode.Trim().ToUpperInvariant();
        var airport = await LookupAirportAsync(normalizedIataCode, cancellationToken);
        return await LookupCurrentTemperatureAsync(airport.Latitude, airport.Longitude, cancellationToken);
    }

    private async Task<AirportLookupResponse> LookupAirportAsync(string iataCode, CancellationToken cancellationToken)
    {
        var requestUri = $"https://airport-data.com/api/ap_info.json?iata={Uri.EscapeDataString(iataCode)}";
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var airport = await JsonSerializer.DeserializeAsync<AirportLookupResponse>(responseStream, SerializerOptions, cancellationToken);

        if (airport is null || airport.Status != 200 || !string.IsNullOrWhiteSpace(airport.Error))
        {
            throw new ArgumentException($"Airport {iataCode} was not found.");
        }

        return airport;
    }

    private async Task<double> LookupCurrentTemperatureAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var requestUri =
            "https://api.open-meteo.com/v1/forecast" +
            $"?latitude={latitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
            "&current=temperature_2m";

        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var weather = await JsonSerializer.DeserializeAsync<OpenMeteoForecastResponse>(responseStream, SerializerOptions, cancellationToken);

        if (weather?.Current is null || weather.Current.Temperature2m is null)
        {
            throw new InvalidOperationException("Weather provider returned no current temperature.");
        }

        return weather.Current.Temperature2m.Value;
    }

    private sealed class AirportLookupResponse
    {
        [JsonPropertyName("status")]
        public int Status { get; init; }

        [JsonPropertyName("error")]
        public string? Error { get; init; }

        [JsonPropertyName("latitude")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public double Latitude { get; init; }

        [JsonPropertyName("longitude")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public double Longitude { get; init; }
    }

    private sealed class OpenMeteoForecastResponse
    {
        [JsonPropertyName("current")]
        public OpenMeteoCurrentWeather? Current { get; init; }
    }

    private sealed class OpenMeteoCurrentWeather
    {
        [JsonPropertyName("temperature_2m")]
        public double? Temperature2m { get; init; }
    }
}
