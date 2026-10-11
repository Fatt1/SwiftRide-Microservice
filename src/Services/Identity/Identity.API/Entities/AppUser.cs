using Microsoft.AspNetCore.Identity;

namespace Identity.API.Entities;

public class AppUser : IdentityUser
{
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
