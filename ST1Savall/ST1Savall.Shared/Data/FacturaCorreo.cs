namespace ST1Savall.Shared.Data;

public sealed class FacturaCorreo
{
    public string Empresa { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public List<AlbaranCorreo> Albaranes { get; set; } = [];
    public ClaveFacturaCorreo ObtenerClave() => new(Empresa, Serie, Numero);
}

public sealed record ClaveFacturaCorreo(string Empresa, string Serie, string Numero);
public sealed record ResultadoFacturaCorreo(string Factura, string Destinatario, bool Enviado, string Detalle);
