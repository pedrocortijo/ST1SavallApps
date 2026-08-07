using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("articulo")]
public class ArticuloSage50
{
    [Key]
    [Column("CODIGO")]
    [StringLength(20)]
    public string Codigo { get; set; } = string.Empty;

    [Column("NOMBRE")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Column("FAMILIA")]
    [StringLength(5)]
    public string Familia { get; set; } = string.Empty;

    [NotMapped]
    public string Descripcion => $"{Codigo.Trim()} - {Nombre.Trim()}";
}
