using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CargosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CargosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cargo>>> GetCargos()
    {
        return await _context.Cargos.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Cargo>> GetCargo(int id)
    {
        var cargo = await _context.Cargos.FindAsync(id);
        if (cargo == null) return NotFound();
        return cargo;
    }

    [HttpPost]
    public async Task<ActionResult<Cargo>> PostCargo(Cargo cargo)
    {
        _context.Cargos.Add(cargo);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCargo), new { id = cargo.IdCargo }, cargo);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutCargo(int id, Cargo cargo)
    {
        if (id != cargo.IdCargo) return BadRequest();
        _context.Entry(cargo).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CargoExists(id)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCargo(int id)
    {
        var cargo = await _context.Cargos.FindAsync(id);
        if (cargo == null) return NotFound();
        _context.Cargos.Remove(cargo);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool CargoExists(int id)
    {
        return _context.Cargos.Any(e => e.IdCargo == id);
    }
}
