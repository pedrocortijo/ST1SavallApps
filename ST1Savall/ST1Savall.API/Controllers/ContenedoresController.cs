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
        return await _context.Contenedores.Include(c => c.Tipo).Include(c => c.Planta).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Contenedor>> GetContenedor(int id)
    {
        var contenedor = await _context.Contenedores.Include(c => c.Tipo).Include(c => c.Planta).FirstOrDefaultAsync(c => c.IdContenedor == id);
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

    [HttpPost("seed-random")]
    public async Task<IActionResult> SeedRandom()
    {
        var random = new System.Random();
        var tipos = await _context.ContenedoresTipos.ToListAsync();
        var plantas = await _context.Plantas.ToListAsync();

        if (tipos == null || !tipos.Any() || plantas == null || !plantas.Any())
        {
            return BadRequest("Se necesitan tipos de contenedores y plantas para crear contenedores.");
        }

        var estados = new[] { "Disponible", "Entregado", "En Reparacion", "Baja" };
        var creados = 0;

        for (int i = 0; i < 100; i++)
        {
            string numSerie;
            do
            {
                numSerie = $"CONT-{random.Next(100000, 999999)}";
            } while (await _context.Contenedores.AnyAsync(c => c.NumSerie == numSerie));

            var tipo = tipos[random.Next(tipos.Count)];
            var planta = plantas[random.Next(plantas.Count)];
            var estado = estados[random.Next(estados.Length)];

            var cont = new Contenedor
            {
                NumSerie = numSerie,
                IdTipo = tipo.IdTipo,
                IdPlanta = planta.IdPlanta,
                EstadoFisico = estado,
                UltimaRevision = DateOnly.FromDateTime(System.DateTime.Today.AddDays(-random.Next(0, 365))),
                Observaciones = $"Contenedor de prueba {i + 1}"
            };

            _context.Contenedores.Add(cont);
            creados++;
        }

        await _context.SaveChangesAsync();
        return Ok($"Se han creado {creados} contenedores al azar.");
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
