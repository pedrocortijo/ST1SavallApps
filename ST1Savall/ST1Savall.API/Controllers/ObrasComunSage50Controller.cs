using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ObrasComunSage50Controller : ControllerBase
{
    private readonly SageComunDbContext _context;

    public ObrasComunSage50Controller(SageComunDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ObraComunSage50>>> GetObras()
    {
        return await _context.Obras.ToListAsync();
    }

    [HttpGet("{codigo}")]
    public async Task<ActionResult<ObraComunSage50>> GetObra(string codigo)
    {
        var obra = await _context.Obras.FindAsync(codigo);
        if (obra == null) return NotFound();
        return obra;
    }

    [HttpPost]
    public async Task<ActionResult<ObraComunSage50>> PostObra(ObraComunSage50 obra)
    {
        _context.Obras.Add(obra);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (ObraExists(obra.Codigo))
            {
                return Conflict();
            }
            throw;
        }
        return CreatedAtAction(nameof(GetObra), new { codigo = obra.Codigo }, obra);
    }

    [HttpPut("{codigo}")]
    public async Task<IActionResult> PutObra(string codigo, ObraComunSage50 obra)
    {
        if (codigo != obra.Codigo) return BadRequest();
        _context.Entry(obra).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ObraExists(codigo)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{codigo}")]
    public async Task<IActionResult> DeleteObra(string codigo)
    {
        var obra = await _context.Obras.FindAsync(codigo);
        if (obra == null) return NotFound();
        _context.Obras.Remove(obra);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool ObraExists(string codigo)
    {
        return _context.Obras.Any(e => e.Codigo == codigo);
    }
}
