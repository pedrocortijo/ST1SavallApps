using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OperariosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OperariosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Operario>>> GetOperarios()
    {
        return await _context.Operarios.Include(o => o.Cargo).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Operario>> GetOperario(int id)
    {
        var operario = await _context.Operarios.Include(o => o.Cargo).FirstOrDefaultAsync(o => o.IdOperario == id);
        if (operario == null) return NotFound();
        return operario;
    }

    [HttpPost]
    public async Task<ActionResult<Operario>> PostOperario(Operario operario)
    {
        _context.Operarios.Add(operario);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetOperario), new { id = operario.IdOperario }, operario);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutOperario(int id, Operario operario)
    {
        if (id != operario.IdOperario) return BadRequest();
        _context.Entry(operario).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!OperarioExists(id)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOperario(int id)
    {
        var operario = await _context.Operarios.FindAsync(id);
        if (operario == null) return NotFound();
        _context.Operarios.Remove(operario);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool OperarioExists(int id)
    {
        return _context.Operarios.Any(e => e.IdOperario == id);
    }
}
