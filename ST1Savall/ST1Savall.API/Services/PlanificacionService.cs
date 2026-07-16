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

        var duracion = solicitud.DuracionPlanificadaMinutos.GetValueOrDefault();
        if (duracion <= 0 && solicitud.FechaHoraFinPlanificada > solicitud.FechaHoraInicioPlanificada)
            duracion = (int)Math.Ceiling((solicitud.FechaHoraFinPlanificada.Value - solicitud.FechaHoraInicioPlanificada.Value).TotalMinutes);
        if (duracion <= 0)
            return "La duración planificada debe ser mayor que cero.";

        solicitud.DuracionPlanificadaMinutos = duracion;
        solicitud.FechaHoraFinPlanificada = solicitud.FechaHoraInicioPlanificada.Value.AddMinutes(duracion);
        solicitud.FechaTarea = solicitud.FechaHoraInicioPlanificada.Value.Date;
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

        if (EstaInactivo(operario, inicio, fin))
            return $"El conductor está inactivo{GetMotivo(operario)} durante el intervalo seleccionado.";

        if (!TrabajaEseDia(operario, inicio.Date))
            return "El conductor no trabaja el día seleccionado.";

        var jornadaInicio = inicio.Date.Add(operario.HoraInicioJornada ?? new TimeSpan(8, 0, 0));
        var jornadaFin = inicio.Date.Add(operario.HoraFinJornada ?? new TimeSpan(17, 0, 0));
        if (inicio < jornadaInicio || fin > jornadaFin)
            return $"El servicio debe estar dentro del horario {jornadaInicio:HH:mm}–{jornadaFin:HH:mm}.";

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

        var minutosServicio = (int)Math.Ceiling((fin - inicio).TotalMinutes);
        var inicioDia = inicio.Date;
        var finDia = inicioDia.AddDays(1);
        var minutosDia = await SumarMinutosAsync(idConductor, inicioDia, finDia, excluirSolicitudId);
        if (operario.MinutosMaximosDiarios > 0 && minutosDia + minutosServicio > operario.MinutosMaximosDiarios)
            return $"Se superaría el máximo diario de {operario.MinutosMaximosDiarios / 60d:0.##} horas.";

        var inicioSemana = inicio.Date.AddDays(-(((int)inicio.DayOfWeek + 6) % 7));
        var minutosSemana = await SumarMinutosAsync(idConductor, inicioSemana, inicioSemana.AddDays(7), excluirSolicitudId);
        if (operario.MinutosMaximosSemanales > 0 && minutosSemana + minutosServicio > operario.MinutosMaximosSemanales)
            return $"Se superaría el máximo semanal de {operario.MinutosMaximosSemanales / 60d:0.##} horas.";

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

        for (var offset = 0; offset < 90; offset++)
        {
            var fecha = desde.Date.AddDays(offset);
            if (!TrabajaEseDia(operario, fecha))
                continue;

            var jornadaInicio = fecha.Add(operario.HoraInicioJornada ?? new TimeSpan(8, 0, 0));
            var jornadaFin = fecha.Add(operario.HoraFinJornada ?? new TimeSpan(17, 0, 0));
            var candidato = offset == 0 && desde > jornadaInicio ? RedondearAlCuartoDeHora(desde) : jornadaInicio;

            var ocupados = await _context.Solicitudes.AsNoTracking()
                .Where(s => s.IdSolicitud != excluirSolicitudId && s.IdConductor == idConductor && s.Estado != EstadoAnulado &&
                            s.FechaHoraInicioPlanificada >= fecha && s.FechaHoraInicioPlanificada < fecha.AddDays(1) &&
                            s.FechaHoraFinPlanificada.HasValue)
                .OrderBy(s => s.FechaHoraInicioPlanificada)
                .Select(s => new { Inicio = s.FechaHoraInicioPlanificada!.Value, Fin = s.FechaHoraFinPlanificada!.Value })
                .ToListAsync();

            foreach (var ocupado in ocupados)
            {
                if (candidato.AddMinutes(duracionMinutos) <= ocupado.Inicio)
                    break;
                if (candidato < ocupado.Fin)
                    candidato = RedondearAlCuartoDeHora(ocupado.Fin);
            }

            var fin = candidato.AddMinutes(duracionMinutos);
            if (fin > jornadaFin || EstaInactivo(operario, candidato, fin))
                continue;

            var error = await ValidarDisponibilidadAsync(idConductor, candidato, fin, excluirSolicitudId);
            if (error == null)
                return new PlanificacionHueco { Disponible = true, Inicio = candidato, Fin = fin };
        }

        return new PlanificacionHueco { Mensaje = "No se encontró un hueco disponible en los próximos 90 días." };
    }

    private async Task<int> SumarMinutosAsync(int idConductor, DateTime desde, DateTime hasta, int excluirSolicitudId)
    {
        var servicios = await _context.Solicitudes.AsNoTracking()
            .Where(s => s.IdSolicitud != excluirSolicitudId && s.IdConductor == idConductor && s.Estado != EstadoAnulado &&
                        s.FechaHoraInicioPlanificada >= desde && s.FechaHoraInicioPlanificada < hasta)
            .Select(s => new { s.DuracionPlanificadaMinutos, s.FechaHoraInicioPlanificada, s.FechaHoraFinPlanificada })
            .ToListAsync();

        return servicios.Sum(s => s.DuracionPlanificadaMinutos.GetValueOrDefault() > 0
            ? s.DuracionPlanificadaMinutos!.Value
            : s.FechaHoraInicioPlanificada.HasValue && s.FechaHoraFinPlanificada.HasValue
                ? (int)Math.Ceiling((s.FechaHoraFinPlanificada.Value - s.FechaHoraInicioPlanificada.Value).TotalMinutes)
                : 0);
    }

    private static bool EstaInactivo(Operario operario, DateTime inicio, DateTime fin)
    {
        if (!string.Equals(operario.EstadoLaboral, "Inactivo", StringComparison.OrdinalIgnoreCase))
            return false;

        var desde = operario.InactivoDesde ?? DateTime.MinValue;
        var hasta = operario.InactivoHasta ?? DateTime.MaxValue;
        return inicio < hasta && fin > desde;
    }

    private static bool TrabajaEseDia(Operario operario, DateTime fecha) => fecha.DayOfWeek switch
    {
        DayOfWeek.Saturday => operario.TrabajaSabados,
        DayOfWeek.Sunday => operario.TrabajaDomingos,
        _ => true
    };

    private static string GetMotivo(Operario operario) => string.IsNullOrWhiteSpace(operario.MotivoInactividad)
        ? string.Empty
        : $" por {operario.MotivoInactividad}";

    private static DateTime RedondearAlCuartoDeHora(DateTime value)
    {
        var minutos = (int)Math.Ceiling(value.TimeOfDay.TotalMinutes / 15d) * 15;
        return value.Date.AddMinutes(minutos);
    }
}
