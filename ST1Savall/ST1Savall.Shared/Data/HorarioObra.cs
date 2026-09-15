using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ST1Savall.Shared.Data;
[Table("HorariosObra")]
public class HorarioObra {
    [Key, Column("CODIGO", TypeName = "char(5)"), StringLength(5)]
    public string Codigo { get; set; } = string.Empty;
    [Column("HORAINICIO", TypeName = "time")]
    public TimeSpan HoraInicio { get; set; }
    [Column("HORAFIN", TypeName = "time")]
    public TimeSpan HoraFin { get; set; }
}