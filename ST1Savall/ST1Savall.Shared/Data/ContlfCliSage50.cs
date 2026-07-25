using System;
using System.ComponentModel.DataAnnotations;

namespace ST1Savall.Shared.Data;

public class ContlfCliSage50
{
    [MaxLength(8)] public string Cliente { get; set; } = string.Empty;
    public int Linea { get; set; } = 0;
    public bool Predet { get; set; } = false;
    [MaxLength(30)] public string Persona { get; set; } = string.Empty;
    [MaxLength(30)] public string Cargo { get; set; } = string.Empty;
    [MaxLength(15)] public string Telefono { get; set; } = string.Empty;
    [MaxLength(150)] public string Observa { get; set; } = string.Empty;
    [MaxLength(150)] public string Email { get; set; } = string.Empty;
    [MaxLength(100)] public string Skype { get; set; } = string.Empty;
    [MaxLength(254)] public string Facebook { get; set; } = string.Empty;
    [MaxLength(254)] public string Twitter { get; set; } = string.Empty;
    public int Lincontcli { get; set; } = 0;
    public int Lintelfcli { get; set; } = 0;
    public bool Vista { get; set; } = false;
    [MaxLength(50)] public string Guid { get; set; } = string.Empty;
    [MaxLength(50)] public string GuidExp { get; set; } = string.Empty;
    public DateTime? Exportar { get; set; }
    public DateTime? Importar { get; set; }
    [MaxLength(50)] public string GuidId { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime Modified { get; set; } = DateTime.Now;
    public int Tipo { get; set; } = 1;

    public string Key => $"{Cliente?.Trim()}_{Linea}";
}
