using System;

namespace ST1Savall.Shared.Data;

public class DeCaDetalleDto
{
    public int IdSolicitud { get; set; }
    public string? GuidDeCa { get; set; }
    public DateTime? FechaHoraEmision { get; set; }
    public string? Tipo { get; set; } // "ENTREGA" | "RETIRADA"
    public string? UrlDescarga { get; set; }
    public bool TienePdf { get; set; }

    // Datos del cargador
    public string? CargadorNombre { get; set; }
    public string? CargadorCif { get; set; }
    public string? ObraNombre { get; set; }
    public string? ObraDireccion { get; set; }

    // Datos del transportista
    public string? TransportistaNombre { get; set; }
    public string? TransportistaCif { get; set; }
    public string? AutorizacionTransporte { get; set; }

    // Vehículo y conductor
    public string? Matricula { get; set; }
    public string? ConductorNombre { get; set; }

    // Origen y Destino
    public string? OrigenDireccion { get; set; }
    public string? DestinoDireccion { get; set; }

    // Carga
    public string? DescripcionCarga { get; set; }
    public decimal? CubicajeM3 { get; set; }
    public int? PesoEstimadoKg { get; set; }
    public string? Contenedores { get; set; }
}

public class GenerarDeCaResultadoDto
{
    public bool Exito { get; set; }
    public string? GuidDeCa { get; set; }
    public string? UrlDescarga { get; set; }
    public string? Mensaje { get; set; }
    public DateTime? FechaHoraEmision { get; set; }
    public string? Tipo { get; set; }
}
