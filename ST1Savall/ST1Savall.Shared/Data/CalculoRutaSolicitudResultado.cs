namespace ST1Savall.Shared.Data;

public class CalculoRutaSolicitudResultado
{
    public bool Calculado { get; set; }
    public string? Mensaje { get; set; }
    public int TramosDesdeCache { get; set; }
    public int TramosDesdeGoogle { get; set; }
    public Solicitud? Solicitud { get; set; }
}
