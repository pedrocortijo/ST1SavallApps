using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("Contenedores")]
public class Contenedor
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdContenedor { get; set; }

    [Required]
    [MaxLength(20)]
    public string NumSerie { get; set; } = string.Empty;

    [Required]
    public int IdTipo { get; set; }

    [ForeignKey("IdTipo")]
    public ContenedorTipo? Tipo { get; set; }

    [Required]
    [MaxLength(20)]
    public string EstadoFisico { get; set; } = "Disponible";

    public DateOnly? UltimaRevision { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    [NotMapped]
    public string? ObraActual { get; set; }
}


