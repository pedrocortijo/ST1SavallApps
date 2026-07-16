using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using ST1Savall.API.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SolicitudesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly PlanificacionService _planificacionService;
    private readonly CalculoRutaSolicitudService _calculoRutaService;

    public SolicitudesController(
        ApplicationDbContext context,
        PlanificacionService planificacionService,
        CalculoRutaSolicitudService calculoRutaService)
    {
        _context = context;
        _planificacionService = planificacionService;
        _calculoRutaService = calculoRutaService;
    }

    [HttpPost("calcular-ruta")]
    public async Task<ActionResult<CalculoRutaSolicitudResultado>> CalcularRuta(
        Solicitud solicitud,
        bool forzarActualizacion = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _calculoRutaService.CalcularYAplicarAsync(
                solicitud, forzarActualizacion, cancellationToken));
        }
        catch (ProveedorRutasException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    [HttpGet("siguiente-hueco")]
    public async Task<ActionResult<PlanificacionHueco>> GetSiguienteHueco(
        int idConductor,
        DateTime fecha,
        int duracionMinutos,
        int excluirSolicitudId = 0)
    {
        return Ok(await _planificacionService.BuscarSiguienteHuecoAsync(
            idConductor, fecha, duracionMinutos, excluirSolicitudId));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Solicitud>>> GetSolicitudes()
    {
        return await _context.Solicitudes.ToListAsync();
    }

    [HttpGet("con-contenedores")]
    public async Task<ActionResult<IEnumerable<Solicitud>>> GetSolicitudesConContenedores()
    {
        var solicitudes = await _context.Solicitudes
            .Where(s1 => s1.CodigoEntrega != null && s1.CodigoEntrega != "" && s1.Estado != 6)
            .Where(s1 => !_context.Solicitudes.Any(s2 => 
                s2.CodigoRecogida == s1.CodigoEntrega && 
                s2.Estado != 6 && 
                (
                    (s2.FechaTarea ?? s2.FechaSolicitud ?? DateTime.MinValue) > (s1.FechaTarea ?? s1.FechaSolicitud ?? DateTime.MinValue) ||
                    ((s2.FechaTarea ?? s2.FechaSolicitud ?? DateTime.MinValue) == (s1.FechaTarea ?? s1.FechaSolicitud ?? DateTime.MinValue) && s2.IdSolicitud > s1.IdSolicitud)
                )
            ))
            .ToListAsync();

        return Ok(solicitudes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Solicitud>> GetSolicitud(int id)
    {
        var solicitud = await _context.Solicitudes
            .FirstOrDefaultAsync(s => s.IdSolicitud == id);
            
        if (solicitud == null) return NotFound();
        return solicitud;
    }

    [HttpPost]
    public async Task<ActionResult<Solicitud>> PostSolicitud(Solicitud solicitud)
    {
        var errorRuta = await CalcularRutaAutomaticamenteAsync(solicitud);
        if (errorRuta != null) return errorRuta;

        var errorPlanificacion = await _planificacionService.PrepararYValidarAsync(solicitud);
        if (errorPlanificacion != null)
            return Conflict(new { message = errorPlanificacion });

        _context.Solicitudes.Add(solicitud);
        await ActualizarEstadosContenedores(solicitud);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSolicitud), new { id = solicitud.IdSolicitud }, solicitud);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutSolicitud(int id, Solicitud solicitud)
    {
        if (id != solicitud.IdSolicitud) return BadRequest();
        var errorRuta = await CalcularRutaAutomaticamenteAsync(solicitud);
        if (errorRuta != null) return errorRuta;

        var errorPlanificacion = await _planificacionService.PrepararYValidarAsync(solicitud);
        if (errorPlanificacion != null)
            return Conflict(new { message = errorPlanificacion });

        _context.Entry(solicitud).State = EntityState.Modified;
        await ActualizarEstadosContenedores(solicitud);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SolicitudExists(id)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSolicitud(int id)
    {
        var solicitud = await _context.Solicitudes.FindAsync(id);
        if (solicitud == null) return NotFound();
        _context.Solicitudes.Remove(solicitud);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task ActualizarEstadosContenedores(Solicitud solicitud)
    {
        if (!string.IsNullOrEmpty(solicitud.CodigoEntrega))
        {
            var contenedorEntrega = await _context.Contenedores
                .FirstOrDefaultAsync(c => c.NumSerie == solicitud.CodigoEntrega);
            if (contenedorEntrega != null)
            {
                contenedorEntrega.EstadoFisico = "Entregado";
                _context.Entry(contenedorEntrega).State = EntityState.Modified;
            }
        }

        if (!string.IsNullOrEmpty(solicitud.CodigoRecogida))
        {
            var contenedorRecogida = await _context.Contenedores
                .FirstOrDefaultAsync(c => c.NumSerie == solicitud.CodigoRecogida);
            if (contenedorRecogida != null)
            {
                contenedorRecogida.EstadoFisico = "Disponible";
                _context.Entry(contenedorRecogida).State = EntityState.Modified;
            }
        }
    }

    private bool SolicitudExists(int id)
    {
        return _context.Solicitudes.Any(e => e.IdSolicitud == id);
    }

    private async Task<ObjectResult?> CalcularRutaAutomaticamenteAsync(Solicitud solicitud)
    {
        if (!CalculoRutaSolicitudService.TieneDatosCompletos(solicitud))
            return null;

        try
        {
            await _calculoRutaService.CalcularYAplicarAsync(solicitud);
            return null;
        }
        catch (ProveedorRutasException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }
}
