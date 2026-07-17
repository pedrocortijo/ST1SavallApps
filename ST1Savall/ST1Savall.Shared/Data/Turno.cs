using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ST1Savall.Shared.Data;
[Table("Turnos")]
public class Turno {
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)] public int IdTurno { get; set; }
    [Required, MaxLength(80)] public string NombreTurno { get; set; } = string.Empty;
    public TimeSpan HoraEntrada { get; set; } = new(8, 0, 0);
    public TimeSpan HoraSalida { get; set; } = new(17, 0, 0);
    public int TiempoAlmuerzoMinutos { get; set; }
    public int ToleranciaEntradaMinutos { get; set; }
}
