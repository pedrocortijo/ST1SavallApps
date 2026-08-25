using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/precios-especiales-obra")]
public class PreciosEspecialesObraController(ApplicationDbContext app, SageGestionDbContext sage, SageComunDbContext comun) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PrecioEspecialCabecera>>> Get() =>
        await app.PreciosEspecialesCabeceras.AsNoTracking().Include(x => x.Detalles)
            .OrderBy(x => x.ObraSage).ToListAsync();

    [HttpPost]
    public async Task<ActionResult<PrecioEspecialCabecera>> Post(PrecioEspecialCabecera cabecera)
    {
        var error = await ValidarCabeceraAsync(cabecera, true);
        if (error is not null) return BadRequest(new { message = error });
        app.PreciosEspecialesCabeceras.Add(cabecera); await app.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = cabecera.IdPrecioEspecialCabecera }, cabecera);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, PrecioEspecialCabecera datos)
    {
        if (id != datos.IdPrecioEspecialCabecera) return BadRequest();
        var actual = await app.PreciosEspecialesCabeceras.FindAsync(id); if (actual is null) return NotFound();
        var error = await ValidarCabeceraAsync(datos, false); if (error is not null) return BadRequest(new { message = error });
        actual.ObraSage = datos.ObraSage; actual.VigenteDesde = datos.VigenteDesde; actual.VigenteHasta = datos.VigenteHasta; actual.Observaciones = datos.Observaciones;
        await app.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cabecera = await app.PreciosEspecialesCabeceras.FindAsync(id); if (cabecera is null) return NotFound();
        app.PreciosEspecialesCabeceras.Remove(cabecera); await app.SaveChangesAsync(); return NoContent();
    }

    [HttpPost("{id:int}/detalles")]
    public async Task<ActionResult<PrecioEspecialDetalle>> PostDetalle(int id, PrecioEspecialDetalle detalle)
    {
        if (!await app.PreciosEspecialesCabeceras.AnyAsync(x => x.IdPrecioEspecialCabecera == id)) return NotFound();
        detalle.IdPrecioEspecialCabecera = id; var error = await ValidarDetalleAsync(detalle, true); if (error is not null) return BadRequest(new { message = error });
        app.PreciosEspecialesDetalles.Add(detalle); await app.SaveChangesAsync(); return Ok(detalle);
    }

    [HttpPut("detalles/{id:int}")]
    public async Task<IActionResult> PutDetalle(int id, PrecioEspecialDetalle datos)
    {
        if (id != datos.IdPrecioEspecialDetalle) return BadRequest(); var actual = await app.PreciosEspecialesDetalles.FindAsync(id); if (actual is null) return NotFound();
        datos.IdPrecioEspecialCabecera = actual.IdPrecioEspecialCabecera; var error = await ValidarDetalleAsync(datos, false); if (error is not null) return BadRequest(new { message = error });
        actual.ArticuloSage = datos.ArticuloSage; actual.Precio = datos.Precio; await app.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("detalles/{id:int}")]
    public async Task<IActionResult> DeleteDetalle(int id)
    {
        var detalle = await app.PreciosEspecialesDetalles.FindAsync(id); if (detalle is null) return NotFound(); app.PreciosEspecialesDetalles.Remove(detalle); await app.SaveChangesAsync(); return NoContent();
    }

    private async Task<string?> ValidarCabeceraAsync(PrecioEspecialCabecera p, bool esNueva)
    {
        p.ObraSage = p.ObraSage?.Trim().ToUpperInvariant() ?? ""; p.Observaciones = string.IsNullOrWhiteSpace(p.Observaciones) ? null : p.Observaciones.Trim();
        if (string.IsNullOrWhiteSpace(p.ObraSage)) return "Debe indicar una obra.";
        if (p.VigenteDesde.HasValue && p.VigenteHasta.HasValue && p.VigenteHasta < p.VigenteDesde) return "La fecha final no puede ser anterior a la inicial.";
        if (!await comun.Obras.AsNoTracking().AnyAsync(o => o.Codigo == p.ObraSage)) return "La obra Sage no existe.";
        if (await app.PreciosEspecialesCabeceras.AnyAsync(x => x.ObraSage == p.ObraSage && x.IdPrecioEspecialCabecera != p.IdPrecioEspecialCabecera)) return "Ya existe una cabecera para esta obra.";
        return null;
    }

    private async Task<string?> ValidarDetalleAsync(PrecioEspecialDetalle p, bool esNuevo)
    {
        p.ArticuloSage = p.ArticuloSage?.Trim().ToUpperInvariant() ?? ""; if (string.IsNullOrWhiteSpace(p.ArticuloSage)) return "Debe indicar el artículo.";
        p.Precio = Math.Round(p.Precio, 2, MidpointRounding.AwayFromZero);
        if (!await sage.Articulos.AsNoTracking().AnyAsync(a => a.Codigo == p.ArticuloSage)) return "El artículo Sage no existe.";
        if (await app.PreciosEspecialesDetalles.AnyAsync(x => x.IdPrecioEspecialCabecera == p.IdPrecioEspecialCabecera && x.ArticuloSage == p.ArticuloSage && x.IdPrecioEspecialDetalle != p.IdPrecioEspecialDetalle)) return "Este artículo ya está incluido en el detalle.";
        return null;
    }
}
