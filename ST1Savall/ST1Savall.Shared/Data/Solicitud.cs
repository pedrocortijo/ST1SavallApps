using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("Solicitudes")]
public class Solicitud
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdSolicitud { get; set; }

    public int? IdConductor { get; set; }
    public int IdTipoTarea { get; set; }

    public DateTime? FechaSolicitud { get; set; }
    public DateTime? FechaTarea { get; set; }
    public DateTime? FechaPrevista { get; set; }

    public int? IdHoraDisponible { get; set; }
    public int IdUsuario { get; set; }
    public int IdCliente { get; set; }
    public int? Prioridad { get; set; }

    [MaxLength(20)]
    public string? CodigoEntrega { get; set; }

    [MaxLength(20)]
    public string? CodigoRecogida { get; set; }

    [MaxLength(20)]
    public string? CodigoAmbosEntrega { get; set; }

    [MaxLength(20)]
    public string? CodigoAmbosRecogida { get; set; }

    public int Estado { get; set; }

    [ForeignKey("Estado")]
    public EstadoSolicitud? EstadoSolicitud { get; set; }

    public DateTime? HoraLlegada { get; set; }

    public string? Observaciones { get; set; }

    [MaxLength(15)]
    public string? IdOperario { get; set; }

    public int? MotivoReprogramacion { get; set; }

    [MaxLength(50)]
    public string? CoordenadasGPS { get; set; }

    [MaxLength(200)]
    public string? NombreCliente { get; set; }

    [MaxLength(200)]
    public string? NombreObra { get; set; }

    [MaxLength(200)]
    public string? DireccionCliente { get; set; }

    [MaxLength(100)]
    public string? PoblacionCliente { get; set; }

    [MaxLength(20)]
    public string? TelefonoCliente { get; set; }

    public DateTime? FechaInicial { get; set; }

    public bool RequiereOVP { get; set; } = false;

    [MaxLength(50)]
    public string? NumLicenciaOVP { get; set; }

    public DateTime? FechaInicioOVP { get; set; }
    public DateTime? FechaFinOVP { get; set; }

    [NotMapped]
    public string? ConductorNombre { get; set; }

    [NotMapped]
    public string? EstadoDescripcion { get; set; }

    [NotMapped]
    public string? TipoTareaDescripcion { get; set; }

    [NotMapped]
    public string? PrioridadDescripcion { get; set; }

    [NotMapped]
    public string? ObraDescripcion { get; set; }

    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public DateTime? FechaActualizacion { get; set; }

    public DateTime? FechaHoraInicioPlanificada { get; set; }
    public DateTime? FechaHoraFinPlanificada { get; set; }
    public int? DuracionPlanificadaMinutos { get; set; }
    public int? DuracionViajeMinutos { get; set; }
    public int? DuracionOperacionMinutos { get; set; }

    public int? IdPlantaOrigen { get; set; }
    public int? IdPlantaDescarga { get; set; }
    public int? IdPlantaRegreso { get; set; }

    public int? DistanciaOrigenObraMetros { get; set; }
    public int? DistanciaObraDescargaMetros { get; set; }
    public int? DistanciaDescargaRegresoMetros { get; set; }
    public int? MinutosOrigenObra { get; set; }
    public int? MinutosObraDescarga { get; set; }
    public int? MinutosDescargaRegreso { get; set; }
    public int? DistanciaTotalMetros { get; set; }

    public decimal? LatitudOrigen { get; set; }
    public decimal? LongitudOrigen { get; set; }
    public decimal? LatitudObra { get; set; }
    public decimal? LongitudObra { get; set; }
    public decimal? LatitudDescarga { get; set; }
    public decimal? LongitudDescarga { get; set; }
    public decimal? LatitudRegreso { get; set; }
    public decimal? LongitudRegreso { get; set; }

    public bool DuracionModificadaManualmente { get; set; }
    public DateTime? FechaCalculoRuta { get; set; }

    [MaxLength(30)]
    public string? ProveedorCalculoRuta { get; set; }

    [MaxLength(100)]
    public string? Encargado { get; set; }

    [MaxLength(20)]
    public string? Movil { get; set; }
}
