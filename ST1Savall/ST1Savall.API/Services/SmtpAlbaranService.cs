using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Services;

public sealed class SmtpAlbaranService
{
    public async Task EnviarAsync(Parametro p, string remitente, string destinatario, string asunto,
        string cuerpo, string nombreAdjunto, byte[] adjunto, CancellationToken ct)
    {
        using var cliente = new TcpClient();
        await cliente.ConnectAsync(p.SmtpServer.Trim(), p.SmtpPort, ct);
        Stream canal = cliente.GetStream();
        var configuracionTls = p.SslSmtpType?.Trim().ToUpperInvariant();
        // El servidor configurado usa 587. En ese puerto TLS se negocia con
        // STARTTLS, aunque en la pantalla de parámetros se muestre SSL/TLS.
        // El TLS implícito se reserva para 465.
        var tlsImplicito = configuracionTls == "SSL/TLS" && p.SmtpPort == 465;
        var startTls = configuracionTls == "STARTTLS"
            || (configuracionTls == "SSL/TLS" && p.SmtpPort != 465);
        if (tlsImplicito)
            canal = await ActivarTlsAsync(canal, p.SmtpServer.Trim(), ct);

        using var lector = new StreamReader(canal, Encoding.ASCII, false, 4096, true);
        using var escritor = new StreamWriter(canal, Encoding.ASCII, 4096, true) { NewLine = "\r\n", AutoFlush = true };
        await EsperarAsync(lector, 220, ct);
        var capacidades = await ComandoAsync(lector, escritor, "EHLO ST1Savall", 250, ct);

        if (startTls)
        {
            await ComandoAsync(lector, escritor, "STARTTLS", 220, ct);
            canal = await ActivarTlsAsync(canal, p.SmtpServer.Trim(), ct);
            lector.Dispose(); escritor.Dispose();
            using var lectorTls = new StreamReader(canal, Encoding.ASCII, false, 4096, true);
            using var escritorTls = new StreamWriter(canal, Encoding.ASCII, 4096, true) { NewLine = "\r\n", AutoFlush = true };
            capacidades = await ComandoAsync(lectorTls, escritorTls, "EHLO ST1Savall", 250, ct);
            await AutenticarYEnviarAsync(lectorTls, escritorTls, capacidades, p, remitente, destinatario, asunto, cuerpo, nombreAdjunto, adjunto, ct);
            return;
        }
        await AutenticarYEnviarAsync(lector, escritor, capacidades, p, remitente, destinatario, asunto, cuerpo, nombreAdjunto, adjunto, ct);
    }

    private static async Task AutenticarYEnviarAsync(StreamReader lector, StreamWriter escritor, string capacidades,
        Parametro p, string remitente, string destinatario, string asunto, string cuerpo, string nombreAdjunto, byte[] adjunto, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(p.SmtpUser))
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"\0{p.SmtpUser}\0{p.SmtpPassword ?? string.Empty}"));
            if (capacidades.Contains("AUTH PLAIN", StringComparison.OrdinalIgnoreCase))
                await ComandoAsync(lector, escritor, $"AUTH PLAIN {token}", 235, ct);
            else
            {
                await ComandoAsync(lector, escritor, "AUTH LOGIN", 334, ct);
                await ComandoAsync(lector, escritor, Convert.ToBase64String(Encoding.UTF8.GetBytes(p.SmtpUser)), 334, ct);
                await ComandoAsync(lector, escritor, Convert.ToBase64String(Encoding.UTF8.GetBytes(p.SmtpPassword ?? string.Empty)), 235, ct);
            }
        }
        await ComandoAsync(lector, escritor, $"MAIL FROM:<{remitente}>", 250, ct);
        await ComandoAsync(lector, escritor, $"RCPT TO:<{destinatario}>", 250, ct);
        await ComandoAsync(lector, escritor, "DATA", 354, ct);
        var limite = "=_ST1_" + Guid.NewGuid().ToString("N");
        var asuntoMime = Convert.ToBase64String(Encoding.UTF8.GetBytes(asunto));
        await escritor.WriteLineAsync($"From: <{remitente}>");
        await escritor.WriteLineAsync($"To: <{destinatario}>");
        await escritor.WriteLineAsync($"Subject: =?UTF-8?B?{asuntoMime}?=");
        await escritor.WriteLineAsync("MIME-Version: 1.0");
        await escritor.WriteLineAsync($"Content-Type: multipart/mixed; boundary=\"{limite}\"");
        await escritor.WriteLineAsync();
        await escritor.WriteLineAsync($"--{limite}");
        await escritor.WriteLineAsync("Content-Type: text/plain; charset=utf-8");
        await escritor.WriteLineAsync("Content-Transfer-Encoding: base64");
        await escritor.WriteLineAsync();
        await LineasBase64Async(escritor, Encoding.UTF8.GetBytes(cuerpo));
        await escritor.WriteLineAsync($"--{limite}");
        await escritor.WriteLineAsync($"Content-Type: application/pdf; name=\"{nombreAdjunto}\"");
        await escritor.WriteLineAsync("Content-Transfer-Encoding: base64");
        await escritor.WriteLineAsync($"Content-Disposition: attachment; filename=\"{nombreAdjunto}\"");
        await escritor.WriteLineAsync();
        await LineasBase64Async(escritor, adjunto);
        await escritor.WriteLineAsync($"--{limite}--");
        await escritor.WriteLineAsync(".");
        await EsperarAsync(lector, 250, ct);
        await escritor.WriteLineAsync("QUIT");
    }

    private static async Task<Stream> ActivarTlsAsync(Stream canal, string host, CancellationToken ct)
    {
        var ssl = new SslStream(canal, leaveInnerStreamOpen: false);
        await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = host
        }, ct);
        return ssl;
    }

    private static async Task<string> ComandoAsync(StreamReader lector, StreamWriter escritor, string comando, int esperado, CancellationToken ct)
    {
        await escritor.WriteLineAsync(comando);
        return await EsperarAsync(lector, esperado, ct);
    }

    private static async Task<string> EsperarAsync(StreamReader lector, int esperado, CancellationToken ct)
    {
        var lineas = new List<string>();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var linea = await lector.ReadLineAsync(ct) ?? throw new IOException("El servidor SMTP cerró la conexión.");
            lineas.Add(linea);
            if (linea.Length < 3 || !int.TryParse(linea[..3], out var codigo))
                throw new InvalidOperationException($"Respuesta SMTP no válida: {linea}");
            if (codigo != esperado)
                throw new InvalidOperationException($"SMTP respondió {codigo}: {string.Join(" ", lineas)}");
            if (linea.Length < 4 || linea[3] != '-') return string.Join("\n", lineas);
        }
    }

    private static async Task LineasBase64Async(StreamWriter escritor, byte[] bytes)
    {
        var texto = Convert.ToBase64String(bytes);
        for (var i = 0; i < texto.Length; i += 76)
            await escritor.WriteLineAsync(texto.Substring(i, Math.Min(76, texto.Length - i)));
    }
}
