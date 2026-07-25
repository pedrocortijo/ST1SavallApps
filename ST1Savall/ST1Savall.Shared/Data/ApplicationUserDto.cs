using System;

namespace ST1Savall.Shared.Data;

public class ApplicationUserDto
{
    public string Id { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Tecnico { get; set; }
    public int? ClienteId { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }

    public bool IsLocked => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
}

public class CreateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Tecnico { get; set; }
    public int? ClienteId { get; set; }
}

public class UpdateUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Tecnico { get; set; }
    public int? ClienteId { get; set; }
    public bool Lockout { get; set; }
}

public class ChangePasswordDto
{
    public string UserId { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
