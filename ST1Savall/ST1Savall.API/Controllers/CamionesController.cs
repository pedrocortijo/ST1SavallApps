using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CamionesController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Camion>>> GetCamiones() =>
        await context.Camiones.AsNoTracking().Include(c => c.Conductor).OrderBy(c => c.Matricula).ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Camion>> GetCamion(int id)
    {
        var camion = await context.Camiones.AsNoTracking().Include(c => c.Conductor).FirstOrDefaultAsync(c => c.IdCamion == id);
        return camion is null ? NotFound() : camion;
    }

    [HttpPost]
    public async Task<ActionResult<Camion>> PostCamion(Camion camion)
    {
        var error = await ValidarAsync(camion);
        if (error is not null) return BadRequest(new { message = error });

        context.Camiones.Add(camion);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCamion), new { id = camion.IdCamion }, camion);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutCamion(int id, Camion camion)
    {
        if (id != camion.IdCamion) return BadRequest();

        var error = await ValidarAsync(camion);
        if (error is not null) return BadRequest(new { message = error });
        if (!await context.Camiones.AnyAsync(c => c.IdCamion == id)) return NotFound();

        context.Entry(camion).State = EntityState.Modified;
        context.Entry(camion).Reference(c => c.Conductor).IsModified = false;
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCamion(int id)
    {
        var camion = await context.Camiones.FindAsync(id);
        if (camion is null) return NotFound();

        context.Camiones.Remove(camion);
        await context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<string?> ValidarAsync(Camion camion)
    {
        camion.Matricula = camion.Matricula.Trim().ToUpperInvariant();
        camion.Descripcion = string.IsNullOrWhiteSpace(camion.Descripcion) ? null : camion.Descripcion.Trim();
        camion.UnidadWialonId = string.IsNullOrWhiteSpace(camion.UnidadWialonId) ? null : camion.UnidadWialonId.Trim();

        if (string.IsNullOrWhiteSpace(camion.Matricula)) return "Debe indicar la matrícula.";
        if (camion.IdConductor.HasValue && !await context.Operarios.AnyAsync(o => o.IdOperario == camion.IdConductor.Value))
            return "El conductor seleccionado no existe.";
        if (await context.Camiones.AnyAsync(c => c.IdCamion != camion.IdCamion && c.Matricula == camion.Matricula))
            return "Ya existe un camión con esta matrícula.";
        if (camion.UnidadWialonId is not null && await context.Camiones.AnyAsync(c => c.IdCamion != camion.IdCamion && c.UnidadWialonId == camion.UnidadWialonId))
            return "Esta unidad de Wialon ya está asignada a otro camión.";
        return null;
    }
}
