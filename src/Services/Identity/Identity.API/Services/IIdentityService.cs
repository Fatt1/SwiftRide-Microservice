using Identity.API.DTOs;
using Shared;

namespace Identity.API.Services;

public interface IIdentityService
{
    Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request);
    Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request);
}
