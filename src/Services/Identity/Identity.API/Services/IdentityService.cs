using Identity.API.DTOs;
using Identity.API.Entities;
using Microsoft.AspNetCore.Identity;
using Shared;

namespace Identity.API.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        UserManager<AppUser> userManager,
        IJwtService jwtService,
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        var existingUserByName = await _userManager.FindByNameAsync(request.UserName);
        if (existingUserByName != null)
        {
            return Result.Failure<RegisterResponseDto>(new ConflictError("Tên đăng nhập đã tồn tại trong hệ thống"));
        }

        var existingUserByEmail = await _userManager.FindByEmailAsync(request.Email);
        if (existingUserByEmail != null)
        {
            return Result.Failure<RegisterResponseDto>(new ConflictError("Email đã được sử dụng trong hệ thống"));
        }

        var user = new AppUser
        {
            UserName = request.UserName,
            Email = request.Email,
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            var errorMessage = string.Join("; ", errors);
            _logger.LogWarning("Tạo tài khoản thất bại cho {UserName}: {Errors}", request.UserName, errorMessage);
            return Result.Failure<RegisterResponseDto>(new BadError(errorMessage));
        }

        _logger.LogInformation("Đăng ký tài khoản thành công cho user: {UserName} (ID: {UserId})", user.UserName, user.Id);

        return Result.Success(new RegisterResponseDto
        {
            UserId = user.Id,
            UserName = user.UserName!,
            Email = user.Email!,
            Message = "Đăng ký tài khoản thành công"
        });
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        AppUser? user = null;

        if (request.UserNameOrEmail.Contains('@'))
        {
            user = await _userManager.FindByEmailAsync(request.UserNameOrEmail);
        }

        user ??= await _userManager.FindByNameAsync(request.UserNameOrEmail);

        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            _logger.LogWarning("Đăng nhập không thành công cho tài khoản: {Account}", request.UserNameOrEmail);
            return Result.Failure<AuthResponseDto>(new BadError("Tên đăng nhập/email hoặc mật khẩu không chính xác"));
        }

        var (token, expiresIn) = _jwtService.GenerateToken(user);

        _logger.LogInformation("Đăng nhập thành công cho user: {UserName} (ID: {UserId})", user.UserName, user.Id);

        return Result.Success(new AuthResponseDto
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName
        });
    }
}
