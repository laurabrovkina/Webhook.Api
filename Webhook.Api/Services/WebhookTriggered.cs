namespace Webhook.Api.Services;

public sealed record WebhookTriggered(Guid SubscriptionId, string EventType, string WebhookUrl, object Data);