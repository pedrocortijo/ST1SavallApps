using System.Net;
using System.Net.Mail;
using System.Text;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.API.Services;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/albaranes-correo")]
public sealed class AlbaranesCorreoController(SageGestionDbContext sage, ApplicationDbContext app,
    SageComunDbContext comun, AlbaranPdfService albaranPdf, SmtpAlbaranService smtpAlbaran,
    ILogger<AlbaranesCorreoController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AlbaranCorreo>>> Pendientes([FromQuery] bool enviados, [FromQuery] bool todos, CancellationToken ct)
    {
        var consulta = sage.AlbaranesVenta.AsNoTracking().Where(a => a.LIBRE_4 == (enviados ? 1 : 0));
        if (!enviados && !todos)
        {
            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var inicioMesSiguiente = inicioMes.AddMonths(1);
            consulta = consulta.Where(a => a.FECHA >= inicioMes && a.FECHA < inicioMesSiguiente);
        }
        var cabeceras = await consulta
            .OrderByDescending(a => a.FECHA).ThenBy(a => a.NUMERO).ToListAsync(ct);
        var codigos = cabeceras.Select(a => a.CLIENTE.Trim()).Distinct().ToList();
        var clientes = await sage.Clientes.AsNoTracking().Where(c => codigos.Contains(c.Codigo.Trim())).ToListAsync(ct);
        var contactosEnvio = await sage.ContlfCli.AsNoTracking()
            .Where(contacto => contacto.Vista && codigos.Contains(contacto.Cliente.Trim()))
            .ToListAsync(ct);
        return cabeceras.Select(a =>
        {
            var c = clientes.FirstOrDefault(c => c.Codigo == a.CLIENTE && c.Clienteerp == a.CLIENTEERP)
                ?? clientes.FirstOrDefault(c => c.Codigo == a.CLIENTE);
            return new AlbaranCorreo
            {
                Empresa = a.EMPRESA?.Trim() ?? string.Empty,
                Numero = a.NUMERO?.Trim() ?? string.Empty,
                Serie = a.LETRA?.Trim() ?? string.Empty,
                Fecha = a.FECHA,
                Cliente = c?.Nombre?.Trim() ?? a.CLIENTE?.Trim() ?? string.Empty,
                Email = ObtenerDestinatarios(contactosEnvio.Where(contacto => contacto.Cliente.Trim() == a.CLIENTE.Trim())),
                Obra = a.OBRA?.Trim() ?? string.Empty,
                Total = a.TOTALDOC
            };
        }).ToList();
    }

    [HttpPost("enviar")]
    public async Task<ActionResult<ResultadoAlbaranCorreo>> Enviar(ClaveAlbaranCorreo clave)
    {
        if (string.IsNullOrWhiteSpace(clave.Empresa) || string.IsNullOrWhiteSpace(clave.Numero)
            || clave.Empresa.Length > 2 || clave.Numero.Length > 20 || clave.Serie is null || clave.Serie.Length > 2)
            return BadRequest(new { message = "Clave de albarán no válida." });
        var etiqueta = $"{clave.Empresa}/{clave.Serie}/{clave.Numero}";
        var destino = "";
        var iniciado = false;
        var aceptado = false;
        try
        {
            var p = await app.Parametros.AsNoTracking().FirstOrDefaultAsync();
            if (p is null || string.IsNullOrWhiteSpace(p.SmtpServer) || p.SmtpPort is < 1 or > 65535
                || !MailAddress.TryCreate(p.SenderEmail, out var remitente))
                return Ok(new ResultadoAlbaranCorreo(etiqueta, destino, false, "Revise servidor, puerto y remitente SMTP en Parámetros."));
            if (string.IsNullOrWhiteSpace(p.PathDocumentos))
                return Ok(new ResultadoAlbaranCorreo(etiqueta, destino, false, "Configure la ruta de documentos en Parámetros antes de enviar."));
            var seguridad = p.SslSmtpType?.Trim().ToUpperInvariant() switch
            {
                "SSL/TLS" => true,
                "STARTTLS" => true,
                "NINGUNO" => false,
                _ => (bool?)null
            };
            if (seguridad is null)
                return Ok(new ResultadoAlbaranCorreo(etiqueta, destino, false, "Seleccione un modo SMTP válido en Parámetros."));

            // SQL lock protects against concurrent sends from different API instances.
            // Do not bind completion to RequestAborted: record SMTP acceptance even if the browser closes.
            await using var tx = await sage.Database.BeginTransactionAsync();
            var a = await sage.AlbaranesVenta.FromSqlInterpolated($"SELECT * FROM c_albven WITH (UPDLOCK, HOLDLOCK) WHERE EMPRESA = {clave.Empresa} AND LTRIM(RTRIM(NUMERO)) = {clave.Numero.Trim()} AND LETRA = {clave.Serie}")
                .SingleOrDefaultAsync();
            if (a is null)
                return Ok(new ResultadoAlbaranCorreo(etiqueta, destino, false, "El albarán no existe."));
            var esReenvio = a.LIBRE_4 == 1;
            var c = await sage.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Codigo == a.CLIENTE && c.Clienteerp == a.CLIENTEERP)
                ?? await sage.Clientes.AsNoTracking().OrderBy(c => c.Clienteerp).FirstOrDefaultAsync(c => c.Codigo == a.CLIENTE);
            if (c is null)
                return Ok(new ResultadoAlbaranCorreo(etiqueta, destino, false, "No se ha encontrado el cliente Sage del albarán."));

            var destinatarios = await sage.ContlfCli.AsNoTracking()
                .Where(contacto => contacto.Cliente.Trim() == a.CLIENTE.Trim() && contacto.Vista)
                .Select(contacto => contacto.Email)
                .ToListAsync();
            var correos = ObtenerDireccionesValidas(destinatarios);
            destino = string.Join("; ", correos);
            if (correos.Count == 0)
                return Ok(new ResultadoAlbaranCorreo(etiqueta, destino, false, "El cliente no tiene contactos de envío de albaranes (VISTA = 1) con un correo válido."));
            var lineas = await sage.LineasAlbaranesVenta.AsNoTracking().Where(l => l.EMPRESA == a.EMPRESA
                && l.NUMERO == a.NUMERO && l.LETRA == a.LETRA).OrderBy(l => l.LINIA).ToListAsync();
            if (lineas.Count == 0)
                return Ok(new ResultadoAlbaranCorreo(etiqueta, destino, false, "El albarán no tiene líneas."));
            var obra = string.IsNullOrWhiteSpace(a.OBRA) ? null : await comun.Obras.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Codigo.Trim() == a.OBRA.Trim());
            var tipoIva = await sage.TipoIva.AsNoTracking().FirstOrDefaultAsync(t => t.Codigo.Trim() == lineas[0].TIPO_IVA.Trim());
            var carpeta = Path.GetFullPath(p.PathDocumentos.Trim());
            Directory.CreateDirectory(carpeta);
            var nombrePdf = $"ALBARAN_{NombreSeguro(a.EMPRESA)}_{NombreSeguro(a.LETRA)}_{NombreSeguro(a.NUMERO)}.pdf";
            var rutaPdf = Path.Combine(carpeta, nombrePdf);
            var serieAlbaran = a.LETRA.Trim();
            var numeroAlbaran = a.NUMERO.Trim();
            var solicitudes = await app.Solicitudes.AsNoTracking()
                .Where(s => s.AlbaranSerieSage != null && s.AlbaranNumeroSage != null
                    && s.AlbaranSerieSage.Trim() == serieAlbaran && s.AlbaranNumeroSage.Trim() == numeroAlbaran)
                .ToListAsync();
            if (solicitudes.Count == 0 && int.TryParse(numeroAlbaran, out var idSolicitudAnterior))
            {
                var solicitudAnterior = await app.Solicitudes.AsNoTracking().FirstOrDefaultAsync(s => s.IdSolicitud == idSolicitudAnterior);
                if (solicitudAnterior is not null) solicitudes.Add(solicitudAnterior);
            }
            var solicitud = solicitudes.FirstOrDefault();
            var idSolicitud = solicitud?.IdSolicitud ?? 0;
            var rutaFirma = solicitud?.FirmaPath;
            if (string.IsNullOrWhiteSpace(rutaFirma) && idSolicitud > 0 && !string.IsNullOrWhiteSpace(p.PathFirmas))
                rutaFirma = Path.Combine(p.PathFirmas.Trim(), $"{idSolicitud}.png");
            var contenedoresEntregados = string.Join(", ", solicitudes.SelectMany(s => new[] { s.CodigoEntrega, s.CodigoAmbosEntrega })
                .Where(codigo => !string.IsNullOrWhiteSpace(codigo)).Select(codigo => codigo!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));
            var contenedoresRetirados = string.Join(", ", solicitudes.SelectMany(s => new[] { s.CodigoRecogida, s.CodigoAmbosRecogida })
                .Where(codigo => !string.IsNullOrWhiteSpace(codigo)).Select(codigo => codigo!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));
            var fotosSolicitud = idSolicitud > 0
                ? await app.SolicitudFotos.AsNoTracking().Where(f => f.IdSolicitud == idSolicitud).OrderBy(f => f.FechaCreacion).Select(f => f.RutaArchivo).ToListAsync()
                : [];
            Camion? camion = null;
            if (solicitud?.IdConductor is int idConductor)
            {
                var idCamion = await app.Operarios.AsNoTracking().Where(o => o.IdOperario == idConductor).Select(o => o.IdCamion).FirstOrDefaultAsync();
                if (idCamion.HasValue) camion = await app.Camiones.AsNoTracking().FirstOrDefaultAsync(c => c.IdCamion == idCamion.Value);
            }
            var pdf = albaranPdf.Crear(p.Empresa, a, c, obra, lineas, tipoIva, solicitud?.FirmaNombre, solicitud?.FirmaDni, rutaFirma, fotosSolicitud, contenedoresEntregados, contenedoresRetirados, solicitud, camion);
            await System.IO.File.WriteAllBytesAsync(rutaPdf, pdf);
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            iniciado = true;
            foreach (var correo in correos)
            {
                await smtpAlbaran.EnviarAsync(p, remitente.Address, correo,
                    $"Albarán {a.LETRA.Trim()}-{a.NUMERO.Trim()} · {p.Empresa}",
                    $"Estimado/a {c.Nombre.Trim()},\n\nAdjuntamos su albarán de fecha {a.FECHA:dd/MM/yyyy}.\n\nAtentamente,\n{p.Empresa}",
                    nombrePdf, pdf, limite.Token);
            }
            aceptado = true;
            a.LIBRE_4 = 1;
            a.MODIFIED = DateTime.Now;
            await sage.SaveChangesAsync();
            await tx.CommitAsync();
            logger.LogInformation("Albarán {Albaran} aceptado por SMTP. Usuario {Usuario}", etiqueta, User.Identity?.Name);
            return Ok(new ResultadoAlbaranCorreo(etiqueta, destino, true, esReenvio ? "Aceptado por SMTP y reenviado correctamente." : "Aceptado por SMTP y marcado como enviado."));
        }
        catch (Exception ex) when (ex is IOException or System.Security.Authentication.AuthenticationException or System.Net.Sockets.SocketException)
        {
            logger.LogWarning(ex, "SMTP rechazó el albarán {Albaran}", etiqueta);
            return Ok(new ResultadoAlbaranCorreo(etiqueta, destino, false, $"No se pudo conectar o autenticar en SMTP: {ex.GetBaseException().Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error de envío {Albaran}. Aceptado SMTP: {Aceptado}", etiqueta, aceptado);
            return Ok(new ResultadoAlbaranCorreo(etiqueta, destino, aceptado, aceptado
                ? "SMTP aceptó el correo, pero no se pudo confirmar la marca en Sage. Revise antes de reenviar."
                : iniciado ? "Resultado SMTP incierto. Revise el servidor antes de reenviar para evitar duplicados."
                : "No enviado. Revise conexión, configuración SMTP y registros del servidor."));
        }
    }

    private static string ObtenerDestinatarios(IEnumerable<ContlfCliSage50> contactos) =>
        string.Join("; ", ObtenerDireccionesValidas(contactos.Select(contacto => contacto.Email)));

    private static List<string> ObtenerDireccionesValidas(IEnumerable<string?> correos) => correos
        .Select(correo => correo?.Trim())
        .Where(correo => !string.IsNullOrWhiteSpace(correo) && MailAddress.TryCreate(correo, out _))
        .Select(correo => correo!)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static string E(string? valor) => WebUtility.HtmlEncode(valor?.Trim() ?? "");
    private static string NombreSeguro(string? valor) => string.Concat((valor ?? string.Empty).Trim()
        .Select(caracter => Path.GetInvalidFileNameChars().Contains(caracter) ? '_' : caracter));
    private static string N(decimal valor) => valor.ToString("N2", CultureInfo.GetCultureInfo("es-ES"));
    private static string Documento(string empresa, AlbaranVentaSage50 a, ClienteSage50 c, List<LineaAlbaranVentaSage50> lineas)
    {
        var filas = string.Concat(lineas.Select(l => $"<tr><td>{E(l.ARTICULO)}</td><td>{E(l.DEFINICION)}</td><td>{N(l.UNIDADES)}</td><td>{N(l.PRECIO)}</td><td>{N(l.DTO1)} / {N(l.DTO2)}</td><td>{N(l.IMPORTE)}</td></tr>"));
        return $"<!doctype html><html lang='es'><head><meta charset='utf-8'><title>Albarán {E(a.NUMERO)}</title><style>body{{font:14px Arial,sans-serif;margin:40px;color:#172b4d}}h1{{color:#174574}}table{{width:100%;border-collapse:collapse;margin:24px 0}}td,th{{padding:9px;border-bottom:1px solid #ccd;text-align:left}}th{{background:#eef3f9}}tr{{break-inside:avoid}}.total{{text-align:right;font-size:18px}}pre{{white-space:pre-wrap;font:inherit}}@media print{{body{{margin:15mm}}thead{{display:table-header-group}}}}</style></head><body><h2>{E(empresa)}</h2><h1>Albarán {E(a.LETRA)}-{E(a.NUMERO)}</h1><p>Fecha: {a.FECHA:dd/MM/yyyy} · Empresa Sage: {E(a.EMPRESA)}</p><h3>{E(c.Nombre)}</h3><p>NIF: {E(c.Cif)}<br>{E(c.Direccion)}<br>{E(c.Codpost)} {E(c.Poblacion)}</p><p>Obra: {E(a.OBRA)}</p><table><thead><tr><th>Artículo</th><th>Descripción</th><th>Unidades</th><th>Precio</th><th>Dto. %</th><th>Importe</th></tr></thead><tbody>{filas}</tbody></table><p class='total'>Total documento: {N(a.TOTALDOC)}</p><h3>Observaciones</h3><pre>{E(a.OBSERVACIO)}</pre></body></html>";
    }
}
