using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ST1Savall.Shared.Data;
[Table("TarifasLin")]
public class TarifaLinea
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)] public int Codigo { get; set; }
    [NotMapped, System.Text.Json.Serialization.JsonIgnore] public Guid EditorId { get; set; } = Guid.NewGuid();
    [MaxLength(2), Column(TypeName = "char(2)")] public string Tarifa { get; set; } = string.Empty;
    [Required, MaxLength(8), Column(TypeName = "char(8)")] public string Articulo { get; set; } = string.Empty;
    [Range(0, 999999999), Column(TypeName = "decimal(18,2)")] public decimal Precio { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] public TarifaCabecera? Cabecera { get; set; }
}