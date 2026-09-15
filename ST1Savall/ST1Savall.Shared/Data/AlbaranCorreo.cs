namespace ST1Savall.Shared.Data;

public record ClaveAlbaranCorreo(string Empresa, string Numero, string Serie);
public sealed class AlbaranCorreo
{
    public string Empresa { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Serie { get; set; } = "";
    public string Clave => $"{Empresa}|{Serie}|{Numero}";
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = "";
    public string Email { get; set; } = "";
    public string Obra { get; set; } = "";
    public decimal Total { get; set; }
    public ClaveAlbaranCorreo ObtenerClave() => new(Empresa, Numero, Serie);
}
public sealed record ResultadoAlbaranCorreo(string Albaran, string Destinatario, bool Enviado, string Detalle);
