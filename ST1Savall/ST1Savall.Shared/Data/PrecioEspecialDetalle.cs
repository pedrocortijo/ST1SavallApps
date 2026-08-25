using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("PreciosEspecialesDetalles")]
public class PrecioEspecialDetalle
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdPrecioEspecialDetalle { get; set; }
    public int IdPrecioEspecialCabecera { get; set; }
    [Required, MaxLength(20), Column(TypeName = "char(20)")] public string ArticuloSage { get; set; } = string.Empty;
    [Range(0, 999999999), Column(TypeName = "decimal(15,6)")] public decimal Precio { get; set; }
}
