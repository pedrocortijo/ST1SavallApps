using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.API.Services;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/solicitudes/{id:int}/tique-pesaje")]
public sealed class TiquesPesajeController(
    ApplicationDbContext context,
    TiquePesajeParser parser,
    AzureDocumentIntelligenceService azureDocumentIntelligence,
    ILogger<TiquesPesajeController> logger) : ControllerBase
{
    [HttpPost("analizar")]
    public async Task<ActionResult<TiquePesajeAnalisisResponse>> Analizar(
        int id, [FromBody] AnalizarTiquePesajeRequest request, CancellationToken cancellationToken)
    {
        var solicitud = await context.Solicitudes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.IdSolicitud == id, cancellationToken);
        if (solicitud is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.ImagenBase64))
            return BadRequest(new { message = "No se ha recibido la imagen del tique." });

        byte[] imagen;
        try
        {
            var base64 = request.ImagenBase64.Contains(',')
                ? request.ImagenBase64[(request.ImagenBase64.IndexOf(',') + 1)..]
                : request.ImagenBase64;
            imagen = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "El formato de la imagen del tique no es válido." });
        }

        if (imagen.Length == 0 || imagen.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "La imagen del tique debe tener un tamaño máximo de 10 MB." });
        var contentType = string.IsNullOrWhiteSpace(request.ContentType) ? "image/jpeg" : request.ContentType.Trim();
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "El archivo seleccionado debe ser una imagen." });

        var textoOcr = request.TextoOcr;

        try
        {
            textoOcr = await azureDocumentIntelligence.ExtraerTextoAsync(imagen, cancellationToken) ?? textoOcr;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Azure Document Intelligence no pudo leer el tique de la solicitud {SolicitudId}; se usará el OCR local.", id);
        }
        if (string.IsNullOrWhiteSpace(textoOcr))
            return UnprocessableEntity(new { message = "No se ha podido leer texto en el tique. Repita la foto con el tique completo, enfocado y bien iluminado." });

        var datos = parser.Extraer(textoOcr);
        var parametro = await context.Parametros.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var rutaBase = !string.IsNullOrWhiteSpace(parametro?.PathImagenes) ? parametro.PathImagenes : parametro?.PathFirmas;
        if (string.IsNullOrWhiteSpace(rutaBase))
            return BadRequest(new { message = "Configure la ruta de imágenes en Parámetros antes de analizar tiques." });

        string rutaArchivo;
        try
        {
            var carpeta = Path.Combine(rutaBase, id.ToString(), "TiquesPesaje");
            Directory.CreateDirectory(carpeta);
            var extension = contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg";
            rutaArchivo = Path.Combine(carpeta, $"{Guid.NewGuid():N}{extension}");
            await System.IO.File.WriteAllBytesAsync(rutaArchivo, imagen, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = $"No se ha podido guardar el tique en la ruta configurada: {ex.Message}" });
        }

        var tique = new SolicitudTiquePesaje
        {
            IdSolicitud = id,
            IdPlanta = solicitud.IdPlantaDescarga ?? solicitud.IdPlantaOrigen,
            RutaArchivo = rutaArchivo,
            NombreArchivo = request.NombreArchivo?.Trim(),
            TextoOcr = textoOcr.Trim(),
            NumeroAlbaran = datos.NumeroAlbaran,
            Matricula = datos.Matricula,
            Fecha = datos.Fecha,
            Hora = datos.Hora,
            PesoNetoKg = datos.PesoNetoKg,
            Confianza = request.ConfianzaOcr,
            FechaCreacion = DateTime.Now
        };
        context.SolicitudTiquesPesaje.Add(tique);
        await context.SaveChangesAsync(cancellationToken);
        return Ok(await CrearRespuestaAsync(solicitud, tique, cancellationToken));
    }

    [HttpPost("{idTique:int}/confirmar")]
    public async Task<ActionResult<TiquePesajeAnalisisResponse>> Confirmar(
        int id, int idTique, [FromBody] ConfirmarTiquePesajeRequest request, CancellationToken cancellationToken)
    {
        var solicitud = await context.Solicitudes.FirstOrDefaultAsync(s => s.IdSolicitud == id, cancellationToken);
        var tique = await context.SolicitudTiquesPesaje
            .FirstOrDefaultAsync(t => t.Id == idTique && t.IdSolicitud == id, cancellationToken);
        if (solicitud is null || tique is null) return NotFound();
        if (tique.Confirmado) return Conflict(new { message = "Este tique ya ha sido confirmado." });

        var numero = request.NumeroAlbaran?.Trim();
        var matricula = TiquePesajeParser.NormalizarMatricula(request.Matricula);
        if (string.IsNullOrWhiteSpace(numero))
            return BadRequest(new { message = "Revise e introduzca el número de albarán del tique." });
        if (numero.Length > 20)
            return BadRequest(new { message = "El número de albarán no puede superar 20 caracteres." });
        if (!request.Fecha.HasValue || !request.Hora.HasValue)
            return BadRequest(new { message = "Revise e introduzca la fecha y la hora del tique." });
        if (request.PesoNetoKg is null or <= 0 or > 100000)
            return BadRequest(new { message = "El peso neto debe estar comprendido entre 1 y 100.000 kg." });

        var idPlanta = solicitud.IdPlantaDescarga ?? solicitud.IdPlantaOrigen;
        if (await context.SolicitudTiquesPesaje.AsNoTracking().AnyAsync(t =>
            t.Id != idTique && t.Confirmado && t.IdPlanta == idPlanta && t.NumeroAlbaran == numero, cancellationToken))
            return Conflict(new { message = "Ya existe un tique confirmado con ese número de albarán para la misma planta." });

        tique.IdPlanta = idPlanta;
        tique.NumeroAlbaran = numero;
        tique.Matricula = string.IsNullOrWhiteSpace(matricula) ? null : matricula;
        tique.Fecha = request.Fecha.Value.Date;
        tique.Hora = request.Hora;
        tique.PesoNetoKg = request.PesoNetoKg;
        tique.Confirmado = true;
        tique.FechaConfirmacion = DateTime.Now;
        tique.UsuarioConfirmacion = User.Identity?.Name;

        solicitud.AlbaranPlanta = numero;
        solicitud.FechaPesaje = request.Fecha.Value.Date;
        solicitud.HoraPesaje = request.Hora;
        solicitud.KgAlbaran = request.PesoNetoKg;
        await context.SaveChangesAsync(cancellationToken);
        return Ok(await CrearRespuestaAsync(solicitud, tique, cancellationToken));
    }

    private async Task<TiquePesajeAnalisisResponse> CrearRespuestaAsync(
        Solicitud solicitud, SolicitudTiquePesaje tique, CancellationToken cancellationToken)
    {
        var respuesta = new TiquePesajeAnalisisResponse
        {
            IdTiquePesaje = tique.Id,
            NumeroAlbaran = tique.NumeroAlbaran,
            Matricula = tique.Matricula,
            Fecha = tique.Fecha,
            Hora = tique.Hora,
            PesoNetoKg = tique.PesoNetoKg,
            Confianza = tique.Confianza,
            TextoOcr = tique.TextoOcr
        };
        if (string.IsNullOrWhiteSpace(respuesta.NumeroAlbaran)) respuesta.Advertencias.Add("No se ha identificado el número de albarán.");
        if (!respuesta.Fecha.HasValue) respuesta.Advertencias.Add("No se ha identificado la fecha.");
        if (!respuesta.Hora.HasValue) respuesta.Advertencias.Add("No se ha identificado la hora.");
        if (!respuesta.PesoNetoKg.HasValue) respuesta.Advertencias.Add("No se ha identificado el peso neto.");
        if (string.IsNullOrWhiteSpace(respuesta.Matricula)) respuesta.Advertencias.Add("No se ha identificado la matrícula.");

        if (solicitud.IdConductor.HasValue && !string.IsNullOrWhiteSpace(respuesta.Matricula))
        {
            var asignada = await context.Operarios.AsNoTracking()
                .Where(o => o.IdOperario == solicitud.IdConductor.Value)
                .Select(o => o.Camion == null ? null : o.Camion.Matricula)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(asignada)
                && TiquePesajeParser.NormalizarMatricula(asignada) != TiquePesajeParser.NormalizarMatricula(respuesta.Matricula))
                respuesta.Advertencias.Add($"La matrícula leída ({respuesta.Matricula}) no coincide con la asignada ({asignada.Trim()}).");
        }

        if (respuesta.Fecha.HasValue && solicitud.FechaTarea.HasValue
            && Math.Abs((respuesta.Fecha.Value.Date - solicitud.FechaTarea.Value.Date).TotalDays) > 1)
            respuesta.Advertencias.Add("La fecha del tique no coincide con la fecha prevista del servicio.");

        var idPlanta = solicitud.IdPlantaDescarga ?? solicitud.IdPlantaOrigen;
        if (!string.IsNullOrWhiteSpace(respuesta.NumeroAlbaran)
            && await context.SolicitudTiquesPesaje.AsNoTracking().AnyAsync(t =>
                t.Id != respuesta.IdTiquePesaje && t.Confirmado && t.IdPlanta == idPlanta
                && t.NumeroAlbaran == respuesta.NumeroAlbaran, cancellationToken))
            respuesta.Advertencias.Add("El número de albarán ya está confirmado en otro servicio de la misma planta.");
        return respuesta;
    }
}
