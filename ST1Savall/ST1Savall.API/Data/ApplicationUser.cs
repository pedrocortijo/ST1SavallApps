using Microsoft.AspNetCore.Identity;

namespace ST1Savall.API.Data;

public class ApplicationUser : IdentityUser
{
    public string? Tecnico { get; set; }
    public int? ClienteId { get; set; }
}
