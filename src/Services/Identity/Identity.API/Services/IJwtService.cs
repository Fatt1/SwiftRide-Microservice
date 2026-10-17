using Identity.API.Entities;

namespace Identity.API.Services;

public interface IJwtService
{
    (string Token, int ExpiresIn) GenerateToken(AppUser user, IEnumerable<string>? roles = null);
}
