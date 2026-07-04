using System;

namespace ST1Savall.Shared.Data;

public class ObraComunSage50
{
    public string Cif { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Codpost { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.Now;
    public decimal Descuento { get; set; } = 0.0m;
    public string Direccion { get; set; } = string.Empty;
    public string Encargado { get; set; } = string.Empty;
    public string Fax { get; set; } = string.Empty;
    public string Fpag { get; set; } = string.Empty;
    public string GuidId { get; set; } = string.Empty;
    public bool Isp { get; set; } = false;
    public string Libre1 { get; set; } = string.Empty;
    public string Libre2 { get; set; } = string.Empty;
    public string Libre3 { get; set; } = string.Empty;
    public string Marvehic { get; set; } = string.Empty;
    public DateTime Modified { get; set; } = DateTime.Now;
    public string Modvehic { get; set; } = string.Empty;
    public string Movil { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Observacio { get; set; }
    public string Password { get; set; } = string.Empty;
    public string Poblacion { get; set; } = string.Empty;
    public int Posicion { get; set; } = 0;
    public decimal Pp { get; set; } = 0.0m;
    public string Provincia { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
    public string Tarifa { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public bool? Terminada { get; set; }
    public string TipoIva { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Vendedor { get; set; } = string.Empty;
    public bool? Vista { get; set; }
    public string Zona { get; set; } = string.Empty;
}
