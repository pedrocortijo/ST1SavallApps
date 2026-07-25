using System;
using System.ComponentModel.DataAnnotations;

namespace ST1Savall.Shared.Data;

public class UsuarioComunSage50
{
    [MaxLength(15)] public string Codigo { get; set; } = string.Empty;
    [MaxLength(100)] public string Nombre { get; set; } = string.Empty;
}
