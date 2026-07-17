using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ST1Savall.Shared.Data;
[Table("Turnos")]
public class Turno {
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)] public int IdTurno { get; set; }
    [Required, MaxLength(80)] public string NombreTurno { get; set; } = string.Empty;
    public TimeSpan HoraEntrada { get; set; } = new(8, 0, 0);
    public TimeSpan HoraSalida { get; set; } = new(17, 0, 0);
    public TimeSpan? HoraInicioBreak { get; set; }
    public TimeSpan? HoraFinBreak { get; set; }
    public int TiempoAlmuerzoMinutos { get; set; }
    public int ToleranciaEntradaMinutos { get; set; }

    [NotMapped]
    public int TotalMinutos
    {
        get
        {
            var jornada = HoraSalida - HoraEntrada;
            if (jornada < TimeSpan.Zero) jornada += TimeSpan.FromDays(1);

            if (HoraInicioBreak is not { } inicioBreak || HoraFinBreak is not { } finBreak)
                return Math.Max(0, (int)jornada.TotalMinutes);

            var descanso = finBreak - inicioBreak;
            if (descanso < TimeSpan.Zero) descanso += TimeSpan.FromDays(1);
            return Math.Max(0, (int)(jornada - descanso).TotalMinutes);
        }
    }

    [NotMapped]
    public decimal TotalHoras => Math.Round(TotalMinutos / 60m, 2);
}
