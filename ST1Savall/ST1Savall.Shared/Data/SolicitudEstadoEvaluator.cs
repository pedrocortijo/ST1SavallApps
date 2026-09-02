using System;
using System.Collections.Generic;
using System.Linq;

namespace ST1Savall.Shared.Data;

public static class SolicitudEstadoEvaluator
{
    public static void EvaluarYAplicarEstado(
        Solicitud solicitud,
        Parametro? parametro,
        HashSet<int>? validEstadoIds = null,
        IEnumerable<EstadoSolicitud>? estados = null)
    {
        if (solicitud == null) return;

        var listaEstados = estados?.ToList();

        int idAdjudicado = ObtenerEstadoId(
            parametro?.EstadoAdjudicado,
            listaEstados,
            new[] { "asignad", "adjudicad" },
            new[] { 9, 2 },
            validEstadoIds);

        int idPendiente = ObtenerEstadoId(
            parametro?.EstadoPendiente,
            listaEstados,
            new[] { "pendiente", "introducido" },
            new[] { 1 },
            validEstadoIds);

        int idReprogramacion = ObtenerEstadoId(
            parametro?.EstadoReprogramacion,
            listaEstados,
            new[] { "reprogram", "anulad" },
            new[] { 6 },
            validEstadoIds);

        int? idFinalizado = parametro?.EstadoFinalizado.HasValue == true && EsEstadoValido(parametro.EstadoFinalizado.Value, validEstadoIds)
            ? parametro.EstadoFinalizado.Value
            : listaEstados?.FirstOrDefault(e => e.Descripcion.Contains("finalizado", StringComparison.OrdinalIgnoreCase))?.IdEstado;

        int? idIniciado = parametro?.EstadoIniciado.HasValue == true && EsEstadoValido(parametro.EstadoIniciado.Value, validEstadoIds)
            ? parametro.EstadoIniciado.Value
            : listaEstados?.FirstOrDefault(e => e.Descripcion.Contains("iniciado", StringComparison.OrdinalIgnoreCase))?.IdEstado;

        bool tieneFechaAnulacion = solicitud.FechaAnulacion.HasValue;
        bool tieneMotivoAplazamiento = solicitud.MotivoReprogramacion.HasValue && solicitud.MotivoReprogramacion.Value > 0;
        bool tieneFechaPrevista = solicitud.FechaPrevista.HasValue;

        // Si se han introducido datos de anulación (Fecha de Anulación, o Motivo de reprogramación/anulación),
        // el estado pasa a Anulado / Reprogramado.
        if (tieneFechaAnulacion || (tieneFechaPrevista && tieneMotivoAplazamiento) || tieneMotivoAplazamiento)
        {
            solicitud.Estado = idReprogramacion;
            return;
        }

        // Si la solicitud está en un estado finalizado o iniciado (y no es anulación), se mantiene su estado
        if (EsEstadoValido(solicitud.Estado, validEstadoIds) &&
            ((idFinalizado.HasValue && solicitud.Estado == idFinalizado.Value) ||
             (idIniciado.HasValue && solicitud.Estado == idIniciado.Value) ||
             (listaEstados != null && listaEstados.Any(e => e.IdEstado == solicitud.Estado && 
                (e.Descripcion.Contains("finalizado", StringComparison.OrdinalIgnoreCase) || 
                 e.Descripcion.Contains("iniciado", StringComparison.OrdinalIgnoreCase))))))
        {
            return;
        }

        bool tieneConductor = solicitud.IdConductor.HasValue && solicitud.IdConductor.Value >= 0;
        bool tieneFecha = solicitud.FechaPrevista.HasValue || solicitud.FechaTarea.HasValue;
        bool tieneHoraInicio = solicitud.FechaHoraInicioPlanificada.HasValue;
        bool tieneHoraFin = solicitud.FechaHoraFinPlanificada.HasValue;
        bool tieneTiempo = solicitud.DuracionPlanificadaMinutos.HasValue && solicitud.DuracionPlanificadaMinutos.Value > 0;

        // Regla principal: Si tiene Conductor + Fecha + Hora Inicio + Hora Fin + Duración -> Asignado / Adjudicado
        if (tieneConductor && tieneFecha && tieneHoraInicio && tieneHoraFin && tieneTiempo)
        {
            solicitud.Estado = idAdjudicado;
        }
        else
        {
            solicitud.Estado = idPendiente;
        }

        // Si por alguna razón el estado asignado no existe en EstadosSolicitud, corregir al primer estado válido
        if (validEstadoIds != null && validEstadoIds.Count > 0 && !validEstadoIds.Contains(solicitud.Estado))
        {
            solicitud.Estado = validEstadoIds.First();
        }
    }

    private static int ObtenerEstadoId(
        int? estadoConfigurado,
        List<EstadoSolicitud>? estados,
        string[] palabrasClave,
        int[] fallbacks,
        HashSet<int>? validEstadoIds)
    {
        if (estadoConfigurado.HasValue && EsEstadoValido(estadoConfigurado.Value, validEstadoIds))
            return estadoConfigurado.Value;

        if (estados != null)
        {
            foreach (var palabra in palabrasClave)
            {
                var encontrado = estados.FirstOrDefault(e => e.Descripcion.Contains(palabra, StringComparison.OrdinalIgnoreCase));
                if (encontrado != null && EsEstadoValido(encontrado.IdEstado, validEstadoIds))
                    return encontrado.IdEstado;
            }
        }

        foreach (var fallback in fallbacks)
        {
            if (EsEstadoValido(fallback, validEstadoIds))
                return fallback;
        }

        if (validEstadoIds != null && validEstadoIds.Count > 0)
            return validEstadoIds.First();

        return fallbacks.FirstOrDefault();
    }

    private static bool EsEstadoValido(int estadoId, HashSet<int>? validEstadoIds)
    {
        if (estadoId <= 0) return false;
        if (validEstadoIds == null) return true;
        return validEstadoIds.Contains(estadoId);
    }
}
