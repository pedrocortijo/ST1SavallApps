using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("PreciosEspecialesObra")]
public class PrecioEspecialObra
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required, MaxLength(8), Column(TypeName = "char(8)")]
    public string ClienteSage { get; set; } = string.Empty;

    [Required, MaxLength(5), Column(TypeName = "char(5)")]
    public string ObraSage { get; set; } = string.Empty;

    [Required, MaxLength(20), Column(TypeName = "char(20)")]
    public string ArticuloSage { get; set; } = string.Empty;

    [Range(0, 999999999)]
    [Column(TypeName = "decimal(15,6)")]
    public decimal Precio { get; set; }

    public DateTime? VigenteDesde { get; set; }
    public DateTime? VigenteHasta { get; set; }
    [MaxLength(250)] public string? Observaciones { get; set; }
}
