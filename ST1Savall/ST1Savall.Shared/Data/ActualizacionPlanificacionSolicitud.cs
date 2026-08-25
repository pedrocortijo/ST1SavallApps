namespace ST1Savall.Shared.Data;

/// <summary>
/// Datos que se modifican al programar varios servicios en una única operación.
/// </summary>
public class ActualizacionPlanificacionSolicitud
{
    public int IdSolicitud { get; set; }
    public int? IdConductor { get; set; }
    public DateTime? FechaTarea { get; set; }
    public DateTime? FechaPrevista { get; set; }
    public DateTime? FechaHoraInicioPlanificada { get; set; }
    public DateTime? FechaHoraFinPlanificada { get; set; }
    public int? DuracionPlanificadaMinutos { get; set; }
    public int Estado { get; set; }
}
