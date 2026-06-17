using System.Text.RegularExpressions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Api.Telemetry;

public partial class PiiTelemetryProcessor(ITelemetryProcessor next) : ITelemetryProcessor
{
    private static readonly string[] SensitivePropertyKeys =
        ["email", "name", "postcode", "firstname", "lastname", "surname", "dob", "dateofbirth", "address", "phone"];

    public void Process(ITelemetry item)
    {
        if (item is RequestTelemetry request && request.Url is not null)
        {
            var scrubbed = ScrubText(request.Url.ToString());
            if (Uri.TryCreate(scrubbed, UriKind.Absolute, out var clean))
            {
                request.Url = clean;
            }
        }

        if (item is ISupportProperties { Properties: var props })
        {
            foreach (var key in props.Keys.ToList())
            {
                if (SensitivePropertyKeys.Any(s => key.Contains(s, StringComparison.OrdinalIgnoreCase)))
                {
                    props[key] = "[redacted]";
                }
                else
                {
                    props[key] = ScrubText(props[key]);
                }
            }
        }

        next.Process(item);
    }

    private static string ScrubText(string value)
    {
        value = EmailRegex().Replace(value, "[email]");
        value = UkPostcodeRegex().Replace(value, "[postcode]");
        return value;
    }

    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b[A-Z]{1,2}[0-9][0-9A-Z]?\s?[0-9][A-Z]{2}\b", RegexOptions.IgnoreCase)]
    private static partial Regex UkPostcodeRegex();
}