using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

/// <summary>Apunte del fondo entregado por el cliente para una obra.</summary>
[Table("MovimientosEntregaObra")]
public class MovimientoEntregaObra
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdMovimientoEntregaObra { get; set; }

    [Required, MaxLength(5), Column(TypeName = "char(5)")]
    public string ObraCodigo { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }
    [Required, MaxLength(20)] public string Tipo { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")] public decimal Debe { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Haber { get; set; }
    [Required, MaxLength(250)] public string Concepto { get; set; } = string.Empty;
    [MaxLength(2)] public string? EmpresaAlbaran { get; set; }
    [MaxLength(12)] public string? NumeroAlbaran { get; set; }
    [MaxLength(2)] public string? SerieAlbaran { get; set; }
    public bool Activo { get; set; } = true;
    public bool Anulado { get; set; }
    public DateTime CreadoEn { get; set; }
    [MaxLength(256)] public string? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    [MaxLength(256)] public string? ModificadoPor { get; set; }
}

public sealed class ResumenEntregasObra
{
    public List<MovimientoEntregaObra> Movimientos { get; set; } = [];
    public decimal TotalEntregado { get; set; }
    public decimal TotalConsumido { get; set; }
    public decimal Saldo { get; set; }
}

public sealed class NuevaEntregaObra
{
    public DateTime Fecha { get; set; } = DateTime.Today;
    [Range(typeof(decimal), "0.00", "999999999", ParseLimitsInInvariantCulture = true)]
    public decimal Importe { get; set; }
    [Required, MaxLength(250)] public string Concepto { get; set; } = string.Empty;
}

public sealed class EstadoMovimientoEntregaObra
{
    public bool Activo { get; set; }
}
