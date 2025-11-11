namespace Webhook.Api.Services;

public sealed record WebhookDispatched(string EventType, object Data);