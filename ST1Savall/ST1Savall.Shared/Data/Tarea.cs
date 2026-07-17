using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("Tareas")]
public class Tarea
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdTarea { get; set; }

    [Column("Tarea")]
    [Required]
    [MaxLength(150)]
    public string NombreTarea { get; set; } = string.Empty;

    public bool Recoger1 { get; set; } = false;
    public bool Recoger2 { get; set; } = false;
    public bool Entrega1 { get; set; } = false;
    public bool Entrega2 { get; set; } = false;
}
