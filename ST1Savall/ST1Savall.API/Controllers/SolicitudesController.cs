using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using ST1Savall.API.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SolicitudesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly PlanificacionService _planificacionService;
    private readonly CalculoRutaSolicitudService _calculoRutaService;
    private readonly ArticulosSage50Service _articulosSage50Service;

    public SolicitudesController(
        ApplicationDbContext context,
        PlanificacionService planificacionService,
        CalculoRutaSolicitudService calculoRutaService,
        ArticulosSage50Service articulosSage50Service)
    {
        _context = context;
        _planificacionService = planificacionService;
        _calculoRutaService = calculoRutaService;
        _articulosSage50Service = articulosSage50Service;
    }

    [HttpPost("calcular-ruta")]
    public async Task<ActionResult<CalculoRutaSolicitudResultado>> CalcularRuta(
        Solicitud solicitud,
        bool forzarActualizacion = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _calculoRutaService.CalcularYAplicarAsync(
                solicitud, forzarActualizacion, cancellationToken));
        }
        catch (ProveedorRutasException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    [HttpGet("siguiente-hueco")]
    public async Task<ActionResult<PlanificacionHueco>> GetSiguienteHueco(
        int idConductor,
        DateTime fecha,
        int duracionMinutos,
        int excluirSolicitudId = 0)
    {
        return Ok(await _planificacionService.BuscarSiguienteHuecoAsync(
            idConductor, fecha, duracionMinutos, excluirSolicitudId));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Solicitud>>> GetSolicitudes()
    {
        return await _context.Solicitudes.ToListAsync();
    }

    [HttpGet("con-contenedores")]
    public async Task<ActionResult<IEnumerable<Solicitud>>> GetSolicitudesConContenedores()
    {
        var solicitudes = await _context.Solicitudes
            .AsNoTracking()
            .Where(s => s.Estado != 6 && (
                !string.IsNullOrEmpty(s.CodigoEntrega) ||
                !string.IsNullOrEmpty(s.CodigoAmbosEntrega) ||
                !string.IsNullOrEmpty(s.CodigoRecogida) ||
                !string.IsNullOrEmpty(s.CodigoAmbosRecogida)))
            .ToListAsync();

        return Ok(solicitudes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Solicitud>> GetSolicitud(int id)
    {
        var solicitud = await _context.Solicitudes
            .FirstOrDefaultAsync(s => s.IdSolicitud == id);
            
        if (solicitud == null) return NotFound();
        return solicitud;
    }

    [HttpPost]
    public async Task<ActionResult<Solicitud>> PostSolicitud(Solicitud solicitud)
    {
        if (TieneContenedorEnEntregaYRetirada(solicitud))
            return BadRequest(new { message = "Un contenedor no puede entregarse y retirarse en la misma solicitud." });

        if (!solicitud.Prioridad.HasValue || solicitud.Prioridad == 0)
        {
            solicitud.Prioridad = 3; // Baja por defecto
        }
        solicitud.NotificacionInicioVisualizada = false;
        var errorRuta = await CalcularRutaAutomaticamenteAsync(solicitud);
        if (errorRuta != null) return errorRuta;

        var errorPlanificacion = await _planificacionService.PrepararYValidarAsync(solicitud);
        if (errorPlanificacion != null)
            return Conflict(new { message = errorPlanificacion });

        var parametro = await _context.Parametros.AsNoTracking().FirstOrDefaultAsync();
        var validEstadoIds = (await _context.EstadosSolicitud.Select(e => e.IdEstado).ToListAsync()).ToHashSet();
        SolicitudEstadoEvaluator.EvaluarYAplicarEstado(solicitud, parametro, validEstadoIds);
        if (validEstadoIds.Count > 0 && !validEstadoIds.Contains(solicitud.Estado))
        {
            solicitud.Estado = validEstadoIds.First();
        }

        _context.Solicitudes.Add(solicitud);
        await ActualizarEstadosContenedores(solicitud);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSolicitud), new { id = solicitud.IdSolicitud }, solicitud);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutSolicitud(int id, Solicitud solicitud)
    {
        if (id != solicitud.IdSolicitud) return BadRequest();
        if (TieneContenedorEnEntregaYRetirada(solicitud))
            return BadRequest(new { message = "Un contenedor no puede entregarse y retirarse en la misma solicitud." });

        var solicitudAnterior = await _context.Solicitudes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.IdSolicitud == id);
        if (solicitudAnterior == null) return NotFound();

        var parametro = await _context.Parametros.AsNoTracking().FirstOrDefaultAsync();
        var idEstadoFinalizadoPorDescripcion = await _context.EstadosSolicitud
            .AsNoTracking()
            .Where(e => e.Descripcion.Contains("Finalizado"))
            .Select(e => (int?)e.IdEstado)
            .FirstOrDefaultAsync();
        var idFinalizado = idEstadoFinalizadoPorDescripcion ?? parametro?.EstadoFinalizado ?? 5;
        if (solicitudAnterior.Estado == idFinalizado)
        {
            if (!Request.Headers.TryGetValue("X-Finalized-Service-Password", out var password)
                || string.IsNullOrWhiteSpace(password))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "La solicitud está finalizada. Debe confirmar su contraseña para modificarla." });

            var adminPassword = parametro?.AdminPassword;
            var isAdminPassValid = !string.IsNullOrEmpty(adminPassword) && password == adminPassword;

            if (!isAdminPassValid)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "La contraseña indicada no es correcta." });
        }

        if (solicitudAnterior.FechaHoraInicioPlanificada != solicitud.FechaHoraInicioPlanificada)
            solicitud.NotificacionInicioVisualizada = false;

        if (!string.Equals(solicitudAnterior.ObservacionesConductor, solicitud.ObservacionesConductor, StringComparison.Ordinal))
            solicitud.NotificacionInicioVisualizada = false;

        // La ruta de un servicio finalizado es histórica. Al editarlo no se debe
        // consultar Mapbox ni sustituir sus datos de planificación.
        if (solicitudAnterior.Estado != idFinalizado && solicitud.Estado != idFinalizado)
        {
            var errorRuta = await CalcularRutaAutomaticamenteAsync(solicitud);
            if (errorRuta != null) return errorRuta;
        }

        var errorPlanificacion = await _planificacionService.PrepararYValidarAsync(solicitud);
        if (errorPlanificacion != null)
            return Conflict(new { message = errorPlanificacion });

        var validEstadoIds = (await _context.EstadosSolicitud.Select(e => e.IdEstado).ToListAsync()).ToHashSet();
        var idAdjudicado = parametro?.EstadoAdjudicado ?? 9;
        int idNoSeguir = 4;

        if (solicitudAnterior.Estado == idAdjudicado && solicitud.Estado != idAdjudicado && solicitud.Estado != idNoSeguir)
        {
            solicitud.FechaPrevista = null;
            solicitud.FechaTarea = null;
            solicitud.FechaHoraInicioPlanificada = null;
            solicitud.FechaHoraFinPlanificada = null;
        }

        SolicitudEstadoEvaluator.EvaluarYAplicarEstado(solicitud, parametro, validEstadoIds);
        if (validEstadoIds.Count > 0 && !validEstadoIds.Contains(solicitud.Estado))
        {
            solicitud.Estado = validEstadoIds.First();
        }

        _context.Entry(solicitud).State = EntityState.Modified;
        await RestaurarEstadosContenedoresEliminados(solicitudAnterior, solicitud);
        await ActualizarEstadosContenedores(solicitud);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SolicitudExists(id)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpPost("marcar-notificaciones-inicio-visualizadas")]
    public async Task<IActionResult> MarcarNotificacionesInicioVisualizadas([FromBody] List<int> idsSolicitudes)
    {
        if (idsSolicitudes.Count == 0) return NoContent();

        var solicitudes = await _context.Solicitudes
            .Where(s => idsSolicitudes.Contains(s.IdSolicitud))
            .ToListAsync();
        foreach (var solicitud in solicitudes)
            solicitud.NotificacionInicioVisualizada = true;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/iniciar")]
    public async Task<IActionResult> IniciarSolicitud(int id)
    {
        var solicitud = await _context.Solicitudes.FindAsync(id);
        if (solicitud == null) return NotFound();

        var parametro = await _context.Parametros.AsNoTracking().FirstOrDefaultAsync();
        var estadoIniciado = parametro?.EstadoIniciado
            ?? await _context.EstadosSolicitud.AsNoTracking()
                .Where(e => e.Descripcion.Contains("iniciado"))
                .Select(e => (int?)e.IdEstado)
                .FirstOrDefaultAsync();

        if (!estadoIniciado.HasValue)
            return BadRequest(new { message = "No hay un estado de servicio iniciado configurado." });

        solicitud.Estado = estadoIniciado.Value;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/cancelar-inicio")]
    public async Task<IActionResult> CancelarInicioSolicitud(int id)
    {
        var solicitud = await _context.Solicitudes.FindAsync(id);
        if (solicitud == null) return NotFound();

        var parametro = await _context.Parametros.AsNoTracking().FirstOrDefaultAsync();
        var estadoIniciado = parametro?.EstadoIniciado
            ?? await _context.EstadosSolicitud.AsNoTracking()
                .Where(e => e.Descripcion.Contains("iniciado"))
                .Select(e => (int?)e.IdEstado)
                .FirstOrDefaultAsync();
        var estadoAsignado = parametro?.EstadoAdjudicado
            ?? await _context.EstadosSolicitud.AsNoTracking()
                .Where(e => e.Descripcion.Contains("asignad") || e.Descripcion.Contains("adjudicad"))
                .Select(e => (int?)e.IdEstado)
                .FirstOrDefaultAsync();

        if (!estadoIniciado.HasValue || !estadoAsignado.HasValue)
            return BadRequest(new { message = "No hay estados de servicio iniciado y asignado configurados." });
        if (solicitud.Estado != estadoIniciado.Value)
            return Conflict(new { message = "Solo se puede cancelar el inicio de un servicio iniciado." });

        solicitud.Estado = estadoAsignado.Value;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/finalizar")]
    public async Task<IActionResult> FinalizarSolicitud(int id)
    {
        var solicitud = await _context.Solicitudes.FindAsync(id);
        if (solicitud == null) return NotFound();

        var parametro = await _context.Parametros.AsNoTracking().FirstOrDefaultAsync();
        var estadoIniciado = parametro?.EstadoIniciado
            ?? await _context.EstadosSolicitud.AsNoTracking()
                .Where(e => e.Descripcion.Contains("iniciado"))
                .Select(e => (int?)e.IdEstado)
                .FirstOrDefaultAsync();
        var estadoFinalizado = parametro?.EstadoFinalizado
            ?? await _context.EstadosSolicitud.AsNoTracking()
                .Where(e => e.Descripcion.Contains("finalizado"))
                .Select(e => (int?)e.IdEstado)
                .FirstOrDefaultAsync();

        if (!estadoIniciado.HasValue || !estadoFinalizado.HasValue)
            return BadRequest(new { message = "No hay estados de servicio iniciado y finalizado configurados." });
        if (solicitud.Estado != estadoIniciado.Value)
            return Conflict(new { message = "Solo se puede finalizar un servicio iniciado." });
        var datosPendientes = new List<string>();
        if (string.IsNullOrWhiteSpace(solicitud.FirmaPath) || !System.IO.File.Exists(solicitud.FirmaPath))
            datosPendientes.Add("Firma");
        if (string.IsNullOrWhiteSpace(solicitud.FirmaNombre))
            datosPendientes.Add("Nombre del firmante");
        if (string.IsNullOrWhiteSpace(solicitud.FirmaDni))
            datosPendientes.Add("DNI del firmante");
        if (string.IsNullOrWhiteSpace(solicitud.TipoResiduo))
            datosPendientes.Add("Tipo de residuo");
        else if (!await _articulosSage50Service.EsArticuloContenedorAsync(solicitud.TipoResiduo))
            return BadRequest(new { message = "El tipo de residuo debe ser un código de artículo válido de Sage 50." });
        if (string.IsNullOrWhiteSpace(solicitud.AlbaranPlanta))
            datosPendientes.Add("número de albarán de planta");
        var tarea = await _context.Tareas.AsNoTracking().FirstOrDefaultAsync(t => t.IdTarea == solicitud.IdTipoTarea);
        if (tarea == null)
            return BadRequest(new { message = "No se ha encontrado el tipo de tarea del servicio." });

        if (tarea.Entrega1 && string.IsNullOrWhiteSpace(solicitud.CodigoEntrega)) datosPendientes.Add("número de serie de Entrega [1]");
        if (tarea.Entrega2 && string.IsNullOrWhiteSpace(solicitud.CodigoAmbosEntrega)) datosPendientes.Add("número de serie de Entrega [2]");
        if (tarea.Recoger1 && string.IsNullOrWhiteSpace(solicitud.CodigoRecogida)) datosPendientes.Add("número de serie de Retirada [1]");
        if (tarea.Recoger2 && string.IsNullOrWhiteSpace(solicitud.CodigoAmbosRecogida)) datosPendientes.Add("número de serie de Retirada [2]");
        if (datosPendientes.Count > 0)
            return BadRequest(new { message = $"Debe cumplimentar:{Environment.NewLine}{string.Join(Environment.NewLine, datosPendientes)}." });

        solicitud.Estado = estadoFinalizado.Value;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/cancelar-finalizacion")]
    public async Task<IActionResult> CancelarFinalizacionSolicitud(int id)
    {
        var solicitud = await _context.Solicitudes.FindAsync(id);
        if (solicitud == null) return NotFound();

        var parametro = await _context.Parametros.AsNoTracking().FirstOrDefaultAsync();
        var estadoIniciado = parametro?.EstadoIniciado
            ?? await _context.EstadosSolicitud.AsNoTracking()
                .Where(e => e.Descripcion.Contains("iniciado"))
                .Select(e => (int?)e.IdEstado)
                .FirstOrDefaultAsync();
        var estadoFinalizado = parametro?.EstadoFinalizado
            ?? await _context.EstadosSolicitud.AsNoTracking()
                .Where(e => e.Descripcion.Contains("finalizado"))
                .Select(e => (int?)e.IdEstado)
                .FirstOrDefaultAsync();

        if (!estadoIniciado.HasValue || !estadoFinalizado.HasValue)
            return BadRequest(new { message = "No hay estados de servicio iniciado y finalizado configurados." });
        if (solicitud.Estado != estadoFinalizado.Value)
            return Conflict(new { message = "Solo se puede cancelar la finalización de un servicio finalizado." });

        solicitud.Estado = estadoIniciado.Value;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}/datos-firma")]
    public async Task<IActionResult> ActualizarDatosFirma(int id, [FromBody] DatosFirmaSolicitudRequest datos)
    {
        var solicitud = await _context.Solicitudes.FindAsync(id);
        if (solicitud == null) return NotFound();

        var solicitudAnterior = new Solicitud
        {
            CodigoEntrega = solicitud.CodigoEntrega,
            CodigoAmbosEntrega = solicitud.CodigoAmbosEntrega,
            CodigoRecogida = solicitud.CodigoRecogida,
            CodigoAmbosRecogida = solicitud.CodigoAmbosRecogida
        };

        solicitud.AlbaranPlanta = datos.AlbaranPlanta?.Trim();
        solicitud.TipoResiduo = datos.TipoResiduo?.Trim();
        if (!string.IsNullOrWhiteSpace(solicitud.TipoResiduo)
            && !await _articulosSage50Service.EsArticuloContenedorAsync(solicitud.TipoResiduo))
            return BadRequest(new { message = "El tipo de residuo seleccionado no corresponde a un artículo válido de Sage 50." });
        solicitud.FirmaNombre = datos.FirmaNombre?.Trim();
        solicitud.FirmaDni = datos.FirmaDni?.Trim();
        solicitud.CodigoEntrega = datos.CodigoEntrega?.Trim();
        solicitud.CodigoAmbosEntrega = datos.CodigoAmbosEntrega?.Trim();
        solicitud.CodigoRecogida = datos.CodigoRecogida?.Trim();
        solicitud.CodigoAmbosRecogida = datos.CodigoAmbosRecogida?.Trim();
        await RestaurarEstadosContenedoresEliminados(solicitudAnterior, solicitud);
        await ActualizarEstadosContenedores(solicitud);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSolicitud(int id)
    {
        var solicitud = await _context.Solicitudes.FindAsync(id);
        if (solicitud == null) return NotFound();
        _context.Solicitudes.Remove(solicitud);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/firma")]
    public async Task<IActionResult> GuardarFirma(int id, [FromBody] FirmaSolicitudRequest firma)
    {
        var solicitud = await _context.Solicitudes.FindAsync(id);
        if (solicitud == null) return NotFound();

        if (string.IsNullOrWhiteSpace(firma.ImagenBase64))
            return BadRequest(new { message = "No se ha recibido la imagen de la firma." });

        var pathFirmas = await _context.Parametros.AsNoTracking()
            .Select(p => p.PathFirmas)
            .FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(pathFirmas))
            return BadRequest(new { message = "Configure la ruta de firmas en Parámetros antes de guardar una firma." });

        byte[] imagen;
        try
        {
            var contenidoBase64 = firma.ImagenBase64.Contains(',')
                ? firma.ImagenBase64[(firma.ImagenBase64.IndexOf(',') + 1)..]
                : firma.ImagenBase64;
            imagen = Convert.FromBase64String(contenidoBase64);
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "El formato de la imagen de firma no es válido." });
        }

        try
        {
            Directory.CreateDirectory(pathFirmas);
            var rutaFirma = Path.Combine(pathFirmas, $"{solicitud.IdSolicitud}.png");
            await System.IO.File.WriteAllBytesAsync(rutaFirma, imagen);

            solicitud.FirmaNombre = firma.Nombre?.Trim();
            solicitud.FirmaDni = firma.Dni?.Trim();
            solicitud.FirmaPath = rutaFirma;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = $"No se ha podido guardar la firma en la ruta configurada: {ex.Message}" });
        }
    }

    [HttpGet("{id}/firma")]
    public async Task<IActionResult> ObtenerFirma(int id)
    {
        var firmaPath = await _context.Solicitudes.AsNoTracking()
            .Where(s => s.IdSolicitud == id)
            .Select(s => s.FirmaPath)
            .FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(firmaPath) || !System.IO.File.Exists(firmaPath)) return NotFound();

        return File(await System.IO.File.ReadAllBytesAsync(firmaPath), "image/png");
    }

    [HttpDelete("{id}/firma")]
    public async Task<IActionResult> EliminarFirma(int id)
    {
        var solicitud = await _context.Solicitudes.FindAsync(id);
        if (solicitud == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(solicitud.FirmaPath) && System.IO.File.Exists(solicitud.FirmaPath))
            System.IO.File.Delete(solicitud.FirmaPath);

        solicitud.FirmaNombre = null;
        solicitud.FirmaDni = null;
        solicitud.FirmaPath = null;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/fotos")]
    public async Task<ActionResult<IEnumerable<SolicitudFoto>>> ObtenerFotos(int id)
    {
        if (!await _context.Solicitudes.AnyAsync(s => s.IdSolicitud == id)) return NotFound();
        return await _context.SolicitudFotos.AsNoTracking()
            .Where(f => f.IdSolicitud == id)
            .OrderByDescending(f => f.FechaCreacion)
            .ToListAsync();
    }

    [HttpGet("{id}/fotos/{idFoto}")]
    public async Task<IActionResult> ObtenerFoto(int id, int idFoto)
    {
        var foto = await _context.SolicitudFotos.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == idFoto && f.IdSolicitud == id);
        if (foto == null || !System.IO.File.Exists(foto.RutaArchivo)) return NotFound();

        return File(await System.IO.File.ReadAllBytesAsync(foto.RutaArchivo), "image/jpeg");
    }

    [HttpGet("{id}/fotos/{idFoto}/miniatura")]
    public async Task<IActionResult> ObtenerMiniaturaFoto(int id, int idFoto)
    {
        var foto = await _context.SolicitudFotos.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == idFoto && f.IdSolicitud == id);
        if (foto == null || !System.IO.File.Exists(foto.RutaArchivo)) return NotFound();

        await using var input = System.IO.File.OpenRead(foto.RutaArchivo);
        using var imagen = await Image.LoadAsync(input);
        imagen.Mutate(context => context.Resize(new ResizeOptions
        {
            Size = new Size(360, 360),
            Mode = ResizeMode.Max
        }));

        await using var output = new MemoryStream();
        await imagen.SaveAsJpegAsync(output, new JpegEncoder { Quality = 75 });
        return File(output.ToArray(), "image/jpeg");
    }

    [HttpPost("{id}/fotos")]
    public async Task<ActionResult<SolicitudFoto>> GuardarFoto(int id, [FromBody] FotoSolicitudRequest foto)
    {
        if (!await _context.Solicitudes.AnyAsync(s => s.IdSolicitud == id)) return NotFound();
        if (string.IsNullOrWhiteSpace(foto.ImagenBase64))
            return BadRequest(new { message = "No se ha recibido ninguna foto." });

        byte[] imagen;
        try
        {
            var contenidoBase64 = foto.ImagenBase64.Contains(',')
                ? foto.ImagenBase64[(foto.ImagenBase64.IndexOf(',') + 1)..]
                : foto.ImagenBase64;
            imagen = Convert.FromBase64String(contenidoBase64);
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "El formato de la foto no es válido." });
        }

        if (imagen.Length == 0 || imagen.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "La foto debe tener un tamaño máximo de 10 MB." });

        var pathFirmas = await _context.Parametros.AsNoTracking().Select(p => p.PathFirmas).FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(pathFirmas))
            return BadRequest(new { message = "Configure la ruta de firmas en Parámetros antes de guardar fotos." });

        var carpetaFotos = Path.Combine(pathFirmas, "Fotos", id.ToString());
        Directory.CreateDirectory(carpetaFotos);
        var rutaArchivo = Path.Combine(carpetaFotos, $"{Guid.NewGuid():N}.jpg");
        await System.IO.File.WriteAllBytesAsync(rutaArchivo, imagen);

        var nuevaFoto = new SolicitudFoto
        {
            IdSolicitud = id,
            RutaArchivo = rutaArchivo,
            NombreArchivo = foto.NombreArchivo?.Trim(),
            FechaCreacion = DateTime.Now
        };
        _context.SolicitudFotos.Add(nuevaFoto);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(ObtenerFoto), new { id, idFoto = nuevaFoto.Id }, nuevaFoto);
    }

    [HttpDelete("{id}/fotos/{idFoto}")]
    public async Task<IActionResult> EliminarFoto(int id, int idFoto)
    {
        var foto = await _context.SolicitudFotos.FirstOrDefaultAsync(f => f.Id == idFoto && f.IdSolicitud == id);
        if (foto == null) return NotFound();

        if (System.IO.File.Exists(foto.RutaArchivo)) System.IO.File.Delete(foto.RutaArchivo);
        _context.SolicitudFotos.Remove(foto);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task ActualizarEstadosContenedores(Solicitud solicitud)
    {
        if (!string.IsNullOrEmpty(solicitud.CodigoEntrega))
        {
            var contenedorEntrega = await _context.Contenedores
                .FirstOrDefaultAsync(c => c.NumSerie == solicitud.CodigoEntrega);
            if (contenedorEntrega != null)
            {
                contenedorEntrega.EstadoFisico = "Entregado";
                _context.Entry(contenedorEntrega).State = EntityState.Modified;
            }
        }

        if (!string.IsNullOrEmpty(solicitud.CodigoRecogida))
        {
            var contenedorRecogida = await _context.Contenedores
                .FirstOrDefaultAsync(c => c.NumSerie == solicitud.CodigoRecogida);
            if (contenedorRecogida != null)
            {
                contenedorRecogida.EstadoFisico = "Disponible";
                _context.Entry(contenedorRecogida).State = EntityState.Modified;
            }
        }

        if (!string.IsNullOrEmpty(solicitud.CodigoAmbosEntrega))
        {
            var contenedorEntrega = await _context.Contenedores
                .FirstOrDefaultAsync(c => c.NumSerie == solicitud.CodigoAmbosEntrega);
            if (contenedorEntrega != null)
            {
                contenedorEntrega.EstadoFisico = "Entregado";
                _context.Entry(contenedorEntrega).State = EntityState.Modified;
            }
        }

        if (!string.IsNullOrEmpty(solicitud.CodigoAmbosRecogida))
        {
            var contenedorRecogida = await _context.Contenedores
                .FirstOrDefaultAsync(c => c.NumSerie == solicitud.CodigoAmbosRecogida);
            if (contenedorRecogida != null)
            {
                contenedorRecogida.EstadoFisico = "Disponible";
                _context.Entry(contenedorRecogida).State = EntityState.Modified;
            }
        }
    }

    public sealed class FirmaSolicitudRequest
    {
        public string? Nombre { get; init; }
        public string? Dni { get; init; }
        public string? ImagenBase64 { get; init; }
    }

    public sealed class DatosFirmaSolicitudRequest
    {
        public string? AlbaranPlanta { get; init; }
        public string? TipoResiduo { get; init; }
        public string? FirmaNombre { get; init; }
        public string? FirmaDni { get; init; }
        public string? CodigoEntrega { get; init; }
        public string? CodigoAmbosEntrega { get; init; }
        public string? CodigoRecogida { get; init; }
        public string? CodigoAmbosRecogida { get; init; }
    }

    public sealed class FotoSolicitudRequest
    {
        public string? NombreArchivo { get; init; }
        public string? ImagenBase64 { get; init; }
    }

    private async Task RestaurarEstadosContenedoresEliminados(Solicitud solicitudAnterior, Solicitud solicitudActual)
    {
        if (!string.IsNullOrWhiteSpace(solicitudAnterior.CodigoEntrega)
            && !string.Equals(solicitudAnterior.CodigoEntrega, solicitudActual.CodigoEntrega, StringComparison.OrdinalIgnoreCase))
        {
            var contenedorEntrega = await _context.Contenedores
                .FirstOrDefaultAsync(c => c.NumSerie == solicitudAnterior.CodigoEntrega);
            if (contenedorEntrega != null)
            {
                contenedorEntrega.EstadoFisico = "Disponible";
            }
        }

        if (!string.IsNullOrWhiteSpace(solicitudAnterior.CodigoRecogida)
            && !string.Equals(solicitudAnterior.CodigoRecogida, solicitudActual.CodigoRecogida, StringComparison.OrdinalIgnoreCase))
        {
            var contenedorRecogida = await _context.Contenedores
                .FirstOrDefaultAsync(c => c.NumSerie == solicitudAnterior.CodigoRecogida);
            if (contenedorRecogida != null)
            {
                contenedorRecogida.EstadoFisico = "Entregado";
            }
        }

        if (!string.IsNullOrWhiteSpace(solicitudAnterior.CodigoAmbosEntrega)
            && !string.Equals(solicitudAnterior.CodigoAmbosEntrega, solicitudActual.CodigoAmbosEntrega, StringComparison.OrdinalIgnoreCase))
        {
            var contenedorEntrega = await _context.Contenedores
                .FirstOrDefaultAsync(c => c.NumSerie == solicitudAnterior.CodigoAmbosEntrega);
            if (contenedorEntrega != null)
            {
                contenedorEntrega.EstadoFisico = "Disponible";
            }
        }

        if (!string.IsNullOrWhiteSpace(solicitudAnterior.CodigoAmbosRecogida)
            && !string.Equals(solicitudAnterior.CodigoAmbosRecogida, solicitudActual.CodigoAmbosRecogida, StringComparison.OrdinalIgnoreCase))
        {
            var contenedorRecogida = await _context.Contenedores
                .FirstOrDefaultAsync(c => c.NumSerie == solicitudAnterior.CodigoAmbosRecogida);
            if (contenedorRecogida != null)
            {
                contenedorRecogida.EstadoFisico = "Entregado";
            }
        }
    }

    private bool SolicitudExists(int id)
    {
        return _context.Solicitudes.Any(e => e.IdSolicitud == id);
    }

    private static bool TieneContenedorEnEntregaYRetirada(Solicitud solicitud)
    {
        var entregas = new[] { solicitud.CodigoEntrega, solicitud.CodigoAmbosEntrega }
            .Where(codigo => !string.IsNullOrWhiteSpace(codigo))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return entregas.Overlaps(new[] { solicitud.CodigoRecogida, solicitud.CodigoAmbosRecogida }
            .Where(codigo => !string.IsNullOrWhiteSpace(codigo)));
    }

    private async Task<ObjectResult?> CalcularRutaAutomaticamenteAsync(Solicitud solicitud)
    {
        if (!CalculoRutaSolicitudService.TieneDatosCompletos(solicitud))
            return null;

        try
        {
            await _calculoRutaService.CalcularYAplicarAsync(solicitud);
            return null;
        }
        catch (ProveedorRutasException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }
}
