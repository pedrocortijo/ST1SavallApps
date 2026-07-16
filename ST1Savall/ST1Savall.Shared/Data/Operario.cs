using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("Operarios")]
public class Operario
{
    [Key]
    public int IdOperario { get; set; }

    [MaxLength(80)]
    public string? Nombre { get; set; }

    [MaxLength(30)]
    public string? Telefono { get; set; }

    public int? IdCargo { get; set; }

    [ForeignKey("IdCargo")]
    public Cargo? Cargo { get; set; }

    public int? IdPlanta { get; set; }

    [ForeignKey("IdPlanta")]
    public Planta? Planta { get; set; }

    public bool? Activo { get; set; }
    public bool? Obras { get; set; }
    public bool? Mensajes { get; set; }
    public bool? Tierras { get; set; }

    [MaxLength(20)]
    public string EstadoLaboral { get; set; } = "Activo";

    [MaxLength(30)]
    public string? MotivoInactividad { get; set; }

    public DateTime? InactivoDesde { get; set; }
    public DateTime? InactivoHasta { get; set; }

    public TimeSpan? HoraInicioJornada { get; set; } = new(8, 0, 0);
    public TimeSpan? HoraFinJornada { get; set; } = new(17, 0, 0);
    public int MinutosMaximosDiarios { get; set; } = 480;
    public int MinutosMaximosSemanales { get; set; } = 2400;
    public bool TrabajaSabados { get; set; }
    public bool TrabajaDomingos { get; set; }
}
