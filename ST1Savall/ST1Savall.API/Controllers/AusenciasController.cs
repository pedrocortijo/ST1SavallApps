using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.API.Services;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AusenciasController(ApplicationDbContext context, EstadoOperariosAusenciasService estadoOperariosAusenciasService) : ControllerBase
{
    [HttpGet]
    public Task<List<Ausencia>> Get([FromQuery] int? idConductor) =>
        context.Ausencias.AsNoTracking()
            .Where(a => !idConductor.HasValue || a.IdConductor == idConductor.Value)
            .OrderByDescending(a => a.FechaInicio)
            .ToListAsync();

    [HttpPost]
    public async Task<ActionResult<Ausencia>> Post(Ausencia ausencia)
    {
        var error = await ValidarAsync(ausencia);
        if (error is not null) return BadRequest(new { message = error });
        var conflicto = await ObtenerConflictoAgendaAsync(ausencia);
        if (conflicto is not null) return Conflict(new { message = conflicto });
        context.Ausencias.Add(ausencia);
        await context.SaveChangesAsync();
        await estadoOperariosAusenciasService.SincronizarAsync();
        return CreatedAtAction(nameof(Get), new { idConductor = ausencia.IdConductor }, ausencia);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, Ausencia ausencia)
    {
        if (id != ausencia.IdAusencia) return BadRequest();
        var error = await ValidarAsync(ausencia);
        if (error is not null) return BadRequest(new { message = error });
        var conflicto = await ObtenerConflictoAgendaAsync(ausencia);
        if (conflicto is not null) return Conflict(new { message = conflicto });
        context.Entry(ausencia).State = EntityState.Modified;
        await context.SaveChangesAsync();
        await estadoOperariosAusenciasService.SincronizarAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ausencia = await context.Ausencias.FindAsync(id);
        if (ausencia is null) return NotFound();
        context.Ausencias.Remove(ausencia);
        await context.SaveChangesAsync();
        await estadoOperariosAusenciasService.SincronizarAsync();
        return NoContent();
    }

    private async Task<string?> ValidarAsync(Ausencia ausencia)
    {
        if (!await context.Operarios.AnyAsync(o => o.IdOperario == ausencia.IdConductor)) return "El conductor seleccionado no existe.";
        if (ausencia.FechaFin < ausencia.FechaInicio) return "La fecha de fin no puede ser anterior a la fecha de inicio.";
        if (string.IsNullOrWhiteSpace(ausencia.Tipo)) return "Debe indicar el tipo de ausencia.";
        ausencia.Tipo = ausencia.Tipo.Trim();
        return null;
    }

    private async Task<string?> ObtenerConflictoAgendaAsync(Ausencia ausencia)
    {
        var inicio = ausencia.FechaInicio.ToDateTime(TimeOnly.MinValue);
        var fin = ausencia.FechaFin.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var servicios = await context.Solicitudes.AsNoTracking()
            .Where(s => s.IdConductor == ausencia.IdConductor && s.Estado != 6 &&
                ((s.FechaHoraInicioPlanificada.HasValue && s.FechaHoraInicioPlanificada < fin &&
                  (s.FechaHoraFinPlanificada == null || s.FechaHoraFinPlanificada > inicio)) ||
                 (!s.FechaHoraInicioPlanificada.HasValue && s.FechaTarea.HasValue &&
                  s.FechaTarea >= inicio && s.FechaTarea < fin)))
            .OrderBy(s => s.FechaHoraInicioPlanificada ?? s.FechaTarea)
            .Select(s => new { s.IdSolicitud, s.FechaHoraInicioPlanificada, s.FechaTarea })
            .ToListAsync();

        if (servicios.Count == 0) return null;

        var detalle = string.Join(", ", servicios.Take(5).Select(s =>
            $"#{s.IdSolicitud} ({(s.FechaHoraInicioPlanificada ?? s.FechaTarea):dd/MM/yyyy HH:mm})"));
        var resto = servicios.Count > 5 ? $" y {servicios.Count - 5} más" : string.Empty;
        return $"No se puede registrar la ausencia: el operario tiene {servicios.Count} servicio(s) asignado(s) en esas fechas: {detalle}{resto}.";
    }
}
