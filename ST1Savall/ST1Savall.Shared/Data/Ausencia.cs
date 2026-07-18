using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("Ausencias")]
public class Ausencia
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdAusencia { get; set; }

    public int IdConductor { get; set; }

    [ForeignKey(nameof(IdConductor))]
    public Operario? Conductor { get; set; }

    public DateOnly FechaInicio { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly FechaFin { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required, MaxLength(150)]
    public string Tipo { get; set; } = string.Empty;
}
