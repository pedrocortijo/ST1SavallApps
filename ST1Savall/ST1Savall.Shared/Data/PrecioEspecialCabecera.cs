using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("PreciosEspecialesCabecera")]
public class PrecioEspecialCabecera
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdPrecioEspecialCabecera { get; set; }
    [Required, MaxLength(5), Column(TypeName = "char(5)")] public string ObraSage { get; set; } = string.Empty;
    public DateTime? VigenteDesde { get; set; }
    public DateTime? VigenteHasta { get; set; }
    [MaxLength(250)] public string? Observaciones { get; set; }
    public List<PrecioEspecialDetalle> Detalles { get; set; } = [];
}
