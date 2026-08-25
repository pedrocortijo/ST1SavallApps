using System.ComponentModel.DataAnnotations;

namespace ST1Savall.Shared.Data;

/// <summary>Valor de un campo adicional de un documento de venta Sage 50 (MULTICA2).</summary>
public class CampoAdicionalDocumentoVentaSage50
{
    [MaxLength(2)] public string EMPRESA { get; set; } = string.Empty;
    [MaxLength(10)] public string NUMERO { get; set; } = string.Empty;
    public int FICHERO { get; set; } = 1;
    [MaxLength(3)] public string CAMPO { get; set; } = string.Empty;
    [MaxLength(100)] public string VALOR { get; set; } = string.Empty;
    public bool? VISTA { get; set; } = true;
    [MaxLength(2)] public string LETRA { get; set; } = string.Empty;
    [MaxLength(50)] public string GUID_ID { get; set; } = string.Empty;
    public DateTime CREATED { get; set; }
    public DateTime MODIFIED { get; set; }
}