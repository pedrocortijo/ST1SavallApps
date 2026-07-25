using System;
using System.ComponentModel.DataAnnotations;

namespace ST1Savall.Shared.Data;

public class FpagSage50
{
    [MaxLength(2)] public string Codigo { get; set; } = string.Empty;
    [MaxLength(50)] public string Nombre { get; set; } = string.Empty;
    public decimal RecFinan { get; set; }
    public bool? Vista { get; set; }
    [MaxLength(2)] public string Grupo { get; set; } = string.Empty;
    public bool Efectivo { get; set; }
    public int Csb34 { get; set; }
    public int DiasRiesgo { get; set; }
    public bool Contado { get; set; }
    [MaxLength(50)] public string Guid { get; set; } = string.Empty;
    public DateTime? Importar { get; set; }
    [MaxLength(2)] public string Fraefpag { get; set; } = string.Empty;
    [MaxLength(50)] public string GuidId { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime Modified { get; set; } = DateTime.Now;
    public bool Domibanc { get; set; }
    public bool Girmescomp { get; set; }
    [MaxLength(100)] public string InfoAdi { get; set; } = string.Empty;

    public string Key => Codigo?.Trim() ?? string.Empty;
}
