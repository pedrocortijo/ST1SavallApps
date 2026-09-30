using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MonedasSage50Controller(SageGestionDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MonedaSage50>>> GetMonedas() =>
        await context.Monedas.AsNoTracking()
            .OrderBy(moneda => moneda.Codigo)
            .ToListAsync();
}
