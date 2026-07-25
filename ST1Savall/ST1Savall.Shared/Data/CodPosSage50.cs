using System;
using System.ComponentModel.DataAnnotations;

namespace ST1Savall.Shared.Data;

public class CodPosSage50
{
    [MaxLength(10)] public string Codigo { get; set; } = string.Empty;
    [MaxLength(5)] public string Cpostalm { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.Now;
    [MaxLength(50)] public string GuidId { get; set; } = string.Empty;
    [MaxLength(30)] public string Lati { get; set; } = string.Empty;
    [MaxLength(5)] public string Linea { get; set; } = string.Empty;
    [MaxLength(30)] public string Longi { get; set; } = string.Empty;
    public DateTime Modified { get; set; } = DateTime.Now;
    [MaxLength(10)] public string Poblacerp { get; set; } = string.Empty;
    [MaxLength(30)] public string Poblacion { get; set; } = string.Empty;
    [MaxLength(30)] public string Provincia { get; set; } = string.Empty;
    [MaxLength(10)] public string Provinerp { get; set; } = string.Empty;
    public bool? Vista { get; set; }
    public string Key => $"{Codigo?.Trim()}_{Linea?.Trim()}";
}
