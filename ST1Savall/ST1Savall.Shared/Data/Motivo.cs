using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("Motivos")]
public class Motivo
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdMotivo { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("Motivo")]
    public string DescripcionMotivo { get; set; } = string.Empty;
}
