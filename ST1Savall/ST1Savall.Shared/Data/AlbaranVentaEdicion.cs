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
    [Required, MaxLength(20)] public string Articulo { get; set; } = string.Empty;
    [Range(0.000001, 999999999)] public decimal Unidades { get; set; } = 1;
    public decimal Precio { get; set; }
    public string Clave => $"{Empresa}|{Numero}|{Serie}";
}
