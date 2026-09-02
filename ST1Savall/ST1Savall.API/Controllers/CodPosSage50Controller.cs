using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CodPosSage50Controller : ControllerBase
{
    private readonly SageGestionDbContext _context;

    public CodPosSage50Controller(SageGestionDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CodPosSage50>>> GetCodpos()
    {
        return await _context.Codpos.AsNoTracking().ToListAsync();
    }

    [HttpGet("paginados")]
    public async Task<ActionResult<ResultadoPaginado<CodPosSage50>>> GetCodposPaginados(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 100,
        [FromQuery] string? buscar = null)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 25, 250);
        var texto = buscar?.Trim();

        IQueryable<CodPosSage50> consulta = _context.Codpos.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(texto))
        {
            var patron = $"%{texto}%";
            consulta = consulta.Where(p =>
                EF.Functions.Like(EF.Functions.Collate(p.Codigo, "Modern_Spanish_CI_AI"), patron) ||
                EF.Functions.Like(EF.Functions.Collate(p.Poblacion, "Modern_Spanish_CI_AI"), patron) ||
                EF.Functions.Like(EF.Functions.Collate(p.Provincia, "Modern_Spanish_CI_AI"), patron) ||
                EF.Functions.Like(EF.Functions.Collate(p.Cpostalm, "Modern_Spanish_CI_AI"), patron));
        }

        var totalRegistros = await consulta.CountAsync();
        var datos = await consulta
            .OrderBy(p => p.Codigo)
            .ThenBy(p => p.Linea)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        foreach (var d in datos)
        {
            d.Codigo = d.Codigo?.Trim() ?? "";
            d.Linea = d.Linea?.Trim() ?? "";
            d.Poblacion = d.Poblacion?.Trim() ?? "";
            d.Provincia = d.Provincia?.Trim() ?? "";
            d.Cpostalm = d.Cpostalm?.Trim() ?? "";
            d.Lati = d.Lati?.Trim() ?? "";
            d.Longi = d.Longi?.Trim() ?? "";
        }

        return Ok(new ResultadoPaginado<CodPosSage50>
        {
            Datos = datos,
            TotalRegistros = totalRegistros,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
    }

    [HttpGet("{codigo}/{linea}")]
    public async Task<ActionResult<CodPosSage50>> GetCodpos(string codigo, string linea)
    {
        var codpos = await _context.Codpos.FindAsync(codigo, linea);
        if (codpos == null) return NotFound();
        return codpos;
    }

    [HttpGet("por-codigo/{codigo}")]
    public async Task<ActionResult<IEnumerable<CodPosSage50>>> GetByCodigo(string codigo)
    {
        return await _context.Codpos.Where(c => c.Codigo == codigo).ToListAsync();
    }

    [HttpGet("por-vista/{vista}")]
    public async Task<ActionResult<IEnumerable<CodPosSage50>>> GetByVista(bool vista)
    {
        return await _context.Codpos.Where(c => c.Vista == vista).ToListAsync();
    }

    [HttpGet("por-codigo-y-vista/{codigo}/{vista}")]
    public async Task<ActionResult<IEnumerable<CodPosSage50>>> GetByCodigoAndVista(string codigo, bool vista)
    {
        return await _context.Codpos.Where(c => c.Codigo == codigo && c.Vista == vista).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<CodPosSage50>> PostCodpos(CodPosSage50 codpos)
    {
        if (!string.IsNullOrEmpty(codpos.Codigo))
        {
            codpos.Provinerp = codpos.Codigo.Length >= 2 ? codpos.Codigo.Substring(0, 2) : codpos.Codigo;
        }
        if (string.IsNullOrWhiteSpace(codpos.GuidId))
        {
            codpos.GuidId = Guid.NewGuid().ToString();
        }
        _context.Codpos.Add(codpos);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (CodposExists(codpos.Codigo, codpos.Linea))
            {
                return Conflict();
            }
            throw;
        }
        return CreatedAtAction(nameof(GetCodpos), new { codigo = codpos.Codigo, linea = codpos.Linea }, codpos);
    }

    [HttpPut("{codigo}/{linea}")]
    public async Task<IActionResult> PutCodpos(string codigo, string linea, CodPosSage50 codpos)
    {
        if (codigo != codpos.Codigo || linea != codpos.Linea) return BadRequest();
        if (!string.IsNullOrEmpty(codpos.Codigo))
        {
            codpos.Provinerp = codpos.Codigo.Length >= 2 ? codpos.Codigo.Substring(0, 2) : codpos.Codigo;
        }
        _context.Entry(codpos).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CodposExists(codigo, linea)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{codigo}/{linea}")]
    public async Task<IActionResult> DeleteCodpos(string codigo, string linea)
    {
        var codpos = await _context.Codpos.FindAsync(codigo, linea);
        if (codpos == null) return NotFound();
        _context.Codpos.Remove(codpos);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool CodposExists(string codigo, string linea)
    {
        return _context.Codpos.Any(e => e.Codigo == codigo && e.Linea == linea);
    }
}
