using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParametrosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ParametrosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Parametro>>> GetParametros()
    {
        return await _context.Parametros.AsNoTracking().OrderBy(p => p.Empresa).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Parametro>> GetParametro(int id)
    {
        var parametro = await _context.Parametros.FindAsync(id);
        return parametro is null ? NotFound() : parametro;
    }

    [HttpPost]
    public async Task<ActionResult<Parametro>> PostParametro(Parametro parametro)
    {
        _context.Parametros.Add(parametro);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetParametro), new { id = parametro.Id }, parametro);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutParametro(int id, Parametro parametro)
    {
        if (id != parametro.Id)
        {
            return BadRequest();
        }

        _context.Entry(parametro).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException) when (!ParametroExists(id))
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteParametro(int id)
    {
        var parametro = await _context.Parametros.FindAsync(id);
        if (parametro is null)
        {
            return NotFound();
        }

        _context.Parametros.Remove(parametro);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool ParametroExists(int id) => _context.Parametros.Any(p => p.Id == id);
}