using EventBus.Messages.Events;
using MassTransit;
using Trip.Domain.Repositories;
namespace Trip.Infrastructure.Consumers;

public sealed class DriverFoundConsumer(ITripRepository trips) : IConsumer<DriverFoundEvent>
{
    public async Task Consume(ConsumeContext<DriverFoundEvent> context)
    {
        var offer = context.Message;
        var trip = await trips.FindByIdAsync(offer.TripId);

        if (trip is null)
            return;

        // Trip còn nhận được lời mời hợp lệ thì lưu tài xế được mời.
        if (trip.SetDriverOffer(offer.MatchSessionId, offer.DriverId, offer.MaxAllowedRejections))
        {
            await trips.Update(trip);
            await trips.SaveChangesAsync();
        }
;
    }
}
