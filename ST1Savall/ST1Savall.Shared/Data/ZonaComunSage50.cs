using System.ComponentModel.DataAnnotations;
namespace ST1Savall.Shared.Data;
public class ZonaComunSage50
{
    [Required, MaxLength(2)] public string Ruta { get; set; } = string.Empty;
    [Required, MaxLength(4)] public string Zona { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string Descripcion { get; set; } = string.Empty;
    public bool? Vista { get; set; } = true;
    public int Linia { get; set; }
    public string Guid { get; set; } = string.Empty;
    public DateTime? Importar { get; set; }
    public DateTime? Exportar { get; set; }
    public string GuidExp { get; set; } = string.Empty;
    public string GuidId { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public string Clave => $"{Ruta.Trim()}|{Zona.Trim()}";
}