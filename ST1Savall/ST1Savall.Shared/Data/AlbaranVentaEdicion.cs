using System.ComponentModel.DataAnnotations;

namespace ST1Savall.Shared.Data;

/// <summary>Datos controlados para la cabecera y la única línea de un albarán de venta.</summary>
public class AlbaranVentaEdicion
{
    [Required, MaxLength(2)] public string Empresa { get; set; } = string.Empty;
    [Required, MaxLength(10)] public string Numero { get; set; } = string.Empty;
    [Required, MaxLength(2)] public string Serie { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Today;
    [Required, MaxLength(8)] public string Cliente { get; set; } = string.Empty;
    [Required, MaxLength(3)] public string Almacen { get; set; } = string.Empty;
    [Required, MaxLength(25)] public string Usuario { get; set; } = string.Empty;
    [MaxLength(5)] public string Vendedor { get; set; } = string.Empty;
    [MaxLength(2)] public string FormaPago { get; set; } = string.Empty;
    [MaxLength(10)] public string Factura { get; set; } = string.Empty;
    public DateTime? FechaFactura { get; set; }
    [MaxLength(2)] public string Operario { get; set; } = string.Empty;
    [MaxLength(5)] public string Obra { get; set; } = string.Empty;
    public string ObraNombre { get; set; } = string.Empty;
    [MaxLength(2)] public string Tarifa { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Articulo { get; set; } = string.Empty;
    [MaxLength(100)] public string Definicion { get; set; } = string.Empty;
    public bool Suplido { get; set; }
    [Range(typeof(decimal), "0.000001", "999999999", ParseLimitsInInvariantCulture = true)] public decimal Unidades { get; set; } = 1;
    public decimal Precio { get; set; }
    public decimal Descuento1 { get; set; }
    public decimal Descuento2 { get; set; }
    public decimal ImporteLinea { get; set; }
    public decimal PorcentajeIva { get; set; }
    public decimal ImporteIva { get; set; }
    public decimal TotalDocumento { get; set; }
    public string Clave => $"{Empresa}|{Numero}|{Serie}";
    public string ClienteCif { get; set; } = string.Empty;
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteDireccion { get; set; } = string.Empty;
    public string ClienteCodigoPostal { get; set; } = string.Empty;
    public string ClientePoblacion { get; set; } = string.Empty;
    public string ClienteProvincia { get; set; } = string.Empty;
    public string ClienteTelefono { get; set; } = string.Empty;
    public string ClienteEmail { get; set; } = string.Empty;
    public string Matricula { get; set; } = string.Empty;
    public string AlbaranPlanta { get; set; } = string.Empty;
    public string FechaPlanta { get; set; } = string.Empty;
    public string NetoKg { get; set; } = string.Empty;
}
