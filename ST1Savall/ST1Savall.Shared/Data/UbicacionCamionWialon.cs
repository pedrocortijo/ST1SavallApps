namespace ST1Savall.Shared.Data;

public sealed class UbicacionCamionWialon
{
    public int IdCamion { get; set; }
    public string Matricula { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? IdServicio { get; set; }
    public string? NombreObra { get; set; }
    public string? NombreConductor { get; set; }
    public string UnidadWialonId { get; set; } = string.Empty;
    public string? NombreUnidad { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public double? VelocidadKmH { get; set; }
    public string EstadoUnidad { get; set; } = string.Empty;
    public string? FaseRuta { get; set; }
    public int? Rumbo { get; set; }
    public DateTime? FechaPosicionUtc { get; set; }
    public DateTime? FechaPosicionLocal { get; set; }
    public string? DestinoRestante { get; set; }
    public int? DistanciaRutaMetros { get; set; }
    public int? MinutosViajeRuta { get; set; }
    public int? DistanciaRestanteMetros { get; set; }
    public int? MinutosViajeRestantes { get; set; }
    public int? MinutosOperacionRestantes { get; set; }
    public int? MinutosTotalRestantes { get; set; }
    public string? ErrorRutaRestante { get; set; }
    public string? Error { get; set; }
}