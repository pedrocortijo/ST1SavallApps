using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrioridadesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PrioridadesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Prioridad>>> GetPrioridades()
    {
        return await _context.Prioridades.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Prioridad>> GetPrioridad(int id)
    {
        var prioridad = await _context.Prioridades.FindAsync(id);
        if (prioridad == null) return NotFound();
        return prioridad;
    }

    [HttpPost]
    public async Task<ActionResult<Prioridad>> PostPrioridad(Prioridad prioridad)
    {
        _context.Prioridades.Add(prioridad);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (PrioridadExists(prioridad.IdPrioridad))
            {
                return Conflict();
            }
            throw;
        }
        return CreatedAtAction(nameof(GetPrioridad), new { id = prioridad.IdPrioridad }, prioridad);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutPrioridad(int id, Prioridad prioridad)
    {
        if (id != prioridad.IdPrioridad) return BadRequest();
        _context.Entry(prioridad).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PrioridadExists(id)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePrioridad(int id)
    {
        var prioridad = await _context.Prioridades.FindAsync(id);
        if (prioridad == null) return NotFound();
        _context.Prioridades.Remove(prioridad);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool PrioridadExists(int id)
    {
        return _context.Prioridades.Any(e => e.IdPrioridad == id);
    }
}
