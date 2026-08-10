using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("Camiones")]
public class Camion
{
    [Key]
    public int IdCamion { get; set; }

    [Required, MaxLength(20)]
    public string Matricula { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Descripcion { get; set; }

    [MaxLength(50)]
    public string? UnidadWialonId { get; set; }

    public int? IdConductor { get; set; }

    [ForeignKey(nameof(IdConductor))]
    public Operario? Conductor { get; set; }

    public bool Activo { get; set; } = true;
}
