using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.API.Services;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/wialon-tracking")]
public sealed class WialonTrackingController(ApplicationDbContext context, WialonTrackingService tracking, MapboxDirectionsService mapboxDirections) : ControllerBase
{
    private const string DefaultWialonUrl = "https://hosting.wialon.com/?lang=es";
    private static readonly TimeZoneInfo ZonaHorariaWialon = ObtenerZonaHorariaWialon();

    private static TimeZoneInfo ObtenerZonaHorariaWialon()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
        }
    }

    private static DateTime? ConvertirAHoraWialon(DateTime? fechaUtc) => fechaUtc.HasValue
        ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(fechaUtc.Value, DateTimeKind.Utc), ZonaHorariaWialon)
        : null;

    private static string ObtenerEstadoUnidad(WialonUnitPositionResult posicion)
    {
        if (!string.IsNullOrWhiteSpace(posicion.Error)) return "Sin conexión";
        if (posicion.Posicion is null || !posicion.Posicion.FechaUtc.HasValue) return "Sin datos";
        if (DateTime.UtcNow - posicion.Posicion.FechaUtc.Value > TimeSpan.FromMinutes(15)) return "Sin conexión";
        return posicion.Posicion.Velocidad is > 1d ? "Moviéndose" : "Parado";
    }

    [HttpGet("portal-url")]
    public async Task<ActionResult<string>> GetPortalUrl(CancellationToken cancellationToken)
    {
        var url = await context.Parametros.AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => p.WialonUrl)
            .FirstOrDefaultAsync(cancellationToken);
        return Ok(string.IsNullOrWhiteSpace(url) ? DefaultWialonUrl : url.Trim());
    }

    [HttpGet("ubicaciones")]
    public async Task<ActionResult<IEnumerable<UbicacionCamionWialon>>> GetUbicaciones(int tipoRuta = 1, CancellationToken cancellationToken = default)
    {
        var recorridoCompleto = tipoRuta == 2;

        if (!await tracking.ConfiguradoAsync(cancellationToken))
            return Problem("Wialon no está configurado en Parámetros.", statusCode: StatusCodes.Status503ServiceUnavailable);

        var camiones = await context.Camiones.AsNoTracking()
            .Where(c => c.Activo && !string.IsNullOrWhiteSpace(c.UnidadWialonId))
            .OrderBy(c => c.Matricula)
            .ToListAsync(cancellationToken);

        var parametros = await context.Parametros.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var estadosFinalizados = await context.EstadosSolicitud.AsNoTracking()
            .Where(e => e.Descripcion.Contains("finalizado"))
            .Select(e => e.IdEstado)
            .ToListAsync(cancellationToken);
        if (parametros?.EstadoFinalizado is int estadoFinalizado && estadoFinalizado > 0 && !estadosFinalizados.Contains(estadoFinalizado))
            estadosFinalizados.Add(estadoFinalizado);

        var estadosAnulados = await context.EstadosSolicitud.AsNoTracking()
            .Where(e => e.Descripcion.Contains("anulad") || e.Descripcion.Contains("reprogram"))
            .Select(e => e.IdEstado)
            .ToListAsync(cancellationToken);
        if (parametros?.EstadoReprogramacion is int estadoAnulado && estadoAnulado > 0 && !estadosAnulados.Contains(estadoAnulado))
            estadosAnulados.Add(estadoAnulado);

        var estadoIniciado = parametros?.EstadoIniciado ?? 8;
        var duracionOperacionPorDefecto = Math.Max(0, parametros?.DuracionOperacionServicioMinutos ?? 20);
        var idsCamion = camiones.Select(c => c.IdCamion).ToList();
        var conductoresPorCamion = await context.Operarios.AsNoTracking()
            .Where(o => o.IdCamion.HasValue && idsCamion.Contains(o.IdCamion.Value))
            .Select(o => new { o.IdCamion, o.IdOperario, o.Nombre })
            .ToDictionaryAsync(o => o.IdCamion!.Value, o => o, cancellationToken);
        var idsConductores = conductoresPorCamion.Values.Select(o => o.IdOperario).Distinct().ToList();
        var serviciosPorConductor = await context.Solicitudes.AsNoTracking()
            .Where(s => s.IdConductor.HasValue && idsConductores.Contains(s.IdConductor.Value)
                && !estadosFinalizados.Contains(s.Estado) && !estadosAnulados.Contains(s.Estado))
            .OrderBy(s => s.Estado == estadoIniciado ? 0 : 1)
            .ThenBy(s => s.FechaHoraInicioPlanificada ?? s.FechaTarea ?? s.FechaSolicitud)
            .Select(s => new
            {
                s.IdSolicitud,
                s.IdConductor,
                s.NombreObra,
                s.FirmaNombre,
                s.FirmaDni,
                s.FirmaPath,
                s.IdPlantaRegreso,
                LatitudDestino = s.Latitud ?? s.LatitudObra,
                LongitudDestino = s.Longitud ?? s.LongitudObra,
                s.DuracionOperacionMinutos,
                s.DistanciaOrigenObraMetros,
                s.MinutosOrigenObra,
                s.DistanciaObraDescargaMetros,
                s.MinutosObraDescarga,
                s.DistanciaDescargaRegresoMetros,
                s.MinutosDescargaRegreso,
                s.DistanciaTotalMetros,
                s.DuracionViajeMinutos
            })
            .ToListAsync(cancellationToken);
        var servicioActualPorConductor = serviciosPorConductor
            .GroupBy(s => s.IdConductor!.Value)
            .ToDictionary(g => g.Key, g => g.First());
        var idsCentralesRegreso = serviciosPorConductor
            .Where(s => s.IdPlantaRegreso.HasValue)
            .Select(s => s.IdPlantaRegreso!.Value)
            .Distinct()
            .ToList();
        var centralesRegreso = await context.Plantas.AsNoTracking()
            .Where(p => idsCentralesRegreso.Contains(p.IdPlanta))
            .Select(p => new { p.IdPlanta, p.Nombre, p.Latitud, p.Longitud })
            .ToDictionaryAsync(p => p.IdPlanta, cancellationToken);

        var result = new List<UbicacionCamionWialon>();
        foreach (var camion in camiones)
        {
            var conductorAsignado = conductoresPorCamion.TryGetValue(camion.IdCamion, out var conductor) ? conductor : null;
            var servicioAsignado = conductorAsignado is not null
                && servicioActualPorConductor.TryGetValue(conductorAsignado.IdOperario, out var servicio)
                    ? servicio : null;
            var servicioFirmado = servicioAsignado is not null
                && !string.IsNullOrWhiteSpace(servicioAsignado.FirmaNombre)
                && !string.IsNullOrWhiteSpace(servicioAsignado.FirmaDni)
                && !string.IsNullOrWhiteSpace(servicioAsignado.FirmaPath);
            var centralRegreso = servicioFirmado && servicioAsignado?.IdPlantaRegreso is int idCentral
                && centralesRegreso.TryGetValue(idCentral, out var central)
                    ? central : null;

            try
            {
                var position = await tracking.ObtenerPosicionAsync(camion.UnidadWialonId!, cancellationToken);
                var ubicacion = new UbicacionCamionWialon
                {
                    IdCamion = camion.IdCamion,
                    Matricula = camion.Matricula,
                    Descripcion = camion.Descripcion,
                    IdServicio = servicioAsignado?.IdSolicitud,
                    NombreObra = servicioAsignado?.NombreObra,
                    NombreConductor = conductorAsignado?.Nombre,
                    DistanciaRutaMetros = servicioAsignado is null ? null : servicioFirmado ? servicioAsignado.DistanciaDescargaRegresoMetros : recorridoCompleto ? servicioAsignado.DistanciaTotalMetros : servicioAsignado.DistanciaOrigenObraMetros,
                    MinutosViajeRuta = servicioAsignado is null ? null : servicioFirmado ? servicioAsignado.MinutosDescargaRegreso : recorridoCompleto ? servicioAsignado.DuracionViajeMinutos : servicioAsignado.MinutosOrigenObra,
                    FaseRuta = servicioAsignado is null ? null : servicioFirmado ? "Regreso" : "En ruta a obra",
                    UnidadWialonId = camion.UnidadWialonId!,
                    NombreUnidad = position.Posicion?.Nombre,
                    Latitud = position.Posicion?.Latitud,
                    Longitud = position.Posicion?.Longitud,
                    VelocidadKmH = position.Posicion?.Velocidad,
                    EstadoUnidad = ObtenerEstadoUnidad(position),
                    Rumbo = position.Posicion?.Rumbo,
                    FechaPosicionUtc = position.Posicion?.FechaUtc,
                    FechaPosicionLocal = ConvertirAHoraWialon(position.Posicion?.FechaUtc),
                    Error = position.Error
                };

                var latitudDestino = servicioFirmado ? centralRegreso?.Latitud : servicioAsignado?.LatitudDestino;
                var longitudDestino = servicioFirmado ? centralRegreso?.Longitud : servicioAsignado?.LongitudDestino;
                if (ubicacion.Error is null
                    && ubicacion.Latitud.HasValue && ubicacion.Longitud.HasValue
                    && latitudDestino.HasValue && longitudDestino.HasValue)
                {
                    ubicacion.DestinoRestante = servicioFirmado ? centralRegreso?.Nombre ?? "Central" : servicioAsignado?.NombreObra;
                    try
                    {
                        var tramo = await mapboxDirections.CalcularTramoAsync(
                            (decimal)ubicacion.Latitud.Value, (decimal)ubicacion.Longitud.Value,
                            latitudDestino.Value, longitudDestino.Value, cancellationToken: cancellationToken);
                        var distanciaRestante = tramo.DistanciaMetros;
                        var minutosRestantes = (int)Math.Ceiling(tramo.DuracionSegundos / 60d);
                        if (recorridoCompleto && !servicioFirmado)
                        {
                            distanciaRestante += (servicioAsignado.DistanciaObraDescargaMetros ?? 0)
                                + (servicioAsignado.DistanciaDescargaRegresoMetros ?? 0);
                            minutosRestantes += (servicioAsignado.MinutosObraDescarga ?? 0)
                                + (servicioAsignado.MinutosDescargaRegreso ?? 0);
                        }
                        ubicacion.DistanciaRestanteMetros = distanciaRestante;
                        ubicacion.MinutosViajeRestantes = minutosRestantes;
                        ubicacion.MinutosOperacionRestantes = servicioAsignado.DuracionOperacionMinutos is > 0
                            ? servicioAsignado.DuracionOperacionMinutos
                            : duracionOperacionPorDefecto;
                        ubicacion.MinutosTotalRestantes = ubicacion.MinutosViajeRestantes + ubicacion.MinutosOperacionRestantes;
                    }
                    catch (Exception ex)
                    {
                        ubicacion.ErrorRutaRestante = ex.Message;
                    }
                }

                result.Add(ubicacion);
            }
            catch (Exception ex)
            {
                result.Add(new UbicacionCamionWialon
                {
                    IdCamion = camion.IdCamion,
                    Matricula = camion.Matricula,
                    Descripcion = camion.Descripcion,
                    IdServicio = servicioAsignado?.IdSolicitud,
                    NombreObra = servicioAsignado?.NombreObra,
                    NombreConductor = conductorAsignado?.Nombre,
                    DistanciaRutaMetros = servicioAsignado is null ? null : servicioFirmado ? servicioAsignado.DistanciaDescargaRegresoMetros : recorridoCompleto ? servicioAsignado.DistanciaTotalMetros : servicioAsignado.DistanciaOrigenObraMetros,
                    MinutosViajeRuta = servicioAsignado is null ? null : servicioFirmado ? servicioAsignado.MinutosDescargaRegreso : recorridoCompleto ? servicioAsignado.DuracionViajeMinutos : servicioAsignado.MinutosOrigenObra,
                    FaseRuta = servicioAsignado is null ? null : servicioFirmado ? "Regreso" : "En ruta a obra",
                    UnidadWialonId = camion.UnidadWialonId!,
                    Error = ex.Message
                });
            }
        }
        return Ok(result);
    }
}