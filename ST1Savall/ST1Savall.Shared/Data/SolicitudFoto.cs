using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ST1Savall.Shared.Data;

[Table("SolicitudFotos")]
public class SolicitudFoto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int IdSolicitud { get; set; }

    [MaxLength(255)]
    [JsonIgnore]
    public string RutaArchivo { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? NombreArchivo { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}
