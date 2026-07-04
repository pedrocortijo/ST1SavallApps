using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("ContenedoresTipos")]
public class ContenedorTipo
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdTipo { get; set; }

    [Required]
    [MaxLength(50)]
    public string Descripcion { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? CapacidadMetrosCubicos { get; set; }

    public int? LargoCm { get; set; }
    public int? AnchoCm { get; set; }
    public int? AltoCm { get; set; }
}
