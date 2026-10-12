using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("FacturasCorreoEnvios")]
public sealed class FacturaCorreoEnvio
{
    [Key] public int IdFacturaCorreoEnvio { get; set; }
    [MaxLength(10)] public string Empresa { get; set; } = string.Empty;
    [MaxLength(30)] public string Numero { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; }
    [MaxLength(1000)] public string Destinatarios { get; set; } = string.Empty;
}
