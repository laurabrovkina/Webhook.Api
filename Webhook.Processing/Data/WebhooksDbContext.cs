using Microsoft.EntityFrameworkCore;
using Webhook.Processing.Models;

namespace Webhook.Processing.Data;

public sealed class WebhooksDbContext : DbContext
{
    public WebhooksDbContext(DbContextOptions<WebhooksDbContext> options) : base(options)
    {
    }

    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; }
    public DbSet<WebhookDeliverAttempt> WebhookDeliverAttempts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WebhookSubscription>(builder =>
        {
            builder.ToTable("subscriptions", "webhooks");
            builder.HasKey(o => o.Id);
        });
        
        modelBuilder.Entity<WebhookDeliverAttempt>(builder =>
        {
            builder.ToTable("delivery_attempts", "webhooks");
            builder.HasKey(o => o.Id);

            builder.HasOne<WebhookSubscription>()
                .WithMany()
                .HasForeignKey(d => d.WebhookSubscriptionId);
        });
    }
}