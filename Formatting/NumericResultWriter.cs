using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;

namespace RestHW03.Formatting;

public static class NumericResultWriter
{
    public static IResult Write(HttpRequest request, double value)
    {
        var preferredXmlContentType = GetPreferredXmlContentType(request);
        if (preferredXmlContentType is not null)
        {
            var xml = new XElement(
                "result",
                value.ToString("G17", CultureInfo.InvariantCulture));

            return Results.Text(xml.ToString(SaveOptions.DisableFormatting), preferredXmlContentType);
        }

        return Results.Text(
            JsonSerializer.Serialize(value),
            "application/json");
    }

    private static string? GetPreferredXmlContentType(HttpRequest request)
    {
        var accept = request.Headers.Accept.ToString();
        if (accept.Contains("application/xml", StringComparison.OrdinalIgnoreCase))
        {
            return "application/xml";
        }

        if (accept.Contains("text/xml", StringComparison.OrdinalIgnoreCase))
        {
            return "text/xml";
        }

        return null;
    }
}
