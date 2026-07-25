using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MotivosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MotivosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Motivo>>> GetMotivos()
    {
        return await _context.Motivos.OrderBy(m => m.DescripcionMotivo).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Motivo>> GetMotivo(int id)
    {
        var motivo = await _context.Motivos.FindAsync(id);
        if (motivo == null) return NotFound();
        return motivo;
    }

    [HttpPost]
    public async Task<ActionResult<Motivo>> PostMotivo(Motivo motivo)
    {
        _context.Motivos.Add(motivo);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (MotivoExists(motivo.IdMotivo))
            {
                return Conflict();
            }
            throw;
        }
        return CreatedAtAction(nameof(GetMotivo), new { id = motivo.IdMotivo }, motivo);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutMotivo(int id, Motivo motivo)
    {
        if (id != motivo.IdMotivo) return BadRequest();
        _context.Entry(motivo).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!MotivoExists(id)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMotivo(int id)
    {
        var motivo = await _context.Motivos.FindAsync(id);
        if (motivo == null) return NotFound();
        _context.Motivos.Remove(motivo);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool MotivoExists(int id)
    {
        return _context.Motivos.Any(e => e.IdMotivo == id);
    }
}
