using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Services;

public sealed class CalculoRutaSolicitudService
{
    private readonly ApplicationDbContext _context;
    private readonly MapboxDirectionsService _mapboxDirections;

    public CalculoRutaSolicitudService(ApplicationDbContext context, MapboxDirectionsService mapboxDirections)
    {
        _context = context;
        _mapboxDirections = mapboxDirections;
    }

    public static bool TieneDatosCompletos(Solicitud solicitud) =>
        solicitud.IdPlantaOrigen.HasValue &&
        solicitud.IdPlantaDescarga.HasValue &&
        solicitud.IdPlantaRegreso.HasValue &&
        GetLatitudObra(solicitud).HasValue &&
        GetLongitudObra(solicitud).HasValue;

    public async Task<CalculoRutaSolicitudResultado> CalcularYAplicarAsync(
        Solicitud solicitud,
        bool forzarActualizacion = false,
        CancellationToken cancellationToken = default)
    {
        if (!solicitud.IdPlantaOrigen.HasValue || !solicitud.IdPlantaDescarga.HasValue || !solicitud.IdPlantaRegreso.HasValue)
            throw new ProveedorRutasException("Seleccione la planta origen, la planta de descarga y la central de regreso.");

        var latitudObra = GetLatitudObra(solicitud);
        var longitudObra = GetLongitudObra(solicitud);
        if (!latitudObra.HasValue || !longitudObra.HasValue)
            throw new ProveedorRutasException("Indique las coordenadas de la obra antes de calcular la ruta.");

        var ids = new[]
        {
            solicitud.IdPlantaOrigen.Value,
            solicitud.IdPlantaDescarga.Value,
            solicitud.IdPlantaRegreso.Value
        }.Distinct().ToArray();

        var plantas = await _context.Plantas.AsNoTracking()
            .Where(p => ids.Contains(p.IdPlanta))
            .ToDictionaryAsync(p => p.IdPlanta, cancellationToken);

        var origen = GetPlanta(plantas, solicitud.IdPlantaOrigen.Value, "origen");
        var descarga = GetPlanta(plantas, solicitud.IdPlantaDescarga.Value, "descarga");
        var regreso = GetPlanta(plantas, solicitud.IdPlantaRegreso.Value, "regreso");

        var tramoOrigenObra = await _mapboxDirections.CalcularTramoAsync(
            origen.Latitud!.Value, origen.Longitud!.Value,
            latitudObra.Value, longitudObra.Value,
            forzarActualizacion, cancellationToken);

        var tramoObraDescarga = await _mapboxDirections.CalcularTramoAsync(
            latitudObra.Value, longitudObra.Value,
            descarga.Latitud!.Value, descarga.Longitud!.Value,
            forzarActualizacion, cancellationToken);

        var tramoDescargaRegreso = await _mapboxDirections.CalcularTramoAsync(
            descarga.Latitud!.Value, descarga.Longitud!.Value,
            regreso.Latitud!.Value, regreso.Longitud!.Value,
            forzarActualizacion, cancellationToken);

        solicitud.LatitudOrigen = origen.Latitud;
        solicitud.LongitudOrigen = origen.Longitud;
        solicitud.LatitudObra = latitudObra;
        solicitud.LongitudObra = longitudObra;
        solicitud.LatitudDescarga = descarga.Latitud;
        solicitud.LongitudDescarga = descarga.Longitud;
        solicitud.LatitudRegreso = regreso.Latitud;
        solicitud.LongitudRegreso = regreso.Longitud;

        solicitud.DistanciaOrigenObraMetros = tramoOrigenObra.DistanciaMetros;
        solicitud.DistanciaObraDescargaMetros = tramoObraDescarga.DistanciaMetros;
        solicitud.DistanciaDescargaRegresoMetros = tramoDescargaRegreso.DistanciaMetros;
        solicitud.MinutosOrigenObra = AMinutos(tramoOrigenObra.DuracionSegundos);
        solicitud.MinutosObraDescarga = AMinutos(tramoObraDescarga.DuracionSegundos);
        solicitud.MinutosDescargaRegreso = AMinutos(tramoDescargaRegreso.DuracionSegundos);
        solicitud.DistanciaTotalMetros = tramoOrigenObra.DistanciaMetros + tramoObraDescarga.DistanciaMetros + tramoDescargaRegreso.DistanciaMetros;
        solicitud.DuracionViajeMinutos = solicitud.MinutosOrigenObra + solicitud.MinutosObraDescarga + solicitud.MinutosDescargaRegreso;

        if (!solicitud.DuracionModificadaManualmente || solicitud.DuracionPlanificadaMinutos.GetValueOrDefault() <= 0)
            solicitud.DuracionPlanificadaMinutos = solicitud.DuracionViajeMinutos + solicitud.DuracionOperacionMinutos.GetValueOrDefault();

        if (solicitud.FechaHoraInicioPlanificada.HasValue && solicitud.DuracionPlanificadaMinutos > 0)
            solicitud.FechaHoraFinPlanificada = solicitud.FechaHoraInicioPlanificada.Value.AddMinutes(solicitud.DuracionPlanificadaMinutos.Value);

        solicitud.FechaCalculoRuta = DateTime.UtcNow;
        solicitud.ProveedorCalculoRuta = "Mapbox Directions";

        var tramos = new[] { tramoOrigenObra, tramoObraDescarga, tramoDescargaRegreso };
        var desdeCache = tramos.Count(t => t.DesdeCache);
        return new CalculoRutaSolicitudResultado
        {
            Calculado = true,
            Mensaje = $"Ruta calculada: {solicitud.DistanciaTotalMetros / 1000d:0.0} km y {solicitud.DuracionViajeMinutos} minutos de viaje.",
            TramosDesdeCache = desdeCache,
            TramosDesdeProveedor = tramos.Length - desdeCache,
            Solicitud = solicitud
        };
    }

    private static Planta GetPlanta(IReadOnlyDictionary<int, Planta> plantas, int id, string tipo)
    {
        if (!plantas.TryGetValue(id, out var planta))
            throw new ProveedorRutasException($"La planta de {tipo} seleccionada no existe.");
        if (!planta.Latitud.HasValue || !planta.Longitud.HasValue)
            throw new ProveedorRutasException($"La planta de {tipo} '{planta.Nombre}' no tiene coordenadas.");
        return planta;
    }

    private static decimal? GetLatitudObra(Solicitud solicitud) => solicitud.Latitud ?? solicitud.LatitudObra;
    private static decimal? GetLongitudObra(Solicitud solicitud) => solicitud.Longitud ?? solicitud.LongitudObra;
    private static int AMinutos(int segundos) => (int)Math.Ceiling(segundos / 60d);
}
