using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

public class ObraComunSage50
{
    [MaxLength(16)] public string Cif { get; set; } = string.Empty;
    [MaxLength(8)] public string Cliente { get; set; } = string.Empty;
    [MaxLength(5)] public string Codigo { get; set; } = string.Empty;
    [MaxLength(13)] public string Codpost { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.Now;
    public decimal Descuento { get; set; } = 0.0m;
    [MaxLength(50)] public string Direccion { get; set; } = string.Empty;
    [MaxLength(30)] public string Encargado { get; set; } = string.Empty;
    [MaxLength(15)] public string Fax { get; set; } = string.Empty;
    [MaxLength(2)] public string Fpag { get; set; } = string.Empty;
    [MaxLength(50)] public string GuidId { get; set; } = string.Empty;
    public bool Isp { get; set; } = false;
    [MaxLength(10)] public string Libre1 { get; set; } = string.Empty;
    [MaxLength(10)] public string Libre2 { get; set; } = string.Empty;
    [MaxLength(30)] public string Libre3 { get; set; } = string.Empty;
    [MaxLength(2)] public string Marvehic { get; set; } = string.Empty;
    public DateTime Modified { get; set; } = DateTime.Now;
    [MaxLength(4)] public string Modvehic { get; set; } = string.Empty;
    [MaxLength(15)] public string Movil { get; set; } = string.Empty;
    [MaxLength(50)] public string Nombre { get; set; } = string.Empty;
    public string? Observacio { get; set; }
    public string Password { get; set; } = string.Empty;
    [MaxLength(30)] public string Poblacion { get; set; } = string.Empty;
    public int Posicion { get; set; } = 0;

    [NotMapped]
    public bool Bloqueada
    {
        get => Posicion == 1;
        set => Posicion = value ? 1 : 0;
    }
    public decimal Pp { get; set; } = 0.0m;
    [MaxLength(30)] public string Provincia { get; set; } = string.Empty;
    [MaxLength(2)] public string Ruta { get; set; } = string.Empty;
    [MaxLength(2)] public string Tarifa { get; set; } = string.Empty;
    [MaxLength(15)] public string Telefono { get; set; } = string.Empty;
    public bool? Terminada { get; set; }
    [MaxLength(2)] public string TipoIva { get; set; } = string.Empty;
    [MaxLength(15)] public string Usuario { get; set; } = string.Empty;
    [MaxLength(5)] public string Vendedor { get; set; } = string.Empty;
    public bool? Vista { get; set; }
    [MaxLength(4)] public string Zona { get; set; } = string.Empty;
}
