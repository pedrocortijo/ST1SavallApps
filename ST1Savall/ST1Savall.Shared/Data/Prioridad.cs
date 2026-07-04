using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("Prioridades")]
public class Prioridad
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdPrioridad { get; set; }

    public int? Codigo { get; set; }

    [MaxLength(100)]
    public string? Descripcion { get; set; }

    [MaxLength(20)]
    public string? BgColor { get; set; }

    [MaxLength(20)]
    public string? TextColor { get; set; }
}