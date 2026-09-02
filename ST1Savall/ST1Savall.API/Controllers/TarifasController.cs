using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/tarifas")]
public class TarifasController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TarifaCabecera>>> Get() =>
        await context.TarifasCabeceras.AsNoTracking().Include(t => t.Lineas).OrderBy(t => t.Codigo).ToListAsync();

    [HttpPost]
    public async Task<ActionResult<TarifaCabecera>> Post(TarifaCabecera tarifa)
    {
        var error = Validar(tarifa); if (error is not null) return BadRequest(new { message = error });
        if (await context.TarifasCabeceras.AnyAsync(t => t.Codigo == tarifa.Codigo)) return Conflict(new { message = "Ya existe una tarifa con ese código." });
        PrepararLineas(tarifa); context.TarifasCabeceras.Add(tarifa); await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), tarifa);
    }

    [HttpPut("{codigo}")]
    public async Task<IActionResult> Put(string codigo, TarifaCabecera datos)
    {
        var actual = await context.TarifasCabeceras.Include(t => t.Lineas).FirstOrDefaultAsync(t => t.Codigo == codigo.Trim());
        if (actual is null) return NotFound(new { message = "No se encuentra la tarifa." });
        datos.Codigo = actual.Codigo.Trim();
        var error = Validar(datos); if (error is not null) return BadRequest(new { message = error });
        actual.Nombre = datos.Nombre; actual.Desde = datos.Desde; actual.Hasta = datos.Hasta; actual.Zona = datos.Zona;
        context.TarifasLineas.RemoveRange(actual.Lineas);
        actual.Lineas = datos.Lineas;
        PrepararLineas(actual);
        await context.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("{codigo}")]
    public async Task<IActionResult> Delete(string codigo)
    {
        var tarifa = await context.TarifasCabeceras.Include(t => t.Lineas).FirstOrDefaultAsync(t => t.Codigo == codigo.Trim());
        if (tarifa is null) return NotFound(); context.TarifasCabeceras.Remove(tarifa); await context.SaveChangesAsync(); return NoContent();
    }

    private static void PrepararLineas(TarifaCabecera tarifa)
    {
        foreach (var linea in tarifa.Lineas) { linea.Codigo = 0; linea.Tarifa = tarifa.Codigo; linea.Articulo = linea.Articulo.Trim().ToUpperInvariant(); linea.Precio = Math.Round(linea.Precio, 2, MidpointRounding.AwayFromZero); }
    }
    private static string? Validar(TarifaCabecera tarifa)
    {
        tarifa.Codigo = tarifa.Codigo?.Trim().ToUpperInvariant() ?? ""; tarifa.Nombre = tarifa.Nombre?.Trim() ?? ""; tarifa.Zona = tarifa.Zona?.Trim().ToUpperInvariant() ?? ""; tarifa.Lineas ??= [];
        if (tarifa.Codigo.Length is < 1 or > 2) return "El código de tarifa debe tener uno o dos caracteres.";
        if (string.IsNullOrWhiteSpace(tarifa.Nombre) || tarifa.Nombre.Length > 30) return "Debe indicar un nombre de hasta 30 caracteres.";
        if (tarifa.Zona.Length > 4) return "La zona no puede superar cuatro caracteres.";
        if (tarifa.Hasta < tarifa.Desde) return "La fecha hasta no puede ser anterior a la fecha desde.";
        if (tarifa.Lineas.Any(l => string.IsNullOrWhiteSpace(l.Articulo) || l.Articulo.Trim().Length > 8)) return "Cada línea debe indicar un artículo de hasta 8 caracteres.";
        if (tarifa.Lineas.Any(l => l.Precio < 0)) return "El precio no puede ser negativo.";
        if (tarifa.Lineas.GroupBy(l => l.Articulo.Trim(), StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1)) return "No puede repetir un artículo en la tarifa.";
        return null;
    }
}