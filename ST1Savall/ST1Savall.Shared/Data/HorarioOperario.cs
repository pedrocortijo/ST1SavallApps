using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ST1Savall.Shared.Data;
[Table("HorariosOperarios")]
public class HorarioOperario {
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)] public int IdAsignacion { get; set; }
    public int IdOperario { get; set; }
    [ForeignKey(nameof(IdOperario))] public Operario? Operario { get; set; }
    public int IdTurno { get; set; }
    [ForeignKey(nameof(IdTurno))] public Turno? Turno { get; set; }
    [Range(1, 7)] public int DiaSemana { get; set; }
    public DateOnly FechaInicioVigencia { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? FechaFinVigencia { get; set; }
}
