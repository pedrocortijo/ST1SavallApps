using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("RutasCache")]
public class RutaCache
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long IdRutaCache { get; set; }

    [MaxLength(64)]
    public string ClaveRuta { get; set; } = string.Empty;

    [Column(TypeName = "decimal(9, 6)")]
    public decimal LatitudOrigen { get; set; }

    [Column(TypeName = "decimal(9, 6)")]
    public decimal LongitudOrigen { get; set; }

    [Column(TypeName = "decimal(9, 6)")]
    public decimal LatitudDestino { get; set; }

    [Column(TypeName = "decimal(9, 6)")]
    public decimal LongitudDestino { get; set; }

    [MaxLength(20)]
    public string ModoViaje { get; set; } = "DRIVE";

    [MaxLength(30)]
    public string PreferenciaRuta { get; set; } = "TRAFFIC_UNAWARE";

    public int DistanciaMetros { get; set; }
    public int DuracionSegundos { get; set; }
    public DateTime FechaCalculoUtc { get; set; }
    public DateTime FechaExpiracionUtc { get; set; }
    public DateTime UltimoUsoUtc { get; set; }
    public int NumeroUsos { get; set; }
}
