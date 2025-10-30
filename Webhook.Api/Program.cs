using Webhook.Api.Models;
using Webhook.Api.Repositories;
using Webhook.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSingleton<InMemoryOrderRepository>();
builder.Services.AddSingleton<InMemoryWebhookSubscriptionRepository>();

builder.Services.AddHttpClient<WebhookDispatcher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("openapi/v1.json", "Webhook.Api");
    });
}

app.UseHttpsRedirection();

// Register webhook
app.MapPost("webhooks/subscriptions", (
    CreateWebhookRequest request,
    InMemoryWebhookSubscriptionRepository subscriptionRepository) =>
    {
        var subscription = new WebhookSubscription(
            Guid.NewGuid(),
            request.EventType,
            request.WebhookUrl,
            DateTime.UtcNow
        );
        
        subscriptionRepository.Add(subscription);

        return Results.Ok(subscription);
    });

// Create an order
app.MapPost("/orders", async (
    CreateOrderRequest request,
    InMemoryOrderRepository orderRepository,
    WebhookDispatcher webhookDispatcher) =>
    {
        var order = new Order(Guid.NewGuid(), request.CustomerName, request.Amount, DateTime.UtcNow);

        orderRepository.Add(order);

        await webhookDispatcher.DispatchAsync("order.created", order);

        return Results.Ok(order);
    })
    .WithTags("Orders");

// Get all orders
app.MapGet("/orders", (InMemoryOrderRepository orderRepository) =>
    {
        return Results.Ok(orderRepository.GetAll());
    })
    .WithTags("Orders");

app.Run();