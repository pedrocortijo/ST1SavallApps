using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Services;

public class PlanificacionService
{
    private const int EstadoAnulado = 6;
    private readonly ApplicationDbContext _context;

    public PlanificacionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string?> PrepararYValidarAsync(Solicitud solicitud)
    {
        if (!solicitud.FechaHoraInicioPlanificada.HasValue)
            return null;

        if (solicitud.FechaTarea.HasValue)
        {
            var targetDate = solicitud.FechaTarea.Value.Date;
            solicitud.FechaHoraInicioPlanificada = targetDate + solicitud.FechaHoraInicioPlanificada.Value.TimeOfDay;
            if (solicitud.FechaHoraFinPlanificada.HasValue)
            {
                solicitud.FechaHoraFinPlanificada = targetDate + solicitud.FechaHoraFinPlanificada.Value.TimeOfDay;
            }
        }
        else
        {
            solicitud.FechaTarea = solicitud.FechaHoraInicioPlanificada.Value.Date;
        }

        var redondeoHora = await ObtenerRedondeoHoraAsync();
        solicitud.FechaHoraInicioPlanificada = RedondearAlIntervalo(solicitud.FechaHoraInicioPlanificada.Value, redondeoHora);

        var duracion = solicitud.DuracionPlanificadaMinutos.GetValueOrDefault();
        if (duracion <= 0 && solicitud.FechaHoraFinPlanificada > solicitud.FechaHoraInicioPlanificada)
            duracion = (int)Math.Ceiling((solicitud.FechaHoraFinPlanificada.Value - solicitud.FechaHoraInicioPlanificada.Value).TotalMinutes);
        if (duracion <= 0)
            return "La duración planificada debe ser mayor que cero.";

        solicitud.DuracionPlanificadaMinutos = duracion;
        solicitud.FechaHoraFinPlanificada = RedondearAlIntervalo(solicitud.FechaHoraInicioPlanificada.Value.AddMinutes(duracion), redondeoHora);
        solicitud.HoraLlegada = solicitud.FechaHoraInicioPlanificada;

        if (!solicitud.IdConductor.HasValue)
            return "Debe seleccionar un conductor para planificar el servicio.";

        return await ValidarDisponibilidadAsync(
            solicitud.IdConductor.Value,
            solicitud.FechaHoraInicioPlanificada.Value,
            solicitud.FechaHoraFinPlanificada.Value,
            solicitud.IdSolicitud);
    }

    public async Task<string?> ValidarDisponibilidadAsync(int idConductor, DateTime inicio, DateTime fin, int excluirSolicitudId = 0)
    {
        if (fin <= inicio)
            return "La hora de finalización debe ser posterior a la hora de inicio.";

        var operario = await _context.Operarios.AsNoTracking().FirstOrDefaultAsync(o => o.IdOperario == idConductor);
        if (operario == null)
            return "El conductor seleccionado no existe.";

        if (operario.Activo == false &&
            !string.Equals(operario.EstadoLaboral, "Inactivo", StringComparison.OrdinalIgnoreCase))
            return "El conductor está desactivado.";

        var inicioJornada = operario.InicioJornada == TimeSpan.Zero ? new TimeSpan(8, 0, 0) : operario.InicioJornada;
        var finJornada = operario.FinJornada == TimeSpan.Zero ? new TimeSpan(17, 0, 0) : operario.FinJornada;

        if (inicio.TimeOfDay < inicioJornada || fin.TimeOfDay > finJornada)
        {
            return $"El horario seleccionado ({inicio:HH:mm}–{fin:HH:mm}) está fuera de la jornada laboral del conductor ({inicioJornada:hh\\:mm}–{finJornada:hh\\:mm}).";
        }

        if (EstaInactivo(operario, inicio, fin))
            return $"El conductor está inactivo{GetMotivo(operario)} durante el intervalo seleccionado.";

        var solape = await _context.Solicitudes.AsNoTracking().AnyAsync(s =>
            s.IdSolicitud != excluirSolicitudId &&
            s.IdConductor == idConductor &&
            s.Estado != EstadoAnulado &&
            s.FechaHoraInicioPlanificada.HasValue &&
            s.FechaHoraFinPlanificada.HasValue &&
            s.FechaHoraInicioPlanificada < fin &&
            s.FechaHoraFinPlanificada > inicio);
        if (solape)
            return "El conductor ya tiene otro servicio dentro de ese intervalo.";

        return null;
    }

    public async Task<PlanificacionHueco> BuscarSiguienteHuecoAsync(int idConductor, DateTime desde, int duracionMinutos, int excluirSolicitudId = 0)
    {
        if (duracionMinutos <= 0)
            return new PlanificacionHueco { Mensaje = "La duración debe ser mayor que cero." };

        var operario = await _context.Operarios.AsNoTracking().FirstOrDefaultAsync(o => o.IdOperario == idConductor);
        if (operario == null)
            return new PlanificacionHueco { Mensaje = "El conductor seleccionado no existe." };
        if (operario.Activo == false &&
            !string.Equals(operario.EstadoLaboral, "Inactivo", StringComparison.OrdinalIgnoreCase))
            return new PlanificacionHueco { Mensaje = "El conductor está desactivado." };

        var inicioJornada = operario.InicioJornada == TimeSpan.Zero ? new TimeSpan(8, 0, 0) : operario.InicioJornada;
        var finJornada = operario.FinJornada == TimeSpan.Zero ? new TimeSpan(17, 0, 0) : operario.FinJornada;

        var redondeoHora = await ObtenerRedondeoHoraAsync();
        var candidato = RedondearAlIntervalo(desde, redondeoHora);
        if (candidato.TimeOfDay < inicioJornada)
        {
            candidato = candidato.Date + inicioJornada;
        }

        var limite = desde.AddDays(90);
        while (candidato < limite)
        {
            if (candidato.TimeOfDay < inicioJornada)
            {
                candidato = candidato.Date + inicioJornada;
            }

            var fin = RedondearAlIntervalo(candidato.AddMinutes(duracionMinutos), redondeoHora);
            if (fin.TimeOfDay > finJornada || candidato.TimeOfDay >= finJornada)
            {
                candidato = candidato.Date.AddDays(1) + inicioJornada;
                continue;
            }

            if (operario.InicioDescanso.HasValue && operario.FinDescanso.HasValue)
            {
                if (candidato.TimeOfDay < operario.FinDescanso.Value && fin.TimeOfDay > operario.InicioDescanso.Value)
                {
                    candidato = candidato.Date + operario.FinDescanso.Value;
                    continue;
                }
            }

            if (string.Equals(operario.EstadoLaboral, "Inactivo", StringComparison.OrdinalIgnoreCase) &&
                operario.InactivoHasta.HasValue && candidato <= operario.InactivoHasta.Value)
            {
                candidato = RedondearAlIntervalo(operario.InactivoHasta.Value.AddTicks(1), redondeoHora);
                continue;
            }

            var ocupado = await _context.Solicitudes.AsNoTracking()
                .Where(s => s.IdSolicitud != excluirSolicitudId && s.IdConductor == idConductor && s.Estado != EstadoAnulado &&
                            s.FechaHoraInicioPlanificada < fin && s.FechaHoraFinPlanificada > candidato)
                .OrderBy(s => s.FechaHoraInicioPlanificada)
                .Select(s => new { Inicio = s.FechaHoraInicioPlanificada!.Value, Fin = s.FechaHoraFinPlanificada!.Value })
                .FirstOrDefaultAsync();
            if (ocupado != null)
            {
                candidato = RedondearAlIntervalo(ocupado.Fin, redondeoHora);
                continue;
            }

            var error = await ValidarDisponibilidadAsync(idConductor, candidato, fin, excluirSolicitudId);
            if (error == null)
                return new PlanificacionHueco { Disponible = true, Inicio = candidato, Fin = fin };

            candidato = candidato.AddMinutes(redondeoHora);
        }

        return new PlanificacionHueco { Mensaje = "No se encontró un hueco disponible dentro de la jornada laboral en los próximos 90 días." };
    }

    private static bool EstaInactivo(Operario operario, DateTime inicio, DateTime fin)
    {
        if (!string.Equals(operario.EstadoLaboral, "Inactivo", StringComparison.OrdinalIgnoreCase))
            return false;

        var desde = operario.InactivoDesde ?? DateTime.MinValue;
        var hasta = operario.InactivoHasta ?? DateTime.MaxValue;
        return inicio < hasta && fin > desde;
    }

    private static string GetMotivo(Operario operario) => string.IsNullOrWhiteSpace(operario.MotivoInactividad)
        ? string.Empty
        : $" por {operario.MotivoInactividad}";

    private async Task<int> ObtenerRedondeoHoraAsync()
    {
        var redondeoHora = await _context.Parametros.AsNoTracking()
            .Select(p => p.RedondeoHora)
            .FirstOrDefaultAsync();
        return redondeoHora > 0 ? redondeoHora : 5;
    }

    private static DateTime RedondearAlIntervalo(DateTime value, int intervaloMinutos)
    {
        var intervalo = TimeSpan.FromMinutes(intervaloMinutos > 0 ? intervaloMinutos : 5).Ticks;
        return new DateTime((value.Ticks + intervalo - 1) / intervalo * intervalo, value.Kind);
    }
}
