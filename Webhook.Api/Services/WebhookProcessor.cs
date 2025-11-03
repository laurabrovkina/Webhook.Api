using System.Diagnostics;
using System.Threading.Channels;
using Webhook.Api.OpenTelemetry;

namespace Webhook.Api.Services;

public sealed class WebhookProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory; 
    private readonly Channel<WebhookDispatch> _webhooksChannel;

    public WebhookProcessor(
        IServiceScopeFactory serviceScopeFactory,
        Channel<WebhookDispatch> webhooksChannel)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _webhooksChannel = webhooksChannel;
    }
        
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (WebhookDispatch dispatch in _webhooksChannel.Reader.ReadAllAsync(stoppingToken))
        {
            using Activity? activity = DiagnosticConfig.Source.StartActivity(
                $"{dispatch.EventType} process webhook",
                ActivityKind.Internal,
                parentId: dispatch.ParentActivityId);
            
            using var scope = _serviceScopeFactory.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<WebhookDispatcher>();
            await dispatcher.ProcessAsync(dispatch.EventType, dispatch.Data);
        }
    }
}