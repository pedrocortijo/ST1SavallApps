using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/series-sage50")]
public class SeriesSage50Controller(SageGestionDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SerieSage50>>> GetSeries() =>
        await context.Series.AsNoTracking()
            .OrderBy(s => s.EMPRESA).ThenBy(s => s.TIPODOC).ThenBy(s => s.SERIE)
            .ToListAsync();

    [HttpPost]
    public async Task<ActionResult<SerieSage50>> PostSerie(SerieSage50 serie)
    {
        var error = NormalizarYValidar(serie);
        if (error is not null) return BadRequest(new { message = error });
        if (await ExisteAsync(serie.EMPRESA, serie.TIPODOC, serie.SERIE))
            return Conflict(new { message = "Ya existe una serie con esa empresa y tipo de documento." });

        context.Series.Add(serie);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSeries), serie);
    }

    [HttpPut]
    public async Task<IActionResult> PutSerie([FromQuery] string empresa, [FromQuery] int tipodoc, [FromQuery] string serie, SerieSage50 datos)
    {
        var actual = await BuscarAsync(empresa, tipodoc, serie);
        if (actual is null) return NotFound(new { message = "No se encuentra la serie de Sage." });

        var error = NormalizarYValidar(datos);
        if (error is not null) return BadRequest(new { message = error });
        var cambiaClave = !string.Equals(actual.EMPRESA.Trim(), datos.EMPRESA, StringComparison.OrdinalIgnoreCase)
            || actual.TIPODOC != datos.TIPODOC
            || !string.Equals(actual.SERIE.Trim(), datos.SERIE, StringComparison.OrdinalIgnoreCase);
        if (cambiaClave && await ExisteAsync(datos.EMPRESA, datos.TIPODOC, datos.SERIE))
            return Conflict(new { message = "Ya existe una serie con esa empresa y tipo de documento." });

        if (cambiaClave)
        {
            context.Series.Remove(actual);
            context.Series.Add(datos);
        }
        else
        {
            actual.CONTADOR = datos.CONTADOR;
        }
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteSerie([FromQuery] string empresa, [FromQuery] int tipodoc, [FromQuery] string serie)
    {
        var actual = await BuscarAsync(empresa, tipodoc, serie);
        if (actual is null) return NotFound(new { message = "No se encuentra la serie de Sage." });
        context.Series.Remove(actual);
        await context.SaveChangesAsync();
        return NoContent();
    }

    private Task<bool> ExisteAsync(string empresa, int tipodoc, string serie) =>
        context.Series.AnyAsync(s => s.EMPRESA.Trim() == empresa && s.TIPODOC == tipodoc && s.SERIE.Trim() == serie);

    private Task<SerieSage50?> BuscarAsync(string empresa, int tipodoc, string serie) =>
        context.Series.FirstOrDefaultAsync(s => s.EMPRESA.Trim() == empresa.Trim() && s.TIPODOC == tipodoc && s.SERIE.Trim() == serie.Trim());

    private static string? NormalizarYValidar(SerieSage50 serie)
    {
        serie.EMPRESA = serie.EMPRESA?.Trim().ToUpperInvariant() ?? string.Empty;
        serie.SERIE = serie.SERIE?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(serie.EMPRESA) || serie.EMPRESA.Length > 2) return "La empresa Sage debe tener uno o dos caracteres.";
        if (string.IsNullOrWhiteSpace(serie.SERIE) || serie.SERIE.Length > 2) return "La serie debe tener uno o dos caracteres.";
        if (serie.TIPODOC is < 1 or > 8) return "El tipo de documento debe estar entre 1 y 8.";
        if (serie.CONTADOR < 0m) return "El contador no puede ser negativo.";
        return null;
    }
}
