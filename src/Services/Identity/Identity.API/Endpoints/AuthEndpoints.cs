using Identity.API.DTOs;
using Identity.API.Services;
using Shared.Extensions;

namespace Identity.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapApiV1Group("auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequestDto request, IIdentityService identityService, CancellationToken ct) =>
        {
            var result = await identityService.RegisterAsync(request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("Register")
        .WithSummary("Đăng ký tài khoản mới (hỗ trợ role Rider hoặc Driver) và phát sự kiện tương ứng")
        .Produces<RegisterResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", async (LoginRequestDto request, IIdentityService identityService) =>
        {
            var result = await identityService.LoginAsync(request);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("Login")
        .WithSummary("Đăng nhập và nhận Access Token")
        .Produces<AuthResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/riders", async (IIdentityService identityService, CancellationToken ct) =>
        {
            var result = await identityService.GetRidersAsync(ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetAuthRiders")
        .WithSummary("Lấy danh sách người dùng có role Rider")
        .Produces<IReadOnlyList<UserDto>>(StatusCodes.Status200OK);

        group.MapGet("/drivers", async (IIdentityService identityService, CancellationToken ct) =>
        {
            var result = await identityService.GetDriversAsync(ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetAuthDrivers")
        .WithSummary("Lấy danh sách người dùng có role Driver")
        .Produces<IReadOnlyList<UserDto>>(StatusCodes.Status200OK);

        return endpoints;
    }
}
