namespace IdentityService.Api.Entities;

public enum UserRole
{
    User,
    Admin
}

public sealed class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string Language { get; set; } = "en";

    public string TimeZoneId { get; set; } = "UTC";
    public bool EmailVerified { get; set; }

    public UserRole Role { get; set; } = UserRole.User;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
