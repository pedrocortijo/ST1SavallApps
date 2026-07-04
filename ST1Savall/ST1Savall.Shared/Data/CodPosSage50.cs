using System;

namespace ST1Savall.Shared.Data;

public class CodPosSage50
{
    public string Codigo { get; set; } = string.Empty;
    public string Cpostalm { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.Now;
    public string GuidId { get; set; } = string.Empty;
    public string Lati { get; set; } = string.Empty;
    public string Linea { get; set; } = string.Empty;
    public string Longi { get; set; } = string.Empty;
    public DateTime Modified { get; set; } = DateTime.Now;
    public string Poblacerp { get; set; } = string.Empty;
    public string Poblacion { get; set; } = string.Empty;
    public string Provincia { get; set; } = string.Empty;
    public string Provinerp { get; set; } = string.Empty;
    public bool? Vista { get; set; }
    public string Key => $"{Codigo?.Trim()}_{Linea?.Trim()}";
}
