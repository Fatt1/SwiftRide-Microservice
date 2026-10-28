using Matching.Application.Features.Matching.Queries.GetAllDriverLocations;
using Matching.Domain.Entities;
using Matching.Domain.Repositories;
using Matching.UnitTests.Common.Builders;
using NSubstitute;
using Xunit;

namespace Matching.UnitTests.Features;

public class GetAllDriverLocationsHandlerTests
{
    private readonly IDriverLocationRepository _driverLocationRepository = Substitute.For<IDriverLocationRepository>();

    [Fact]
    public async Task Handle_WhenLocationsExist_ReturnsAllDriverLocationDtos()
    {
        // Arrange
        var locations = new List<DriverLocation>
        {
            new DriverLocationBuilder().WithFullName("Driver 1").WithAvailability(true).Build(),
            new DriverLocationBuilder().WithFullName("Driver 2").WithAvailability(false).Build()
        };

        _driverLocationRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(locations);

        var handler = new GetAllDriverLocationsHandler(_driverLocationRepository);
        var query = new GetAllDriverLocationsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("Driver 1", result.Value[0].FullName);
        Assert.True(result.Value[0].IsAvailable);
        Assert.Equal("Driver 2", result.Value[1].FullName);
        Assert.False(result.Value[1].IsAvailable);
    }
}
