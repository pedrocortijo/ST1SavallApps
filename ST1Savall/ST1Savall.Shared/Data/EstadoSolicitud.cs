using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("EstadosSolicitud")]
public class EstadoSolicitud
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

    public int IdEstado { get; set; }

    [MaxLength(100)]
    public string? Descripcion { get; set; }

    [MaxLength(20)]
    public string? BgColor { get; set; }

    [MaxLength(20)]
    public string? TextColor { get; set; }
}
