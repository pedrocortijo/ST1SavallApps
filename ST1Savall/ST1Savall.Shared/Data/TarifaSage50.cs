using System.ComponentModel.DataAnnotations;

namespace ST1Savall.Shared.Data;

public class TarifaSage50
{
    [Key, MaxLength(2)] public string Codigo { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string Nombre { get; set; } = string.Empty;
}
