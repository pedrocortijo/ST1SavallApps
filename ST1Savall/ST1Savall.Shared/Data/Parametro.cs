using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
}
