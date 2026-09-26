namespace ST1Savall.Shared.Data;

public sealed class AnalizarTiquePesajeRequest
{
    public string? NombreArchivo { get; init; }
    public string? ContentType { get; init; }
    public string? ImagenBase64 { get; init; }
    public string? TextoOcr { get; init; }
    public decimal? ConfianzaOcr { get; init; }
}

public sealed class TiquePesajeAnalisisResponse
{
    public int IdTiquePesaje { get; set; }
    public string? NumeroAlbaran { get; set; }
    public string? Matricula { get; set; }
    public DateTime? Fecha { get; set; }
    public TimeSpan? Hora { get; set; }
    public int? PesoNetoKg { get; set; }
    public decimal? Confianza { get; set; }
    public string? TextoOcr { get; set; }
    public List<string> Advertencias { get; set; } = [];
}

public sealed class ConfirmarTiquePesajeRequest
{
    public string? NumeroAlbaran { get; init; }
    public string? Matricula { get; init; }
    public DateTime? Fecha { get; init; }
    public TimeSpan? Hora { get; init; }
    public int? PesoNetoKg { get; init; }
}
