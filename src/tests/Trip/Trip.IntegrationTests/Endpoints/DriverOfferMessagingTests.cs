using EventBus.Messages.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trip.Domain.Repositories;
using Trip.Infrastructure.Consumers;
using Trip.Infrastructure.Persistence;
using Trip.Infrastructure.Repositories;
using Trip.IntegrationTests.Common.Builders;
using Trip.IntegrationTests.Fixtures;
using Xunit;
namespace Trip.IntegrationTests.Endpoints;
public class OfferRejectionCapture
{
    public TaskCompletionSource<DriverOfferRejectedEvent> Received { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
public sealed class CaptureRejectedOfferConsumer(OfferRejectionCapture capture) : IConsumer<DriverOfferRejectedEvent>
{
    public Task Consume(ConsumeContext<DriverOfferRejectedEvent> context)
    {
        capture.Received.TrySetResult(context.Message);
        return Task.CompletedTask;
    }
}
public class DriverOfferMessagingTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task PublishOffer_WithSqlOutbox_StoresInvitationAndReleasesInvalidProposalThroughEvent()
    {
        // Arrange: real SQL, production consumer/outbox, in-memory broker transport.
        await fixture.ResetDatabaseAsync(); using var db = fixture.GetDbContext();
        var trip = new TripBuilder().Build(); db.Trips.Add(trip); await db.SaveChangesAsync();
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton<OfferRejectionCapture>();
        services.AddDbContext<TripDbContext>(o => o.UseSqlServer(db.Database.GetConnectionString()));
        services.AddScoped<ITripRepository, TripRepository>();
        services.AddMassTransit(x =>
        {
            x.AddConsumer<DriverFoundConsumer>(); x.AddConsumer<CaptureRejectedOfferConsumer>();
            x.AddEntityFrameworkOutbox<TripDbContext>(o => { o.UseSqlServer(); o.UseBusOutbox(); });
            x.AddConfigureEndpointsCallback((context, _, endpoint) => endpoint.UseEntityFrameworkOutbox<TripDbContext>(context));
            x.UsingInMemory((context, config) => config.ConfigureEndpoints(context));
        });
        await using var provider = services.BuildServiceProvider(); var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();
        try
        {
            var offer = new DriverFoundEvent { TripId = trip.Id, MatchSessionId = trip.MatchSessionId!, DriverId = Guid.NewGuid(),
                MaxAllowedRejections = 1 };
            // Act
            await bus.Publish(offer);
            var deadline = DateTime.UtcNow.AddSeconds(15);
            do
            {
                await db.Entry(trip).ReloadAsync();
                if (trip.OfferedDriverId == offer.DriverId) break;
                await Task.Delay(50);
            } while (DateTime.UtcNow < deadline);
            // Assert
            Assert.Equal(offer.DriverId, trip.OfferedDriverId); Assert.Null(trip.DriverId);
            // Arrange: cancellation raced with a pending invitation.
            trip.Cancel(); await db.SaveChangesAsync();
            // Act
            await bus.Publish(offer);
            var rejected = await provider.GetRequiredService<OfferRejectionCapture>().Received.Task.WaitAsync(TimeSpan.FromSeconds(15));
            await db.Entry(trip).ReloadAsync();
            // Assert
            Assert.Equal(offer.DriverId, rejected.DriverId); Assert.Null(trip.OfferedDriverId);
            Assert.Equal(Trip.Domain.Enums.TripStatus.Cancelled, trip.Status);
        }
        finally { await bus.StopAsync(); }
    }
}
