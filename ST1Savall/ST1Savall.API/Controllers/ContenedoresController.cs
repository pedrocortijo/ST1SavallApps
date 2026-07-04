using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContenedoresController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ContenedoresController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Contenedor>>> GetContenedores()
    {
        return await _context.Contenedores.Include(c => c.Tipo).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Contenedor>> GetContenedor(int id)
    {
        var contenedor = await _context.Contenedores.Include(c => c.Tipo).FirstOrDefaultAsync(c => c.IdContenedor == id);
        if (contenedor == null) return NotFound();
        return contenedor;
    }

    [HttpPost]
    public async Task<ActionResult<Contenedor>> PostContenedor(Contenedor contenedor)
    {
        _context.Contenedores.Add(contenedor);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (ContenedorNumSerieExists(contenedor.NumSerie))
            {
                return Conflict("El número de serie ya está registrado.");
            }
            throw;
        }
        return CreatedAtAction(nameof(GetContenedor), new { id = contenedor.IdContenedor }, contenedor);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutContenedor(int id, Contenedor contenedor)
    {
        if (id != contenedor.IdContenedor) return BadRequest();
        _context.Entry(contenedor).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ContenedorExists(id)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContenedor(int id)
    {
        var contenedor = await _context.Contenedores.FindAsync(id);
        if (contenedor == null) return NotFound();
        _context.Contenedores.Remove(contenedor);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool ContenedorExists(int id)
    {
        return _context.Contenedores.Any(e => e.IdContenedor == id);
    }

    private bool ContenedorNumSerieExists(string numSerie)
    {
        return _context.Contenedores.Any(e => e.NumSerie == numSerie);
    }
}
