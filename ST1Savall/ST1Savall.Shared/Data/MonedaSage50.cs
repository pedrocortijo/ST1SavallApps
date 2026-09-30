using System.ComponentModel.DataAnnotations;

namespace ST1Savall.Shared.Data;

/// <summary>Divisa de Sage 50 (tabla MONEDA).</summary>
public class MonedaSage50
{
    [MaxLength(3)] public string Codigo { get; set; } = string.Empty;
    [MaxLength(20)] public string Nombre { get; set; } = string.Empty;
    public string Descripcion => $"{Codigo?.Trim()} - {Nombre?.Trim()}";
}
