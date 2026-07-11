using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("Plantas")]
public class Planta
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdPlanta { get; set; }

    [MaxLength(100)]
    public string? Nombre { get; set; }

    [MaxLength(100)]
    public string? Direccion { get; set; }

    [MaxLength(100)]
    public string? Poblacion { get; set; }

    [MaxLength(5)]
    public string? CodigoPostal { get; set; }

    [MaxLength(15)]
    public string? Nima { get; set; }

    [MaxLength(30)]
    public string? Autorizacion { get; set; }

    [Column(TypeName = "decimal(9, 6)")]
    public decimal? Latitud { get; set; }

    [Column(TypeName = "decimal(9, 6)")]
    public decimal? Longitud { get; set; }
}
