namespace Webhook.Contacts;

public sealed record WebhookDispatched(string EventType, object Data);