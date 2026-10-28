using Matching.Application.Features.Matching.Queries.GetMatchSessionById;
using Matching.Domain.Entities;
using Matching.Domain.Repositories;
using Matching.UnitTests.Common.Builders;
using NSubstitute;
using Shared;
using Xunit;

namespace Matching.UnitTests.Features;

public class GetMatchSessionByIdHandlerTests
{
    private readonly IMatchingRepository _matchingRepository = Substitute.For<IMatchingRepository>();

    [Fact]
    public async Task Handle_WhenSessionDoesNotExist_ReturnsNotFoundError()
    {
        // Arrange
        const string sessionId = "507f1f77bcf86cd799439011";
        _matchingRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns((MatchSession?)null);

        var handler = new GetMatchSessionByIdHandler(_matchingRepository);
        var query = new GetMatchSessionByIdQuery(sessionId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Fact]
    public async Task Handle_WhenSessionExists_ReturnsMatchSessionDto()
    {
        // Arrange
        var session = new MatchSessionBuilder()
            .WithId("507f1f77bcf86cd799439011")
            .WithDistance(4.5, 12.0)
            .Build();

        _matchingRepository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        var handler = new GetMatchSessionByIdHandler(_matchingRepository);
        var query = new GetMatchSessionByIdQuery(session.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.NotNull(dto);
        Assert.Equal(session.Id, dto.Id);
        Assert.Equal(session.TripId, dto.TripId);
        Assert.Equal(4.5, dto.DistanceKm);
        Assert.Equal(12.0, dto.EstimatedMinutes);
    }
}
