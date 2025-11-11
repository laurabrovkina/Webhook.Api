using MassTransit;
using Microsoft.EntityFrameworkCore;
using Webhook.Api.Data;

namespace Webhook.Api.Services;

public sealed class WebhookDispatchedConsumer : IConsumer<WebhookDispatched>
{
    private WebhooksDbContext _dbContext;

    public WebhookDispatchedConsumer(WebhooksDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<WebhookDispatched> context)
    {
        var message = context.Message;
        
        var subscriptions = await _dbContext.WebhookSubscriptions
            .AsNoTracking()
            .Where(s => s.EventType.Equals(message.EventType))
            .ToListAsync();

        // foreach (var subscription in subscriptions)
        // {
        //     await context.Publish(new WebhookTriggered(
        //         subscription.Id,
        //         subscription.EventType,
        //         subscription.WebhookUrl,
        //         message.Data));
        // }

        await context.PublishBatch(subscriptions.Select(s =>
            new WebhookTriggered(
                s.Id,
                s.EventType,
                s.WebhookUrl,
                message.Data)));
    }
}