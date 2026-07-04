using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SolicitudesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SolicitudesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Solicitud>>> GetSolicitudes()
    {
        return await _context.Solicitudes.Include(s => s.Operario).ThenInclude(o => o!.Cargo).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Solicitud>> GetSolicitud(int id)
    {
        var solicitud = await _context.Solicitudes
            .Include(s => s.Operario)
            .ThenInclude(o => o!.Cargo)
            .FirstOrDefaultAsync(s => s.IdSolicitud == id);
            
        if (solicitud == null) return NotFound();
        return solicitud;
    }

    [HttpPost]
    public async Task<ActionResult<Solicitud>> PostSolicitud(Solicitud solicitud)
    {
        _context.Solicitudes.Add(solicitud);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSolicitud), new { id = solicitud.IdSolicitud }, solicitud);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutSolicitud(int id, Solicitud solicitud)
    {
        if (id != solicitud.IdSolicitud) return BadRequest();
        _context.Entry(solicitud).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SolicitudExists(id)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSolicitud(int id)
    {
        var solicitud = await _context.Solicitudes.FindAsync(id);
        if (solicitud == null) return NotFound();
        _context.Solicitudes.Remove(solicitud);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool SolicitudExists(int id)
    {
        return _context.Solicitudes.Any(e => e.IdSolicitud == id);
    }
}
