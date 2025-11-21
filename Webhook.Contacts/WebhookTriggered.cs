namespace Webhook.Contacts;

public sealed record WebhookTriggered(Guid SubscriptionId, string EventType, string WebhookUrl, object Data);