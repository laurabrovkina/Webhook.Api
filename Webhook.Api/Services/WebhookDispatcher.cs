using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Webhook.Api.Data;
using Webhook.Api.Models;
using Webhook.Api.OpenTelemetry;

namespace Webhook.Api.Services;

public sealed class WebhookDispatcher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WebhooksDbContext _dbContext;
    private readonly Channel<WebhookDispatch> _webhooksChannel;

    public WebhookDispatcher(
        Channel<WebhookDispatch> webhooksChannel,
        IHttpClientFactory httpClientFactory,
        WebhooksDbContext dbContext)
    {
        _webhooksChannel = webhooksChannel;
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
    }

    public async Task DispatchAsync<T>(string eventType, T data)
        where T : notnull
    {
        await _webhooksChannel.Writer.WriteAsync(new WebhookDispatch(eventType, data, activity?.Id));
    }
    public async Task ProcessAsync<T>(string eventType, T data)
    {
        var subscriptions = await _dbContext.WebhookSubscriptions
            .AsNoTracking()
            .Where(s => s.EventType.Equals(eventType))
            .ToListAsync();

        foreach (var subscription in subscriptions)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            
            var payload = new WebhookPayload<T>
            {
                Id = Guid.NewGuid(),
                EventType = subscription.EventType,
                SubscriptionId = subscription.Id,
                Timestamp = DateTime.UtcNow,
                Data = data
            };
            var jsonPayload = JsonSerializer.Serialize(payload);

            try
            {
                var response = await httpClient.PostAsJsonAsync(subscription.WebhookUrl, payload);

                var deliveryAttempt = new WebhookDeliverAttempt
                {
                    Id = Guid.NewGuid(),
                    WebhookSubscriptionId = subscription.Id,
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
                    WebhookSubscriptionId = subscription.Id,
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
}