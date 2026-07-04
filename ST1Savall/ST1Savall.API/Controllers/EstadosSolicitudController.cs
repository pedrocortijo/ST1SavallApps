using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstadosSolicitudController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EstadosSolicitudController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EstadoSolicitud>>> GetEstados()
    {
        return await _context.EstadosSolicitud.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EstadoSolicitud>> GetEstado(int id)
    {
        var estado = await _context.EstadosSolicitud.FindAsync(id);
        if (estado == null) return NotFound();
        return estado;
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutEstado(int id, EstadoSolicitud estado)
    {
        if (id != estado.IdEstado) return BadRequest();
        _context.Entry(estado).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.EstadosSolicitud.AnyAsync(e => e.IdEstado == id)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<EstadoSolicitud>> PostEstado(EstadoSolicitud estado)
    {
        _context.EstadosSolicitud.Add(estado);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEstado), new { id = estado.IdEstado }, estado);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEstado(int id)
    {
        var estado = await _context.EstadosSolicitud.FindAsync(id);
        if (estado == null) return NotFound();
        _context.EstadosSolicitud.Remove(estado);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
