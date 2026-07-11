using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TareasRelacionesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TareasRelacionesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TareaRelacion>>> GetTareasRelaciones()
    {
        return await _context.TareasRelaciones
            .Include(tr => tr.TareaOrigen)
            .Include(tr => tr.TareaDestino)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<TareaRelacion>> PostTareaRelacion(TareaRelacion relacion)
    {
        var existe = await _context.TareasRelaciones.AnyAsync(r => 
            r.IdTareaOrigen == relacion.IdTareaOrigen && 
            r.IdTareaDestino == relacion.IdTareaDestino);
            
        if (existe)
        {
            return Conflict("Esta relación de tareas ya existe.");
        }

        _context.TareasRelaciones.Add(relacion);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetTareasRelaciones), new { idOrigen = relacion.IdTareaOrigen, idDestino = relacion.IdTareaDestino }, relacion);
    }

    [HttpDelete("{idOrigen}/{idDestino}")]
    public async Task<IActionResult> DeleteTareaRelacion(int idOrigen, int idDestino)
    {
        var relacion = await _context.TareasRelaciones.FindAsync(idOrigen, idDestino);
        if (relacion == null) return NotFound();

        _context.TareasRelaciones.Remove(relacion);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
