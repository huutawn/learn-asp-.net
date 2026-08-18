using System.ComponentModel.DataAnnotations;

namespace IdentityService.Api.DTOs.Auth;

public sealed record RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; init; } = null!;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; init; } = null!;

    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string DisplayName { get; init; } = null!;
}
