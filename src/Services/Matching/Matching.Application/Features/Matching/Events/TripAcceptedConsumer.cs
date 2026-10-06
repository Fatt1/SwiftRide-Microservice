using EventBus.Messages.Events;
using MassTransit;

namespace Matching.Application.Features.Matching.Events;

public class TripAcceptedConsumer : IConsumer<TripAcceptedEvent>
{
    // Nhớ khi log
    // Cập nhật trạng thái của chuyến đi và trạng thái của tài xế trong cơ sở dữ liệu
    public Task Consume(ConsumeContext<TripAcceptedEvent> context)
    {
        throw new NotImplementedException();
    }
}
