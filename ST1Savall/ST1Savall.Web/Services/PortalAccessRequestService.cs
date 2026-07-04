using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ST1Savall.Web.Services
{
    public interface IPortalAccessRequestService
    {
        Task SendAccessRequestAsync(string nombre, string email, string telefono, string mensaje);
    }

    public class PortalAccessRequestService : IPortalAccessRequestService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PortalAccessRequestService> _logger;

        public PortalAccessRequestService(IConfiguration configuration, ILogger<PortalAccessRequestService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAccessRequestAsync(string nombre, string email, string telefono, string mensaje)
        {
            var receiverEmail = _configuration["PortalAccessSettings:ReceiverEmail"] ?? "admin@sat.com";
            var senderEmail = _configuration["PortalAccessSettings:SenderEmail"] ?? "no-reply@sat.com";
            var smtpServer = _configuration["PortalAccessSettings:SmtpServer"] ?? "localhost";
            var smtpPortString = _configuration["PortalAccessSettings:SmtpPort"];
            int smtpPort = 25;
            if (int.TryParse(smtpPortString, out int port))
            {
                smtpPort = port;
            }

            var subject = "Nueva solicitud de acceso al Portal SAT";
            var body = $@"
<h3>Nueva Solicitud de Acceso al Portal</h3>
<p>Se ha recibido una nueva solicitud de acceso con los siguientes datos:</p>
<ul>
    <li><strong>Nombre / Razón Social:</strong> {nombre}</li>
    <li><strong>Correo electrónico:</strong> {email}</li>
    <li><strong>Teléfono:</strong> {telefono}</li>
    <li><strong>Mensaje / Comentarios:</strong> {mensaje}</li>
</ul>
<p>Fecha de la solicitud: {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
";

            _logger.LogInformation("Enviando correo de solicitud de acceso a {ReceiverEmail} para {UserEmail}", receiverEmail, email);

            try
            {
                using (var mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(senderEmail);
                    mailMessage.To.Add(receiverEmail);
                    mailMessage.Subject = subject;
                    mailMessage.Body = body;
                    mailMessage.IsBodyHtml = true;

                    using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
                    {
                        smtpClient.EnableSsl = false;
                        await smtpClient.SendMailAsync(mailMessage);
                    }
                }
                _logger.LogInformation("Correo enviado con éxito.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar el correo de solicitud de acceso.");
                // We catch but don't throw to avoid user-visible crashes, ensuring the UI flow continues gracefully
            }
        }
    }
}
