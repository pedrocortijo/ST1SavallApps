namespace ST1Savall.Shared.Data;

/// <summary>Cabecera de factura de venta de Sage 50 (c_factuven).</summary>
public sealed class FacturaVentaSage50
{
    public string EMPRESA { get; set; } = string.Empty;
    public string NUMERO { get; set; } = string.Empty;
    public string LETRA { get; set; } = string.Empty;
    public DateTime FECHA { get; set; }
    public string CLIENTE { get; set; } = string.Empty;
    public string CLIENTEERP { get; set; } = string.Empty;
    public decimal TOTALDOC { get; set; }
    public DateTime MODIFIED { get; set; }
}
