using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FpagSage50Controller : ControllerBase
{
    private readonly SageGestionDbContext _context;

    public FpagSage50Controller(SageGestionDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FpagSage50>>> GetFpag()
    {
        return await _context.Fpag.ToListAsync();
    }

    [HttpGet("{codigo}")]
    public async Task<ActionResult<FpagSage50>> GetFpag(string codigo)
    {
        var targetCodigo = (codigo ?? "").Trim();
        var fpag = await _context.Fpag.FirstOrDefaultAsync(e => e.Codigo.Trim() == targetCodigo);
        if (fpag == null) return NotFound();
        return fpag;
    }
}
