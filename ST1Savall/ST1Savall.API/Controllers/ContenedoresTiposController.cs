using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContenedoresTiposController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ContenedoresTiposController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContenedorTipo>>> GetContenedoresTipos()
    {
        return await _context.ContenedoresTipos.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ContenedorTipo>> GetContenedorTipo(int id)
    {
        var tipo = await _context.ContenedoresTipos.FindAsync(id);
        if (tipo == null) return NotFound();
        return tipo;
    }

    [HttpPost]
    public async Task<ActionResult<ContenedorTipo>> PostContenedorTipo(ContenedorTipo tipo)
    {
        _context.ContenedoresTipos.Add(tipo);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetContenedorTipo), new { id = tipo.IdTipo }, tipo);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutContenedorTipo(int id, ContenedorTipo tipo)
    {
        if (id != tipo.IdTipo) return BadRequest();
        _context.Entry(tipo).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ContenedorTipoExists(id)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContenedorTipo(int id)
    {
        var tipo = await _context.ContenedoresTipos.FindAsync(id);
        if (tipo == null) return NotFound();
        _context.ContenedoresTipos.Remove(tipo);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool ContenedorTipoExists(int id)
    {
        return _context.ContenedoresTipos.Any(e => e.IdTipo == id);
    }
}
