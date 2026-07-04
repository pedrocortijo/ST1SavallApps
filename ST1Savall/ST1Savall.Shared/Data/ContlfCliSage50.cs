using System;

namespace ST1Savall.Shared.Data;

public class ContlfCliSage50
{
    public string Cliente { get; set; } = string.Empty;
    public int Linea { get; set; } = 0;
    public bool Predet { get; set; } = false;
    public string Persona { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Observa { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Skype { get; set; } = string.Empty;
    public string Facebook { get; set; } = string.Empty;
    public string Twitter { get; set; } = string.Empty;
    public int Lincontcli { get; set; } = 0;
    public int Lintelfcli { get; set; } = 0;
    public bool Vista { get; set; } = false;
    public string Guid { get; set; } = string.Empty;
    public string GuidExp { get; set; } = string.Empty;
    public DateTime? Exportar { get; set; }
    public DateTime? Importar { get; set; }
    public string GuidId { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime Modified { get; set; } = DateTime.Now;
    public int Tipo { get; set; } = 1;

    public string Key => $"{Cliente?.Trim()}_{Linea}";
}