using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST1Savall.Shared.Data;

[Table("Obras")]
public class Obra
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdObra { get; set; }

    [MaxLength(400)]
    public string? Descripcion { get; set; }

    [MaxLength(200)]
    public string? Ubicacion { get; set; }

    [MaxLength(100)]
    public string? Poblacion { get; set; }

    [MaxLength(5)]
    public string? CodigoPostal { get; set; }

    public bool? Finalizada { get; set; }
    public bool? Visible { get; set; }
    public int? Contador { get; set; }
    public int? IdEmpresa { get; set; }
    public int? Anyo { get; set; }

    [MaxLength(15)]
    public string? Cliente { get; set; }

    [MaxLength(100)]
    public string? Provincia { get; set; }

    [MaxLength(30)]
    public string? Codigo { get; set; }

    [MaxLength(15)]
    public string? Nima { get; set; }

    [MaxLength(200)]
    public string? NombreCliente { get; set; }

    [MaxLength(200)]
    public string? DireccionCliente { get; set; }

    [MaxLength(100)]
    public string? PoblacionCliente { get; set; }

    [MaxLength(5)]
    public string? CodigoPostalCliente { get; set; }

    [MaxLength(20)]
    public string? TelefonoCliente { get; set; }

    [MaxLength(20)]
    public string? Movil { get; set; }

    [MaxLength(20)]
    public string? TelefonoContactoCliente { get; set; }

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [MaxLength(100)]
    public string? EmailCliente { get; set; }

    [MaxLength(200)]
    public string? ResponsableCliente { get; set; }

    [MaxLength(100)]
    public string? Encargado { get; set; }
}