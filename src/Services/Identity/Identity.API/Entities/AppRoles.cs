namespace Identity.API.Entities;

public static class AppRoles
{
    public const string Rider = "Rider";
    public const string Driver = "Driver";

    public static readonly string[] All = [Rider, Driver];

    public static bool IsValid(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return false;
        return role.Equals(Rider, StringComparison.OrdinalIgnoreCase) ||
               role.Equals(Driver, StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string role)
    {
        if (role.Equals(Rider, StringComparison.OrdinalIgnoreCase)) return Rider;
        if (role.Equals(Driver, StringComparison.OrdinalIgnoreCase)) return Driver;
        return role;
    }
}
