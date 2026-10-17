using EventBus.Messages.Events;
using Identity.API.DTOs;
using Identity.API.Entities;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Shared;

namespace Identity.API.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IJwtService _jwtService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IJwtService jwtService,
        IPublishEndpoint publishEndpoint,
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!AppRoles.IsValid(request.Role))
        {
            return Result.Failure<RegisterResponseDto>(new BadError("Role không hợp lệ. Vui lòng chọn 'Rider' hoặc 'Driver'."));
        }

        var role = AppRoles.Normalize(request.Role);

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

        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
        }

        await _userManager.AddToRoleAsync(user, role);

        _logger.LogInformation("Đăng ký tài khoản thành công cho user: {UserName} (ID: {UserId}, Role: {Role})",
            user.UserName, user.Id, role);

        // Bắn sự kiện UserCreated qua MassTransit RabbitMQ
        await _publishEndpoint.Publish<UserCreated>(new UserCreated
        {
            Id = Guid.TryParse(user.Id, out var parsedGuid) ? parsedGuid : Guid.NewGuid(),
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName
        }, cancellationToken);

        _logger.LogInformation("Đã publish sự kiện UserCreated cho user: {UserId}", user.Id);

        // Nếu role là Driver thì bắn thêm sự kiện DriverCreated cho Matching service
        if (role == AppRoles.Driver)
        {
            var driverGuid = Guid.TryParse(user.Id, out var parsedDriverId) ? parsedDriverId : Guid.NewGuid();
            await _publishEndpoint.Publish<DriverCreated>(new DriverCreated
            {
                Id = Guid.NewGuid(),
                DriverId = driverGuid,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName
            }, cancellationToken);

            _logger.LogInformation("Đã publish thêm sự kiện DriverCreated cho driver: {DriverId}", user.Id);
        }

        return Result.Success(new RegisterResponseDto
        {
            UserId = user.Id,
            UserName = user.UserName!,
            Email = user.Email!,
            Role = role,
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

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresIn) = _jwtService.GenerateToken(user, roles);

        _logger.LogInformation("Đăng nhập thành công cho user: {UserName} (ID: {UserId})", user.UserName, user.Id);

        return Result.Success(new AuthResponseDto
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Roles = roles
        });
    }

    public async Task<Result<IReadOnlyList<UserDto>>> GetUsersByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        if (!AppRoles.IsValid(role))
        {
            return Result.Failure<IReadOnlyList<UserDto>>(new BadError("Role không hợp lệ. Vui lòng chọn 'Rider' hoặc 'Driver'."));
        }

        var normalizedRole = AppRoles.Normalize(role);
        var users = await _userManager.GetUsersInRoleAsync(normalizedRole);

        var userDtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            UserName = u.UserName ?? string.Empty,
            Email = u.Email ?? string.Empty,
            FullName = u.FullName,
            Role = normalizedRole,
            CreatedAt = u.CreatedAt
        })
        .OrderByDescending(u => u.CreatedAt)
        .ToList();

        return Result.Success<IReadOnlyList<UserDto>>(userDtos);
    }

    public Task<Result<IReadOnlyList<UserDto>>> GetRidersAsync(CancellationToken cancellationToken = default)
        => GetUsersByRoleAsync(AppRoles.Rider, cancellationToken);

    public Task<Result<IReadOnlyList<UserDto>>> GetDriversAsync(CancellationToken cancellationToken = default)
        => GetUsersByRoleAsync(AppRoles.Driver, cancellationToken);

    public async Task<Result<IReadOnlyList<UserDto>>> GetAllUsersAsync(string? role = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(role))
        {
            return await GetUsersByRoleAsync(role, cancellationToken);
        }

        var ridersResult = await GetRidersAsync(cancellationToken);
        var driversResult = await GetDriversAsync(cancellationToken);

        var allUsers = new List<UserDto>();
        if (ridersResult.IsSuccess) allUsers.AddRange(ridersResult.Value);
        if (driversResult.IsSuccess) allUsers.AddRange(driversResult.Value);

        return Result.Success<IReadOnlyList<UserDto>>(allUsers.OrderByDescending(u => u.CreatedAt).ToList());
    }
}
