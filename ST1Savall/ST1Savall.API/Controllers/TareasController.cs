using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TareasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TareasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Tarea>>> GetTareas()
    {
        return await _context.Tareas.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Tarea>> GetTarea(int id)
    {
        var tarea = await _context.Tareas.FindAsync(id);
        if (tarea == null) return NotFound();
        return tarea;
    }

    [HttpGet("{id}/siguientes")]
    public async Task<ActionResult<IEnumerable<Tarea>>> GetTareasSiguientes(int id)
    {
        var siguientes = await _context.TareasRelaciones
            .Where(tr => tr.IdTareaOrigen == id)
            .Select(tr => tr.TareaDestino)
            .ToListAsync();

        if (!siguientes.Any())
        {
            return await _context.Tareas.ToListAsync();
        }

        return siguientes!;
    }

    [HttpPost]
    public async Task<ActionResult<Tarea>> PostTarea(Tarea tarea)
    {
        tarea.IdTarea = 0;
        _context.Tareas.Add(tarea);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (TareaExists(tarea.IdTarea))
            {
                return Conflict();
            }
            throw;
        }
        return CreatedAtAction(nameof(GetTarea), new { id = tarea.IdTarea }, tarea);
    }

    [HttpPost("{id}/duplicar")]
    public async Task<ActionResult<Tarea>> DuplicarTarea(int id)
    {
        var tareaOrigen = await _context.Tareas
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IdTarea == id);
        if (tareaOrigen == null) return NotFound();

        var copia = new Tarea
        {
            NombreTarea = $"{tareaOrigen.NombreTarea} (Copia)",
            Recoger1 = tareaOrigen.Recoger1,
            Recoger2 = tareaOrigen.Recoger2,
            Entrega1 = tareaOrigen.Entrega1,
            Entrega2 = tareaOrigen.Entrega2
        };

        _context.Tareas.Add(copia);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTarea), new { id = copia.IdTarea }, copia);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutTarea(int id, Tarea tarea)
    {
        if (id != tarea.IdTarea) return BadRequest();
        
        var dbTarea = await _context.Tareas.FindAsync(id);
        if (dbTarea == null)
        {
            return NotFound();
        }

        dbTarea.NombreTarea = tarea.NombreTarea;
        dbTarea.Recoger1 = tarea.Recoger1;
        dbTarea.Recoger2 = tarea.Recoger2;
        dbTarea.Entrega1 = tarea.Entrega1;
        dbTarea.Entrega2 = tarea.Entrega2;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TareaExists(id)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTarea(int id)
    {
        var tarea = await _context.Tareas.FindAsync(id);
        if (tarea == null) return NotFound();
        _context.Tareas.Remove(tarea);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool TareaExists(int id)
    {
        return _context.Tareas.Any(e => e.IdTarea == id);
    }
}
