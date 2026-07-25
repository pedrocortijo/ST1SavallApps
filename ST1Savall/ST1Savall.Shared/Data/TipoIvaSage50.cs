using System;
using System.ComponentModel.DataAnnotations;

namespace ST1Savall.Shared.Data;

public class TipoIvaSage50
{
    [MaxLength(2)] public string Codigo { get; set; } = string.Empty;
    [MaxLength(50)] public string Nombre { get; set; } = string.Empty;
    public decimal Iva { get; set; }
    public decimal Recarg { get; set; }
    [MaxLength(8)] public string CtaIvSop { get; set; } = string.Empty;
    [MaxLength(8)] public string CtaIvRep { get; set; } = string.Empty;
    [MaxLength(8)] public string CtaReSop { get; set; } = string.Empty;
    [MaxLength(8)] public string CtaReRep { get; set; } = string.Empty;
    public bool? Vista { get; set; } = true;
    public int Comunitari { get; set; } = 1;
    public bool Inmovil { get; set; } = false;
    [MaxLength(2)] public string IvaCee { get; set; } = string.Empty;
    public bool Deduce { get; set; } = true;
    public bool Exento { get; set; } = false;
    public bool AgViaje { get; set; } = false;
    [MaxLength(8)] public string Pendevrep { get; set; } = string.Empty;
    [MaxLength(8)] public string Pendedsop { get; set; } = string.Empty;
    [MaxLength(50)] public string Guid { get; set; } = string.Empty;
    public DateTime? Importar { get; set; }
    [MaxLength(8)] public string Recsopcdev { get; set; } = string.Empty;
    [MaxLength(8)] public string Recrepcdev { get; set; } = string.Empty;
    public int Grupoiva { get; set; } = 0;
    [MaxLength(2)] public string Ivaequierp { get; set; } = string.Empty;
    public int Territerp { get; set; } = 0;
    [MaxLength(50)] public string GuidId { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime Modified { get; set; } = DateTime.Now;
    public int Tipo { get; set; } = 0;
    public bool IgicImpli { get; set; } = false;
    [MaxLength(8)] public string Prtivsopnd { get; set; } = string.Empty;
    [MaxLength(8)] public string Prtivsndpd { get; set; } = string.Empty;
    public int TipoImp { get; set; } = 0;
    public bool Cero { get; set; } = false;
    public bool BInv { get; set; } = false;

    // Helper property to display code, name and rate in the UI.
    public string DisplayLabel => $"{Codigo?.Trim()} - {Nombre?.Trim()} ({Iva:N2}%)";

    public string Key => Codigo?.Trim() ?? string.Empty;
}
