using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/precios-especiales-obra")]
public class PreciosEspecialesObraController(
    ApplicationDbContext applicationContext,
    SageGestionDbContext sageGestionContext,
    SageComunDbContext sageComunContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PrecioEspecialObra>>> Get() =>
        await applicationContext.PreciosEspecialesObra.AsNoTracking()
            .OrderBy(p => p.ClienteSage).ThenBy(p => p.ObraSage).ThenBy(p => p.ArticuloSage).ToListAsync();

    [HttpPost]
    public async Task<ActionResult<PrecioEspecialObra>> Post(PrecioEspecialObra precio)
    {
        var error = await ValidarAsync(precio, true);
        if (error is not null) return BadRequest(new { message = error });
        applicationContext.PreciosEspecialesObra.Add(precio);
        await applicationContext.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = precio.Id }, precio);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, PrecioEspecialObra precio)
    {
        if (id != precio.Id) return BadRequest();
        var error = await ValidarAsync(precio, false);
        if (error is not null) return BadRequest(new { message = error });
        if (!await applicationContext.PreciosEspecialesObra.AnyAsync(p => p.Id == id)) return NotFound();
        applicationContext.Entry(precio).State = EntityState.Modified;
        await applicationContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var precio = await applicationContext.PreciosEspecialesObra.FindAsync(id);
        if (precio is null) return NotFound();
        applicationContext.PreciosEspecialesObra.Remove(precio);
        await applicationContext.SaveChangesAsync();
        return NoContent();
    }

    private async Task<string?> ValidarAsync(PrecioEspecialObra precio, bool isNew)
    {
        precio.ClienteSage = precio.ClienteSage?.Trim().ToUpperInvariant() ?? string.Empty;
        precio.ObraSage = precio.ObraSage?.Trim().ToUpperInvariant() ?? string.Empty;
        precio.ArticuloSage = precio.ArticuloSage?.Trim().ToUpperInvariant() ?? string.Empty;
        precio.Observaciones = string.IsNullOrWhiteSpace(precio.Observaciones) ? null : precio.Observaciones.Trim();
        if (string.IsNullOrWhiteSpace(precio.ClienteSage) || string.IsNullOrWhiteSpace(precio.ObraSage) || string.IsNullOrWhiteSpace(precio.ArticuloSage)) return "Debe indicar cliente, obra y artículo.";
        if (precio.VigenteDesde.HasValue && precio.VigenteHasta.HasValue && precio.VigenteHasta < precio.VigenteDesde) return "La fecha final no puede ser anterior a la fecha inicial.";
        if (!await sageGestionContext.Clientes.AsNoTracking().AnyAsync(c => c.Codigo == precio.ClienteSage)) return "El cliente Sage indicado no existe.";
        var obra = await sageComunContext.Obras.AsNoTracking().FirstOrDefaultAsync(o => o.Codigo == precio.ObraSage);
        if (obra is null) return "La obra Sage indicada no existe.";
        if (!string.Equals(obra.Cliente.Trim(), precio.ClienteSage, StringComparison.OrdinalIgnoreCase)) return "La obra seleccionada no pertenece al cliente Sage indicado.";
        if (!await sageGestionContext.Articulos.AsNoTracking().AnyAsync(a => a.Codigo == precio.ArticuloSage)) return "El artículo Sage indicado no existe.";
        if (isNew && await applicationContext.PreciosEspecialesObra.AnyAsync(p => p.ClienteSage == precio.ClienteSage && p.ObraSage == precio.ObraSage && p.ArticuloSage == precio.ArticuloSage)) return "Ya existe un precio especial para este cliente, obra y artículo.";
        return null;
    }
}
