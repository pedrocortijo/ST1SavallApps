using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

public enum UnidadPeriodicidad { Dias = 1, Semanas = 2, Meses = 3 }
public enum InicioPeriodicidad { PrimerServicioRealizado = 1, FechaManual = 2 }
public enum EstadoEjecucionPeriodicidad { Pendiente = 1, Generada = 2, Realizada = 3, Cancelada = 4 }

[Table("PeriodicidadesObra")]
public class PeriodicidadObra
{
    [Key] public int IdPeriodicidadObra { get; set; }
    [Required, MaxLength(5)] public string ObraCodigo { get; set; } = string.Empty;
    public bool Activa { get; set; }
    public bool PendienteConfirmacion { get; set; }
    public UnidadPeriodicidad Unidad { get; set; } = UnidadPeriodicidad.Semanas;
    [Range(1, 999)] public int Cantidad { get; set; } = 1;
    public int L { get; set; }
    public int M { get; set; }
    public int X { get; set; }
    public int J { get; set; }
    public int V { get; set; }
    public int S { get; set; }
    public int D { get; set; }
    public TimeSpan HoraPrevista { get; set; } = new(8, 0, 0);
    public InicioPeriodicidad TipoInicio { get; set; } = InicioPeriodicidad.PrimerServicioRealizado;
    public DateTime? FechaInicioManual { get; set; }
    public DateTime? FechaUltimaRealizada { get; set; }
    public DateTime? ProximaFecha { get; set; }
    public int IdTipoTarea { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
    public List<PeriodicidadObraEjecucion> Ejecuciones { get; set; } = [];
}

[Table("PeriodicidadesObraEjecuciones")]
public class PeriodicidadObraEjecucion
{
    [Key] public int IdPeriodicidadObraEjecucion { get; set; }
    public int IdPeriodicidadObra { get; set; }
    public DateTime FechaPrevista { get; set; }
    public TimeSpan HoraPrevista { get; set; }
    public int? IdSolicitud { get; set; }
    public EstadoEjecucionPeriodicidad Estado { get; set; } = EstadoEjecucionPeriodicidad.Pendiente;
    public DateTime? FechaRealizacion { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(IdPeriodicidadObra))] public PeriodicidadObra? Periodicidad { get; set; }
}