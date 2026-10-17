using Identity.API.DTOs;
using Identity.API.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Extensions;

namespace Identity.API.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapApiV1Group("users").WithTags("Users");

        group.MapGet("/", async ([FromQuery] string? role, IIdentityService identityService, CancellationToken ct) =>
        {
            var result = await identityService.GetAllUsersAsync(role, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetAllUsers")
        .WithSummary("Lấy danh sách người dùng (hỗ trợ lọc theo query param ?role=Rider hoặc ?role=Driver)")
        .Produces<IReadOnlyList<UserDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/riders", async (IIdentityService identityService, CancellationToken ct) =>
        {
            var result = await identityService.GetRidersAsync(ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetRiders")
        .WithSummary("Lấy danh sách người dùng có vai trò là Rider")
        .Produces<IReadOnlyList<UserDto>>(StatusCodes.Status200OK);

        group.MapGet("/drivers", async (IIdentityService identityService, CancellationToken ct) =>
        {
            var result = await identityService.GetDriversAsync(ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetDrivers")
        .WithSummary("Lấy danh sách người dùng có vai trò là Driver")
        .Produces<IReadOnlyList<UserDto>>(StatusCodes.Status200OK);

        return endpoints;
    }
}
