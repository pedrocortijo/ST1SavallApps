using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/zonas")]
public class ZonasController(SageComunDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ZonaComunSage50>>> Get()
    {
        var lista = await context.Zonas.AsNoTracking().OrderBy(z => z.Ruta).ThenBy(z => z.Zona).ToListAsync();
        foreach (var z in lista)
        {
            z.Ruta = z.Ruta?.Trim() ?? "";
            z.Zona = z.Zona?.Trim() ?? "";
            z.Descripcion = z.Descripcion?.Trim() ?? "";
        }
        return Ok(lista);
    }

    [HttpPost]
    public async Task<ActionResult<ZonaComunSage50>> Post(ZonaComunSage50 zona)
    {
        var error = NormalizarYValidar(zona);
        if (error is not null) return BadRequest(new { message = error });
        if (await ExisteAsync(zona.Ruta, zona.Zona)) return Conflict(new { message = "Ya existe esa zona para la ruta indicada." });
        var now = DateTime.Now;
        zona.Linia = await context.Zonas.Where(z => z.Ruta.Trim() == zona.Ruta).Select(z => (int?)z.Linia).MaxAsync() ?? 0;
        zona.Linia++;
        zona.Guid = Guid.NewGuid().ToString(); zona.GuidExp = Guid.NewGuid().ToString(); zona.GuidId = Guid.NewGuid().ToString();
        zona.Created = now; zona.Modified = now;
        context.Zonas.Add(zona); await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), zona);
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromQuery] string ruta, [FromQuery] string zona, ZonaComunSage50 datos)
    {
        var actual = await BuscarAsync(ruta, zona);
        if (actual is null) return NotFound(new { message = "No se encuentra la zona." });
        var error = NormalizarYValidar(datos);
        if (error is not null) return BadRequest(new { message = error });
        var cambiaClave = actual.Ruta.Trim() != datos.Ruta || actual.Zona.Trim() != datos.Zona;
        if (cambiaClave && await ExisteAsync(datos.Ruta, datos.Zona)) return Conflict(new { message = "Ya existe esa zona para la ruta indicada." });
        actual.Ruta = datos.Ruta; actual.Zona = datos.Zona; actual.Descripcion = datos.Descripcion; actual.Vista = datos.Vista; actual.Modified = DateTime.Now;
        await context.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] string ruta, [FromQuery] string zona)
    {
        var actual = await BuscarAsync(ruta, zona); if (actual is null) return NotFound();
        context.Zonas.Remove(actual); await context.SaveChangesAsync(); return NoContent();
    }

    private Task<bool> ExisteAsync(string ruta, string zona) => context.Zonas.AnyAsync(z => z.Ruta.Trim() == ruta && z.Zona.Trim() == zona);
    private Task<ZonaComunSage50?> BuscarAsync(string ruta, string zona) => context.Zonas.FirstOrDefaultAsync(z => z.Ruta.Trim() == ruta.Trim() && z.Zona.Trim() == zona.Trim());
    private static string? NormalizarYValidar(ZonaComunSage50 zona)
    {
        zona.Ruta = "01"; zona.Zona = zona.Zona?.Trim().ToUpperInvariant() ?? ""; zona.Descripcion = zona.Descripcion?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(zona.Ruta) || zona.Ruta.Length > 2) return "La ruta debe tener entre uno y dos caracteres.";
        if (string.IsNullOrWhiteSpace(zona.Zona) || zona.Zona.Length > 4) return "La zona debe tener entre uno y cuatro caracteres.";
        if (string.IsNullOrWhiteSpace(zona.Descripcion) || zona.Descripcion.Length > 50) return "Debe indicar una descripción de hasta 50 caracteres.";
        return null;
    }
}