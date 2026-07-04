using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TipoIvaSage50Controller : ControllerBase
{
    private readonly SageGestionDbContext _context;

    public TipoIvaSage50Controller(SageGestionDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TipoIvaSage50>>> GetTipoIva()
    {
        return await _context.TipoIva.ToListAsync();
    }

    [HttpGet("{codigo}")]
    public async Task<ActionResult<TipoIvaSage50>> GetTipoIva(string codigo)
    {
        var targetCodigo = (codigo ?? "").Trim();
        var tipoIva = await _context.TipoIva.FirstOrDefaultAsync(e => e.Codigo.Trim() == targetCodigo);
        if (tipoIva == null) return NotFound();
        return tipoIva;
    }
}
