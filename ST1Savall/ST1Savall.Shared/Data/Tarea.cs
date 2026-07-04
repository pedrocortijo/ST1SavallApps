using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("Tareas")]
public class Tarea
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int IdTarea { get; set; }

    [Column("Tarea")]
    [Required]
    [MaxLength(150)]
    public string NombreTarea { get; set; } = string.Empty;
}
