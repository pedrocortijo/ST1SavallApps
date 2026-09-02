using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ST1Savall.Shared.Data;

[Table("Parametros")]
public class Parametro
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required, MaxLength(256)]
    public string Empresa { get; set; } = string.Empty;

    [Required, MaxLength(256), EmailAddress]
    public string ReceiverEmail { get; set; } = string.Empty;

    [Required, MaxLength(256), EmailAddress]
    public string SenderEmail { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string SmtpServer { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int SmtpPort { get; set; }

    [MaxLength(50)]
    public string? SmtpUser { get; set; }

    [MaxLength(50)]
    public string? SmtpPassword { get; set; }

    [MaxLength(30)]
    public string? SslSmtpType { get; set; }

    public int AvisoTiempoServicio { get; set; }

    public int RedondeoHora { get; set; } = 5;

    public int DuracionOperacionServicioMinutos { get; set; } = 30;

    public int AvisoTiempoContenedor { get; set; }

    [Required, MaxLength(2)]
    [Column(TypeName = "char(2)")]
    public string SerieAlbaranes { get; set; } = string.Empty;

    [MaxLength(2)]
    [Column(TypeName = "char(2)")]
    public string? EmpresaAlbaranes { get; set; }

    [Required, MaxLength(3)]
    [Column(TypeName = "char(3)")]
    public string AlmacenAlbaranes { get; set; } = string.Empty;

    [MaxLength(25)]
    [Column(TypeName = "char(25)")]
    public string? UsuarioAlbaranes { get; set; }

    [MaxLength(500)]
    public string? ExcelAlbaranesSabospaAlicante { get; set; }

    [MaxLength(500)]
    public string? ExcelAlbaranesSabospaFinestrat { get; set; }

    [MaxLength(500)]
    public string? ExcelAlbaranesSabospaMonforte { get; set; }
    [MaxLength(255)]
    public string? ExcelAlbaranesSabospaAlicanteNombre { get; set; }

    [MaxLength(255)]
    public string? ExcelAlbaranesSabospaFinestratNombre { get; set; }

    [MaxLength(255)]
    public string? ExcelAlbaranesSabospaMonforteNombre { get; set; }

    [MaxLength(255)]
    [Column(TypeName = "varchar(255)")]
    public string? PathImagenes { get; set; }

    [MaxLength(255)]
    [Column(TypeName = "varchar(255)")]
    public string? PathFirmas { get; set; }

    public int? EstadoReprogramacion { get; set; }

    public int? EstadoIniciado { get; set; }

    public int? EstadoFinalizado { get; set; }

    public int? EstadoAdjudicado { get; set; }

    public int? EstadoPendiente { get; set; }

    [MaxLength(100)]
    public string? AdminPassword { get; set; }

    [MaxLength(500)]
    public string? WialonUrl { get; set; }

    [MaxLength(100)]
    public string? WialonUsuario { get; set; }

    [MaxLength(500)]
    public string? MapboxBaseUrl { get; set; }
    [MaxLength(50)]
    public string? MapboxProfile { get; set; }
    public int? MapboxCacheDurationHours { get; set; }
    public int? MapboxCoordinatePrecision { get; set; }
    [JsonIgnore]
    [MaxLength(500)]
    [Column("MapboxAccessToken")]
    public string? MapboxAccessTokenProtegido { get; set; }
    [NotMapped]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MapboxAccessToken { get; set; }
    [MaxLength(255)]
    public string? WialonHost { get; set; }
    [JsonIgnore]
    [MaxLength(500)]
    [Column("WialonAccessToken")]
    public string? WialonAccessTokenProtegido { get; set; }
    [NotMapped]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WialonAccessToken { get; set; }
    [JsonIgnore]
    [MaxLength(500)]
    [Column("WialonPassword")]
    public string? WialonPasswordProtegida { get; set; }

    [NotMapped]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WialonPassword { get; set; }
}