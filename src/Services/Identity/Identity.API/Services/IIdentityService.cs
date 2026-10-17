using Identity.API.DTOs;
using Shared;

namespace Identity.API.Services;

public interface IIdentityService
{
    Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    Task<Result<IReadOnlyList<UserDto>>> GetUsersByRoleAsync(string role, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<UserDto>>> GetRidersAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<UserDto>>> GetDriversAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<UserDto>>> GetAllUsersAsync(string? role = null, CancellationToken cancellationToken = default);
}
