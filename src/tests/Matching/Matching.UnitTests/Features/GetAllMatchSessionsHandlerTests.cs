using Matching.Application.Features.Matching.Queries.GetAllMatchSessions;
using Matching.Domain.Entities;
using Matching.Domain.Repositories;
using Matching.UnitTests.Common.Builders;
using NSubstitute;
using Xunit;

namespace Matching.UnitTests.Features;

public class GetAllMatchSessionsHandlerTests
{
    private readonly IMatchingRepository _matchingRepository = Substitute.For<IMatchingRepository>();

    [Fact]
    public async Task Handle_WhenSessionsExist_ReturnsAllSessionDtos()
    {
        // Arrange
        var sessions = new List<MatchSession>
        {
            new MatchSessionBuilder().WithId("507f1f77bcf86cd799439011").Build(),
            new MatchSessionBuilder().WithId("507f1f77bcf86cd799439012").Build()
        };

        _matchingRepository.GetAllMatchingSessionAsync(Arg.Any<CancellationToken>())
            .Returns(sessions);

        var handler = new GetAllMatchSessionsHandler(_matchingRepository);
        var query = new GetAllMatchSessionsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("507f1f77bcf86cd799439011", result.Value[0].Id);
        Assert.Equal("507f1f77bcf86cd799439012", result.Value[1].Id);
    }

    [Fact]
    public async Task Handle_WhenNoSessionsExist_ReturnsEmptyList()
    {
        // Arrange
        _matchingRepository.GetAllMatchingSessionAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetAllMatchSessionsHandler(_matchingRepository);
        var query = new GetAllMatchSessionsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
