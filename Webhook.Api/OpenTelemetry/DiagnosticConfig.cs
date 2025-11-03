using System.Diagnostics;

namespace Webhook.Api.OpenTelemetry;

public static class DiagnosticConfig
{
    public static readonly ActivitySource Source = new("webhook-api");
}