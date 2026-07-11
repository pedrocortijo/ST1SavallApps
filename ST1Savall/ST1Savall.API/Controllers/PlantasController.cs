using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlantasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PlantasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Planta>>> GetPlantas()
    {
        return await _context.Plantas.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Planta>> GetPlanta(int id)
    {
        var planta = await _context.Plantas.FindAsync(id);
        if (planta == null) return NotFound();
        return planta;
    }

    [HttpPost]
    public async Task<ActionResult<Planta>> PostPlanta(Planta planta)
    {
        _context.Plantas.Add(planta);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPlanta), new { id = planta.IdPlanta }, planta);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutPlanta(int id, Planta planta)
    {
        if (id != planta.IdPlanta) return BadRequest();
        _context.Entry(planta).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PlantaExists(id)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePlanta(int id)
    {
        var planta = await _context.Plantas.FindAsync(id);
        if (planta == null) return NotFound();
        _context.Plantas.Remove(planta);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool PlantaExists(int id)
    {
        return _context.Plantas.Any(e => e.IdPlanta == id);
    }
}