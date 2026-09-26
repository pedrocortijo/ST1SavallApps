using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ST1Savall.Shared.Data;

[Table("SolicitudTiquesPesaje")]
public class SolicitudTiquePesaje
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int IdSolicitud { get; set; }
    public int? IdPlanta { get; set; }
    [MaxLength(500), JsonIgnore] public string RutaArchivo { get; set; } = string.Empty;
    [MaxLength(150)] public string? NombreArchivo { get; set; }
    public string? TextoOcr { get; set; }
    [MaxLength(20)] public string? NumeroAlbaran { get; set; }
    [MaxLength(20)] public string? Matricula { get; set; }
    public DateTime? Fecha { get; set; }
    public TimeSpan? Hora { get; set; }
    public int? PesoNetoKg { get; set; }
    [Column(TypeName = "decimal(5,4)")] public decimal? Confianza { get; set; }
    public bool Confirmado { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaConfirmacion { get; set; }
    [MaxLength(256)] public string? UsuarioConfirmacion { get; set; }
}
