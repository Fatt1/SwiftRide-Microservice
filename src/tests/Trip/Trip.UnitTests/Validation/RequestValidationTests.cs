using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shared;
using Trip.Application.DTOs;
using Trip.Application.Extensions;
using Trip.Application.Features.Trips.Commands.CreateTrip;
using Trip.Application.Features.Trips.Commands.DriverAccept;
using Trip.Application.Interfaces;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;
using Trip.UnitTests.Common.Builders;
using Xunit;
namespace Trip.UnitTests.Validation;
public class RequestValidationTests
{
    [Fact]
    public async Task Send_InvalidCreateRequest_StopsBeforeRoutingOrPersistence()
    {
        // Arrange: use the actual application DI registration and MediatR pipeline.
        var services = new ServiceCollection(); services.AddLogging(); services.AddTripApplication();
        var repo = Substitute.For<ITripRepository>(); var pricing = Substitute.For<IPricingGrpcService>();
        services.AddSingleton(repo); services.AddSingleton(pricing);
        await using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var command = new CreateTripCommand(Guid.Empty, "", new string('x', 501), double.NaN, 181, 91, double.PositiveInfinity);
        // Act
        var result = await sender.Send(command);
        // Assert
        var error = Assert.IsType<ValidationError>(result.Error);
        Assert.Contains(error.Errors, e => e is FieldError f && f.Field == "PickupLat");
        Assert.Contains(error.Errors, e => e is FieldError f && f.Field == "RiderId");
        await repo.DidNotReceive().CreateAsync(Arg.Any<TripEntity>());
        Assert.Empty(pricing.ReceivedCalls());
    }
    [Fact]
    public async Task Validate_MissingDriverId_RejectsWithoutReadingDatabase()
    {
        // Arrange
        var repo = Substitute.For<ITripRepository>();
        var validator = new DriverAcceptCommandValidator();
        // Act
        var result = await validator.ValidateAsync(new DriverAcceptCommand(Guid.NewGuid(), Guid.Empty));
        // Assert
        Assert.False(result.IsValid); Assert.Contains(result.Errors, e => e.PropertyName == "DriverId");
        Assert.Empty(repo.ReceivedCalls());
    }
}
