using Microsoft.EntityFrameworkCore;
using Webhook.Api.Data;

namespace Webhook.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task ApplyMigrationAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WebhooksDbContext>();

        await db.Database.MigrateAsync();
    }
}