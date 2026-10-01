using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.API.Services;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/facturas-correo")]
public sealed class FacturasCorreoController(SageGestionDbContext sage, ApplicationDbContext app, SageComunDbContext comun, AlbaranPdfService albaranPdf, SmtpAlbaranService smtp, FacturaPdfService pdf, ILogger<FacturasCorreoController> logger) : ControllerBase
{
    [HttpGet("ruta-origen")]
    public async Task<ActionResult<string>> RutaOrigen(CancellationToken ct)
    {
        var p = await app.Parametros.AsNoTracking().FirstOrDefaultAsync(ct);
        return string.IsNullOrWhiteSpace(p?.PathPdfFacturas) ? BadRequest() : Ok(p.PathPdfFacturas.Trim());
    }
    [HttpGet("rutas")]
    public async Task<ActionResult<RutasFacturaCorreo>> Rutas(CancellationToken ct)
    {
        var p = await app.Parametros.AsNoTracking().FirstOrDefaultAsync(ct);
        if (p is null || string.IsNullOrWhiteSpace(p.PathPdfFacturas) || string.IsNullOrWhiteSpace(p.PathDocumentos)) return BadRequest();
        return Ok(new RutasFacturaCorreo(p.PathPdfFacturas.Trim(), p.PathDocumentos.Trim()));
    }
    [HttpGet]
    public async Task<ActionResult<List<FacturaCorreo>>> Pendientes(CancellationToken ct)
    {
        try
        {
        var facturas = await sage.FacturasVenta.AsNoTracking().OrderByDescending(f => f.MODIFIED).Take(500).ToListAsync(ct);
        var claves = facturas.Select(f => f.NUMERO.Trim()).ToList();
        var albaranes = await sage.AlbaranesVenta.AsNoTracking().Where(a => claves.Contains(a.FACTURA.Trim())).ToListAsync(ct);
        var clientes = await sage.Clientes.AsNoTracking().ToListAsync(ct);
        var contactos = await sage.ContlfCli.AsNoTracking().Where(x => x.Vista).ToListAsync(ct);
        var codigosObra = albaranes.Select(a => a.OBRA.Trim()).Where(codigo => !string.IsNullOrWhiteSpace(codigo)).Distinct().ToList();
        var obras = await comun.Obras.AsNoTracking().Where(obra => codigosObra.Contains(obra.Codigo.Trim())).ToListAsync(ct);
        return Ok(facturas.Select(f => {
            var relacionadosSage = albaranes.Where(a => a.FACTURA.Trim() == f.NUMERO.Trim()).OrderBy(a => a.FECHA).ToList();
            var albaranReferencia = relacionadosSage.FirstOrDefault();
            var c = albaranReferencia is null ? null : clientes.FirstOrDefault(x => x.Codigo.Trim() == albaranReferencia.CLIENTE.Trim() && x.Clienteerp.Trim() == albaranReferencia.CLIENTEERP.Trim()) ?? clientes.FirstOrDefault(x => x.Codigo.Trim() == albaranReferencia.CLIENTE.Trim());
            var relacionados = relacionadosSage.Select(a =>
            {
                var obra = obras.FirstOrDefault(o => o.Codigo.Trim() == a.OBRA.Trim());
                var textoObra = string.IsNullOrWhiteSpace(obra?.Nombre) ? a.OBRA.Trim() : $"{a.OBRA.Trim()} - {obra.Nombre.Trim()}";
                return new AlbaranCorreo { Empresa=a.EMPRESA.Trim(), Serie=a.LETRA.Trim(), Numero=a.NUMERO.Trim(), Fecha=a.FECHA, Cliente=c?.Nombre.Trim() ?? a.CLIENTE.Trim(), Obra=textoObra, Total=a.TOTALDOC };
            }).ToList();
            var emails = albaranReferencia is null ? [] : contactos.Where(x => x.Cliente.Trim() == albaranReferencia.CLIENTE.Trim()).Select(x => x.Email.Trim()).Where(x => MailAddress.TryCreate(x, out _)).Distinct().ToList();
            relacionados.ForEach(a => a.Email = string.Join("; ", emails));
            return new FacturaCorreo { Empresa=f.EMPRESA.Trim(), Serie=albaranReferencia?.LETRA.Trim() ?? string.Empty, Numero=f.NUMERO.Trim(), Fecha=albaranReferencia?.FECHA_FAC ?? albaranReferencia?.FECHA ?? f.MODIFIED, Cliente=c?.Nombre.Trim() ?? string.Empty, Email=string.Join("; ", emails), Total=f.TOTALDOC, Albaranes=relacionados };
        }).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudieron cargar las facturas de Sage50");
            return Problem($"No se pudieron consultar las facturas de Sage50: {ex.GetBaseException().Message}", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("enviar")]
    public async Task<ActionResult<ResultadoFacturaCorreo>> Enviar(ClaveFacturaCorreo clave, CancellationToken ct)
    {
        var etiqueta = $"{clave.Empresa}/{clave.Serie}/{clave.Numero}";
        var p = await app.Parametros.AsNoTracking().FirstOrDefaultAsync(ct);
        if (p is null || string.IsNullOrWhiteSpace(p.PathPdfFacturas) || string.IsNullOrWhiteSpace(p.PathDocumentos) || !MailAddress.TryCreate(p.SenderEmail, out var remitente))
            return Ok(new ResultadoFacturaCorreo(etiqueta, "", false, "Configure SMTP, ruta de PDF de facturas y ruta de documentos en Parámetros."));
        var factura = await sage.FacturasVenta.AsNoTracking().SingleOrDefaultAsync(f => f.EMPRESA == clave.Empresa && f.NUMERO.Trim() == clave.Numero.Trim(), ct);
        if (factura is null) return Ok(new ResultadoFacturaCorreo(etiqueta, "", false, "La factura no existe en Sage50."));
        var nombreFactura = $"FA{string.Concat(clave.Numero.Where(char.IsLetterOrDigit))}.pdf";
        var carpetaDocumentos = Path.GetFullPath(p.PathDocumentos.Trim());
        var rutaFactura = Path.Combine(carpetaDocumentos, nombreFactura);
        byte[] pdfFactura;
        try
        {
            pdfFactura = await System.IO.File.ReadAllBytesAsync(rutaFactura, ct);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var identidad = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            return Ok(new ResultadoFacturaCorreo(etiqueta, "", false, $"No se puede leer el PDF local {nombreFactura} en {rutaFactura}: {ex.GetBaseException().Message} (cuenta de la API: {identidad})"));
        }
        var albaranes = await sage.AlbaranesVenta.AsNoTracking().Where(a => a.FACTURA.Trim() == clave.Numero.Trim()).OrderBy(a => a.FECHA).ToListAsync(ct);
        var clienteFactura = albaranes.FirstOrDefault()?.CLIENTE;
        if (string.IsNullOrWhiteSpace(clienteFactura)) return Ok(new ResultadoFacturaCorreo(etiqueta, "", false, "No se encontró el albarán de origen para identificar el cliente de la factura."));
        var correos = await sage.ContlfCli.AsNoTracking().Where(x => x.Cliente.Trim() == clienteFactura.Trim() && x.Vista).Select(x => x.Email).ToListAsync(ct);
        var destinos = correos.Where(x => MailAddress.TryCreate(x?.Trim(), out _)).Select(x => x!.Trim()).Distinct().ToList();
        if (destinos.Count == 0) return Ok(new ResultadoFacturaCorreo(etiqueta, "", false, "El cliente no tiene contactos de facturación válidos."));
        var anexos = new List<byte[]> { pdfFactura };
        foreach (var a in albaranes)
        {
            var ruta = Path.Combine(Path.GetFullPath(p.PathDocumentos.Trim()), $"ALBARAN_{a.EMPRESA.Trim()}_{a.LETRA.Trim()}_{a.NUMERO.Trim()}.pdf");
            if (!System.IO.File.Exists(ruta))
            {
                try
                {
                    var regenerado = await RegenerarAlbaran(a, p, ct);
                    await System.IO.File.WriteAllBytesAsync(ruta, regenerado, ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "No se pudo regenerar el albarán {Albaran} de la factura {Factura}", a.NUMERO, etiqueta);
                    return Ok(new ResultadoFacturaCorreo(etiqueta, string.Join("; ", destinos), false, $"No se pudo regenerar el PDF del albarán {a.LETRA.Trim()}-{a.NUMERO.Trim()}: {ex.GetBaseException().Message}"));
                }
            }
            anexos.Add(await System.IO.File.ReadAllBytesAsync(ruta, ct));
        }
        byte[] combinado;
        try { combinado = pdf.Unir(anexos[0], anexos.Skip(1)); }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo combinar la factura {Factura} y sus albaranes", etiqueta);
            return Ok(new ResultadoFacturaCorreo(etiqueta, string.Join("; ", destinos), false, $"No se pudo preparar el PDF combinado: {ex.GetBaseException().Message}"));
        }
        foreach (var destino in destinos) await smtp.EnviarAsync(p, remitente.Address, destino, $"Factura {clave.Serie}-{clave.Numero} · {p.Empresa}", $"Adjuntamos su factura de fecha {factura.FECHA:dd/MM/yyyy}.", nombreFactura, combinado, ct);
        logger.LogInformation("Factura {Factura} enviada a {Destinos}", etiqueta, string.Join("; ", destinos));
        return Ok(new ResultadoFacturaCorreo(etiqueta, string.Join("; ", destinos), true, albaranes.Count == 0 ? "Factura enviada." : $"Factura y {albaranes.Count} albarán(es) enviados en un único PDF."));
    }

    private async Task<byte[]> RegenerarAlbaran(AlbaranVentaSage50 a, Parametro p, CancellationToken ct)
    {
        var cliente = await sage.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Codigo == a.CLIENTE && c.Clienteerp == a.CLIENTEERP, ct)
            ?? await sage.Clientes.AsNoTracking().FirstAsync(c => c.Codigo == a.CLIENTE, ct);
        var lineas = await sage.LineasAlbaranesVenta.AsNoTracking().Where(l => l.EMPRESA == a.EMPRESA && l.NUMERO == a.NUMERO && l.LETRA == a.LETRA).OrderBy(l => l.LINIA).ToListAsync(ct);
        if (lineas.Count == 0) throw new InvalidOperationException("El albarán no tiene líneas.");
        var obra = string.IsNullOrWhiteSpace(a.OBRA) ? null : await comun.Obras.AsNoTracking().FirstOrDefaultAsync(o => o.Codigo.Trim() == a.OBRA.Trim(), ct);
        var iva = await sage.TipoIva.AsNoTracking().FirstOrDefaultAsync(i => i.Codigo.Trim() == lineas[0].TIPO_IVA.Trim(), ct);
        var solicitudes = await app.Solicitudes.AsNoTracking().Where(s => s.AlbaranSerieSage != null && s.AlbaranNumeroSage != null && s.AlbaranSerieSage.Trim() == a.LETRA.Trim() && s.AlbaranNumeroSage.Trim() == a.NUMERO.Trim()).ToListAsync(ct);
        var solicitud = solicitudes.FirstOrDefault();
        var firma = solicitud?.FirmaPath;
        if (string.IsNullOrWhiteSpace(firma) && solicitud is not null && !string.IsNullOrWhiteSpace(p.PathFirmas)) firma = Path.Combine(p.PathFirmas.Trim(), $"{solicitud.IdSolicitud}.png");
        var fotos = solicitud is null ? [] : await app.SolicitudFotos.AsNoTracking().Where(f => f.IdSolicitud == solicitud.IdSolicitud).OrderBy(f => f.FechaCreacion).Select(f => f.RutaArchivo).ToListAsync(ct);
        Camion? camion = null; Planta? planta = null;
        if (solicitud?.IdConductor is int conductor) { var id = await app.Operarios.Where(o => o.IdOperario == conductor).Select(o => o.IdCamion).FirstOrDefaultAsync(ct); if (id.HasValue) camion = await app.Camiones.AsNoTracking().FirstOrDefaultAsync(c => c.IdCamion == id, ct); }
        if (solicitud?.IdPlantaDescarga is int plantaId || solicitud?.IdPlantaOrigen is int plantaOrigen) planta = await app.Plantas.AsNoTracking().FirstOrDefaultAsync(x => x.IdPlanta == (solicitud!.IdPlantaDescarga ?? solicitud.IdPlantaOrigen), ct);
        var entregados = string.Join(", ", solicitudes.SelectMany(s => new[] { s.CodigoEntrega, s.CodigoAmbosEntrega }).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()).Distinct());
        var retirados = string.Join(", ", solicitudes.SelectMany(s => new[] { s.CodigoRecogida, s.CodigoAmbosRecogida }).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()).Distinct());
        return albaranPdf.Crear(p.Empresa, a, cliente, obra, lineas, iva, solicitud?.FirmaNombre, solicitud?.FirmaDni, firma, fotos, entregados, retirados, solicitud, camion, planta);
    }
}

public sealed record RutasFacturaCorreo(string OrigenFacturas, string DestinoDocumentos);
