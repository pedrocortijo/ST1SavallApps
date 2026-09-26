using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST1Savall.API.Services;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/deca")]
public class DeCaController(DeCaEmisionService deCaService) : ControllerBase
{
    /// <summary>
    /// Endpoint público sin autenticación exigido por la normativa DeCA para que los agentes de transporte
    /// y Guardia Civil descarguen directamente el PDF nativo al escanear el código QR en carretera.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("publico/descarga/{guid}")]
    public async Task<IActionResult> DescargarPdfPublico(string guid)
    {
        var archivo = await deCaService.ObtenerPdfPorGuidAsync(guid);
        if (archivo is null)
        {
            return NotFound("El documento DeCA solicitado no existe o no ha sido emitido.");
        }

        // Devolver inline para visualización directa en el navegador o visor PDF del móvil/tablet del agente
        return File(archivo.Value.Contenido, "application/pdf", archivo.Value.NombreArchivo);
    }

    /// <summary>
    /// Obtiene los datos detallados del documento DeCA para visualización en la aplicación.
    /// </summary>
    [HttpGet("solicitud/{idSolicitud}")]
    public async Task<ActionResult<DeCaDetalleDto>> ObtenerDetalle(int idSolicitud)
    {
        var urlBase = $"{Request.Scheme}://{Request.Host}";
        var detalle = await deCaService.ObtenerDetalleDeCaAsync(idSolicitud, urlBase);
        if (detalle is null)
        {
            return NotFound($"No se ha encontrado la solicitud #{idSolicitud}.");
        }

        return Ok(detalle);
    }

    /// <summary>
    /// Emite o regenera manualmente el documento DeCA para una solicitud.
    /// </summary>
    [HttpPost("solicitud/{idSolicitud}/emitir")]
    public async Task<ActionResult<GenerarDeCaResultadoDto>> EmitirDeCa(int idSolicitud, [FromQuery] string tipo = "RETIRADA")
    {
        var urlBase = $"{Request.Scheme}://{Request.Host}";
        var resultado = await deCaService.EmitirDeCaAsync(idSolicitud, tipo, urlBase);
        if (!resultado.Exito)
        {
            return BadRequest(resultado);
        }

        return Ok(resultado);
    }

    /// <summary>
    /// Descarga el PDF del DeCA para usuarios autenticados de la aplicación.
    /// </summary>
    [HttpGet("solicitud/{idSolicitud}/pdf")]
    public async Task<IActionResult> DescargarPdfAutenticado(int idSolicitud)
    {
        var urlBase = $"{Request.Scheme}://{Request.Host}";
        var detalle = await deCaService.ObtenerDetalleDeCaAsync(idSolicitud, urlBase);
        if (detalle is null || string.IsNullOrWhiteSpace(detalle.GuidDeCa))
        {
            return NotFound("La solicitud no tiene un documento DeCA emitido.");
        }

        var archivo = await deCaService.ObtenerPdfPorGuidAsync(detalle.GuidDeCa);
        if (archivo is null)
        {
            return NotFound("No se ha podido recuperar el archivo PDF del DeCA.");
        }

        return File(archivo.Value.Contenido, "application/pdf", archivo.Value.NombreArchivo);
    }

    /// <summary>
    /// Devuelve la imagen PNG del código QR de inspección de la solicitud.
    /// </summary>
    [HttpGet("solicitud/{idSolicitud}/qr-imagen")]
    public async Task<IActionResult> ObtenerQrImagen(int idSolicitud)
    {
        var urlBase = $"{Request.Scheme}://{Request.Host}";
        var detalle = await deCaService.ObtenerDetalleDeCaAsync(idSolicitud, urlBase);
        if (detalle is null || string.IsNullOrWhiteSpace(detalle.UrlDescarga))
        {
            return NotFound("No hay DeCA emitido para esta solicitud.");
        }

        var pngBytes = deCaService.GenerarQrPng(detalle.UrlDescarga);
        return File(pngBytes, "image/png");
    }
}
