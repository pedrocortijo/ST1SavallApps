using System;
using System.ComponentModel.DataAnnotations;

namespace ST1Savall.Shared.Data;

public class DocumentoDeCa
{
    public int IdDocumentoDeCa { get; set; }
    public int IdSolicitud { get; set; }
    [MaxLength(50)] public string GuidDeCa { get; set; } = string.Empty;
    [MaxLength(20)] public string Tipo { get; set; } = string.Empty;
    public DateTime FechaHoraEmision { get; set; }
    [MaxLength(255)] public string RutaArchivo { get; set; } = string.Empty;
    [MaxLength(500)] public string? UrlPublica { get; set; }
}
