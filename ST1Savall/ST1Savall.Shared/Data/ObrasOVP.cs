using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("ObrasOVP")]
public class ObrasOVP
{
    [Key]
    public int IdObrasOVP { get; set; }

    [Required, MaxLength(5)]
    [Column(TypeName = "char(5)")]
    public string IdObra { get; set; } = string.Empty;

    public bool RequiereOVP { get; set; }

    [MaxLength(50)]
    public string? NumLicenciaOVP { get; set; }

    [Column(TypeName = "datetime2(7)")]
    public DateTime? FechaInicioOVP { get; set; }

    [Column(TypeName = "datetime2(7)")]
    public DateTime? FechaFinOVP { get; set; }

    [MaxLength(255)]
    [Column(TypeName = "varchar(255)")]
    public string? PathOVP { get; set; }
}
