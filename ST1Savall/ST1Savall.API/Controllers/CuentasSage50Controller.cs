using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CuentasSage50Controller(SageGestionDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CuentaSage50>>> GetCuentas() =>
        await context.Cuentas.AsNoTracking()
            .OrderBy(cuenta => cuenta.Codigo)
            .ToListAsync();
}
