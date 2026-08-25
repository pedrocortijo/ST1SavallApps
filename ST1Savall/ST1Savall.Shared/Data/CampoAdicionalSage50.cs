using System.ComponentModel.DataAnnotations;

namespace ST1Savall.Shared.Data;

/// <summary>Valor de un campo adicional de Sage 50 almacenado en MULTICAM.</summary>
public class CampoAdicionalSage50
{
    [MaxLength(8)] public string FICHERO { get; set; } = string.Empty;
    [MaxLength(20)] public string CODIGO { get; set; } = string.Empty;
    [MaxLength(3)] public string CAMPO { get; set; } = string.Empty;
    [MaxLength(100)] public string VALOR { get; set; } = string.Empty;
    public bool VISTA { get; set; } = true;
    [MaxLength(50)] public string GUID_ID { get; set; } = string.Empty;
    public DateTime CREATED { get; set; }
    public DateTime MODIFIED { get; set; }
}
