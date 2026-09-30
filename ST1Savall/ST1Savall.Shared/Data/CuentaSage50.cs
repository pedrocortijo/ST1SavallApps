using System.ComponentModel.DataAnnotations;

namespace ST1Savall.Shared.Data;

/// <summary>Cuenta contable de Sage 50 (tabla CUENTAS).</summary>
public class CuentaSage50
{
    [MaxLength(8)] public string Codigo { get; set; } = string.Empty;
    [MaxLength(120)] public string Nombre { get; set; } = string.Empty;
    public string Descripcion => $"{Codigo?.Trim()} - {Nombre?.Trim()}";
}
