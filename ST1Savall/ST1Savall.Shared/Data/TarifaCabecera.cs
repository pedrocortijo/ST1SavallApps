using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ST1Savall.Shared.Data;
[Table("TarifasCab")]
public class TarifaCabecera
{
    [Key, Required, MaxLength(2), Column(TypeName = "char(2)")] public string Codigo { get; set; } = string.Empty;
    [Required, MaxLength(30), Column(TypeName = "char(30)")] public string Nombre { get; set; } = string.Empty;
    [Column(TypeName = "date")] public DateTime Desde { get; set; } = DateTime.Today;
    [Column(TypeName = "date")] public DateTime Hasta { get; set; } = DateTime.Today;
    [Required, MaxLength(4), Column(TypeName = "char(4)")] public string Zona { get; set; } = string.Empty;
    public List<TarifaLinea> Lineas { get; set; } = [];
}