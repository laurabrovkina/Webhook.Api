using System.Text.Json;
using MassTransit;
using Webhook.Api.Data;
using Webhook.Api.Models;

namespace Webhook.Api.Services;

public sealed class WebhookTriggeredConsumer : IConsumer<WebhookTriggered>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WebhooksDbContext _dbContext;

    public WebhookTriggeredConsumer(IHttpClientFactory httpClientFactory, WebhooksDbContext dbContext)
    {
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<WebhookTriggered> context)
    {
        using var httpClient = _httpClientFactory.CreateClient();
            
        var payload = new WebhookPayload
        {
            Id = Guid.NewGuid(),
            EventType = context.Message.EventType,
            SubscriptionId = context.Message.SubscriptionId,
            Timestamp = DateTime.UtcNow,
            Data = context.Message.Data
        };
        var jsonPayload = JsonSerializer.Serialize(payload);

        try
        {
            var response = await httpClient.PostAsJsonAsync(context.Message.WebhookUrl, payload);
            response.EnsureSuccessStatusCode();

            var deliveryAttempt = new WebhookDeliverAttempt
            {
                Id = Guid.NewGuid(),
                WebhookSubscriptionId = context.Message.SubscriptionId,
                Payload = jsonPayload,
                ResponseStatusCode = (int)response.StatusCode,
                Success = response.IsSuccessStatusCode,
                Timestamp = DateTime.UtcNow
            };

            _dbContext.WebhookDeliverAttempts.Add(deliveryAttempt);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            var deliveryAttempt = new WebhookDeliverAttempt
            {
                Id = Guid.NewGuid(),
                WebhookSubscriptionId = context.Message.SubscriptionId,
                Payload = jsonPayload,
                ResponseStatusCode = null,
                Success = false,
                Timestamp = DateTime.UtcNow
            };

            _dbContext.WebhookDeliverAttempts.Add(deliveryAttempt);
            await _dbContext.SaveChangesAsync();
        }
    }
}