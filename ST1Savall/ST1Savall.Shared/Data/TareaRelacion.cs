using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("TareasRelaciones")]
public class TareaRelacion
{
    [Key, Column(Order = 0)]
    public int IdTareaOrigen { get; set; }

    [Key, Column(Order = 1)]
    public int IdTareaDestino { get; set; }

    [ForeignKey(nameof(IdTareaOrigen))]
    public Tarea? TareaOrigen { get; set; }

    [ForeignKey(nameof(IdTareaDestino))]
    public Tarea? TareaDestino { get; set; }

    [NotMapped]
    public string CompositeKey => $"{IdTareaOrigen}_{IdTareaDestino}";
}
