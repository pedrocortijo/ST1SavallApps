using System;
using System.Collections.Generic;
using System.Linq;

namespace ST1Savall.Shared.Data;

public static class SolicitudEstadoEvaluator
{
    public static void EvaluarYAplicarEstado(Solicitud solicitud, Parametro? parametro, HashSet<int>? validEstadoIds = null)
    {
        if (solicitud == null || parametro == null) return;

        // Si la solicitud está en un estado exclusivo o finalizado/iniciado, se mantiene su estado (si es válido)
        if (EsEstadoValido(solicitud.Estado, validEstadoIds) &&
            ((parametro.EstadoFinalizado.HasValue && solicitud.Estado == parametro.EstadoFinalizado.Value) ||
             (parametro.EstadoIniciado.HasValue && solicitud.Estado == parametro.EstadoIniciado.Value) ||
             solicitud.Estado == 3 || solicitud.Estado == 4 || solicitud.Estado == 5))
        {
            return;
        }

        bool tieneConductor = solicitud.IdConductor.HasValue && solicitud.IdConductor.Value > 0;
        bool tieneFechaPrevista = solicitud.FechaPrevista.HasValue || solicitud.FechaTarea.HasValue;
        bool tieneHoraInicio = solicitud.FechaHoraInicioPlanificada.HasValue;
        bool tieneHoraFin = solicitud.FechaHoraFinPlanificada.HasValue;
        bool tieneTiempo = solicitud.DuracionPlanificadaMinutos.HasValue && solicitud.DuracionPlanificadaMinutos.Value > 0;
        bool tieneMotivoAplazamiento = solicitud.MotivoReprogramacion.HasValue && solicitud.MotivoReprogramacion.Value > 0;

        int? nuevoEstado = null;

        // Rule 3: Si hay fecha Prevista y Motivo del aplazamiento el estado pasa a Reprogramado
        if (tieneFechaPrevista && tieneMotivoAplazamiento)
        {
            if (parametro.EstadoReprogramacion.HasValue && EsEstadoValido(parametro.EstadoReprogramacion.Value, validEstadoIds))
            {
                nuevoEstado = parametro.EstadoReprogramacion.Value;
            }
        }
        // Rule 1: Si se le ha asignado un conductor/operario, tiene fecha Prevista, Hora Inicio, Hora Fin y tiempo el estado pasa a Adjudicado/Asignado
        else if (tieneConductor && tieneFechaPrevista && tieneHoraInicio && tieneHoraFin && tieneTiempo)
        {
            if (parametro.EstadoAdjudicado.HasValue && EsEstadoValido(parametro.EstadoAdjudicado.Value, validEstadoIds))
            {
                nuevoEstado = parametro.EstadoAdjudicado.Value;
            }
            else if (EsEstadoValido(2, validEstadoIds))
            {
                nuevoEstado = 2;
            }
        }
        // Rule 2: Si falta alguno de los valores anteriores y no hay Motivo del aplazamiento, el estado pasa a Pendiente
        else if (!tieneMotivoAplazamiento)
        {
            if (parametro.EstadoPendiente.HasValue && EsEstadoValido(parametro.EstadoPendiente.Value, validEstadoIds))
            {
                nuevoEstado = parametro.EstadoPendiente.Value;
            }
            else if (EsEstadoValido(1, validEstadoIds))
            {
                nuevoEstado = 1;
            }
        }

        if (nuevoEstado.HasValue)
        {
            solicitud.Estado = nuevoEstado.Value;
        }

        // Si por alguna razón el estado asignado no existe en EstadosSolicitud, corregir al primer estado válido
        if (validEstadoIds != null && validEstadoIds.Any() && !validEstadoIds.Contains(solicitud.Estado))
        {
            solicitud.Estado = validEstadoIds.First();
        }
    }

    private static bool EsEstadoValido(int estadoId, HashSet<int>? validEstadoIds)
    {
        if (estadoId <= 0) return false;
        if (validEstadoIds == null) return true;
        return validEstadoIds.Contains(estadoId);
    }
}
