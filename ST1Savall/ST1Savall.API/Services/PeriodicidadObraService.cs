using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Services;

/// <summary>Genera una única solicitud futura por obra y la repone al finalizarse.</summary>
public sealed class PeriodicidadObraService(ApplicationDbContext context, SageComunDbContext comunContext, SageGestionDbContext gestionContext, ILogger<PeriodicidadObraService> logger)
{
    private readonly ApplicationDbContext _context = context;
    private readonly SageComunDbContext _comunContext = comunContext;
    private readonly SageGestionDbContext _gestionContext = gestionContext;
    private readonly ILogger<PeriodicidadObraService> _logger = logger;

    public async Task SincronizarAsync(CancellationToken cancellationToken = default)
    {
        var finalizado = await ObtenerEstadoFinalizadoAsync(cancellationToken);
        var ejecuciones = await _context.PeriodicidadesObraEjecuciones.Include(e => e.Periodicidad)
            .Where(e => e.IdSolicitud.HasValue && e.Estado != EstadoEjecucionPeriodicidad.Realizada)
            .Join(_context.Solicitudes.Where(s => s.Estado == finalizado), e => e.IdSolicitud, s => s.IdSolicitud, (e, _) => e)
            .ToListAsync(cancellationToken);
        foreach (var ejecucion in ejecuciones) await MarcarRealizadaAsync(ejecucion, DateTime.Today, cancellationToken);
        var ids = await _context.PeriodicidadesObra.Where(p => p.Activa && p.ProximaFecha.HasValue)
            .Select(p => p.IdPeriodicidadObra).ToListAsync(cancellationToken);
        foreach (var id in ids) await MarcarPendienteConfirmacionAsync(id, cancellationToken);
    }

    public async Task RegistrarServicioRealizadoAsync(Solicitud solicitud, CancellationToken cancellationToken = default)
    {
        if (solicitud.IdPeriodicidadObraEjecucion.HasValue)
        {
            var ejecucion = await _context.PeriodicidadesObraEjecuciones.Include(e => e.Periodicidad)
                .FirstOrDefaultAsync(e => e.IdPeriodicidadObraEjecucion == solicitud.IdPeriodicidadObraEjecucion.Value, cancellationToken);
            if (ejecucion != null) await MarcarRealizadaAsync(ejecucion, DateTime.Today, cancellationToken);
            return;
        }        var candidatas = await _context.PeriodicidadesObra.Where(p => p.Activa &&
            p.TipoInicio == InicioPeriodicidad.PrimerServicioRealizado && p.FechaUltimaRealizada == null).ToListAsync(cancellationToken);
        var periodicidad = candidatas.FirstOrDefault(p => int.TryParse(p.ObraCodigo.Trim(), out var idObra) && idObra == solicitud.IdCliente);
        if (periodicidad == null) return;
        periodicidad.FechaUltimaRealizada = DateTime.Today;
        periodicidad.ProximaFecha = SumarPeriodo(DateTime.Today, periodicidad);
        periodicidad.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await MarcarPendienteConfirmacionAsync(periodicidad.IdPeriodicidadObra, cancellationToken);
    }

    public async Task MarcarPendienteConfirmacionAsync(int idPeriodicidadObra, CancellationToken cancellationToken = default)
    {
        var periodicidad = await _context.PeriodicidadesObra.FirstOrDefaultAsync(p => p.IdPeriodicidadObra == idPeriodicidadObra && p.Activa && p.ProximaFecha.HasValue, cancellationToken);
        if (periodicidad == null) return;
        if (periodicidad.ProximaFecha!.Value.Date > DateTime.Today)
        {
            if (periodicidad.PendienteConfirmacion)
            {
                periodicidad.PendienteConfirmacion = false;
                periodicidad.FechaActualizacion = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
            return;
        }
        if (periodicidad.PendienteConfirmacion) return;
        var existePendiente = await _context.PeriodicidadesObraEjecuciones.AnyAsync(e => e.IdPeriodicidadObra == idPeriodicidadObra && (e.Estado == EstadoEjecucionPeriodicidad.Pendiente || e.Estado == EstadoEjecucionPeriodicidad.Generada), cancellationToken);
        if (existePendiente) return;
        periodicidad.PendienteConfirmacion = true;
        periodicidad.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
    public async Task<int?> GenerarSiguienteAsync(int idPeriodicidadObra, CancellationToken cancellationToken = default)
    {
        var periodicidad = await _context.PeriodicidadesObra.FirstOrDefaultAsync(p => p.IdPeriodicidadObra == idPeriodicidadObra && p.Activa, cancellationToken);
        if (periodicidad?.ProximaFecha is not DateTime fecha) return null;
        periodicidad.PendienteConfirmacion = false;
        var pendiente = await _context.PeriodicidadesObraEjecuciones.AnyAsync(e => e.IdPeriodicidadObra == periodicidad.IdPeriodicidadObra &&
            (e.Estado == EstadoEjecucionPeriodicidad.Pendiente || e.Estado == EstadoEjecucionPeriodicidad.Generada), cancellationToken);
        if (pendiente) return null;
        var obra = await _comunContext.Obras.AsNoTracking().FirstOrDefaultAsync(o => o.Codigo.Trim() == periodicidad.ObraCodigo.Trim(), cancellationToken);
        if (obra == null) { _logger.LogWarning("No se ha generado la periodicidad {Id}: no existe la obra {Obra}.", periodicidad.IdPeriodicidadObra, periodicidad.ObraCodigo); return null; }
        var cliente = await _gestionContext.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Codigo.Trim() == obra.Cliente.Trim(), cancellationToken);
        var parametro = await _context.Parametros.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var estadoPendiente = parametro?.EstadoPendiente ?? await _context.EstadosSolicitud.AsNoTracking().Where(e => e.Descripcion.Contains("pendiente"))
            .Select(e => (int?)e.IdEstado).FirstOrDefaultAsync(cancellationToken) ?? 1;
        var ejecucion = new PeriodicidadObraEjecucion { IdPeriodicidadObra = periodicidad.IdPeriodicidadObra, FechaPrevista = fecha.Date, HoraPrevista = periodicidad.HoraPrevista };
        _context.PeriodicidadesObraEjecuciones.Add(ejecucion);
        await _context.SaveChangesAsync(cancellationToken);
        var solicitud = new Solicitud
        {
            IdTipoTarea = periodicidad.IdTipoTarea, IdUsuario = 1,
            IdCliente = int.TryParse(obra.Codigo.Trim(), out var idObra) ? idObra : 0,
            Estado = estadoPendiente, FechaSolicitud = DateTime.Today, FechaTarea = fecha.Date, FechaPrevista = fecha.Date, FechaInicial = fecha.Date,
            FechaHoraInicioPlanificada = fecha.Date.Add(periodicidad.HoraPrevista), IdPeriodicidadObraEjecucion = ejecucion.IdPeriodicidadObraEjecucion,
            NombreCliente = cliente?.Nombre?.Trim() ?? string.Empty, NombreObra = obra.Nombre?.Trim() ?? string.Empty,
            DireccionCliente = obra.Direccion?.Trim() ?? string.Empty, PoblacionCliente = obra.Poblacion?.Trim() ?? string.Empty,
            TelefonoCliente = obra.Telefono?.Trim() ?? string.Empty, Observaciones = obra.Observacio?.Trim(), TarifaAplicada = obra.Tarifa?.Trim()
        };
        _context.Solicitudes.Add(solicitud);
        ejecucion.Estado = EstadoEjecucionPeriodicidad.Generada;
        periodicidad.ProximaFecha = null; // No se crea otra hasta que ésta quede realizada.
        periodicidad.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        ejecucion.IdSolicitud = solicitud.IdSolicitud;
        await _context.SaveChangesAsync(cancellationToken);
        return solicitud.IdSolicitud;
    }

    private async Task MarcarRealizadaAsync(PeriodicidadObraEjecucion ejecucion, DateTime fecha, CancellationToken cancellationToken)
    {
        if (ejecucion.Estado == EstadoEjecucionPeriodicidad.Realizada || ejecucion.Periodicidad == null) return;
        ejecucion.Estado = EstadoEjecucionPeriodicidad.Realizada;
        ejecucion.FechaRealizacion = fecha.Date;
        ejecucion.Periodicidad.FechaUltimaRealizada = fecha.Date;
        ejecucion.Periodicidad.ProximaFecha = SumarPeriodo(fecha.Date, ejecucion.Periodicidad);
        ejecucion.Periodicidad.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await MarcarPendienteConfirmacionAsync(ejecucion.Periodicidad.IdPeriodicidadObra, cancellationToken);
    }

    private async Task<int> ObtenerEstadoFinalizadoAsync(CancellationToken cancellationToken) =>
        (await _context.Parametros.AsNoTracking().FirstOrDefaultAsync(cancellationToken))?.EstadoFinalizado ??
        await _context.EstadosSolicitud.AsNoTracking().Where(e => e.Descripcion.Contains("finalizado")).Select(e => (int?)e.IdEstado).FirstOrDefaultAsync(cancellationToken) ?? 5;

    public static DateTime SumarPeriodo(DateTime fecha, PeriodicidadObra p)
    {
        if (p.Unidad == UnidadPeriodicidad.Semanas)
        {
            var seleccionados = new[] { p.L, p.M, p.X, p.J, p.V, p.S, p.D };
            if (seleccionados.Any(dia => dia == 1))
            {
                for (var desplazamiento = 1; desplazamiento <= 7; desplazamiento++)
                {
                    var candidata = fecha.AddDays(desplazamiento);
                    var indice = ((int)candidata.DayOfWeek + 6) % 7;
                    if (seleccionados[indice] == 1) return candidata;
                }
            }
            return fecha.AddDays(p.Cantidad * 7);
        }
        return p.Unidad == UnidadPeriodicidad.Dias ? fecha.AddDays(p.Cantidad) :
            p.Unidad == UnidadPeriodicidad.Meses ? fecha.AddMonths(p.Cantidad) : fecha;
    }
}