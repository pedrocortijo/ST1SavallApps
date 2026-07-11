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
}
