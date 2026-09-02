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
        solicitud.IdPlantaRegreso.HasValue &&
        GetLatitudObra(solicitud).HasValue &&
        GetLongitudObra(solicitud).HasValue;

    public async Task<CalculoRutaSolicitudResultado> CalcularYAplicarAsync(
        Solicitud solicitud,
        bool forzarActualizacion = false,
        CancellationToken cancellationToken = default)
    {
        if (!solicitud.IdPlantaOrigen.HasValue || !solicitud.IdPlantaRegreso.HasValue)
            throw new ProveedorRutasException("Seleccione la central de origen y la central de regreso.");

        var latitudObra = GetLatitudObra(solicitud);
        var longitudObra = GetLongitudObra(solicitud);
        if (!latitudObra.HasValue || !longitudObra.HasValue)
            throw new ProveedorRutasException("Indique las coordenadas de la obra antes de calcular la ruta.");

        var ids = new[]
            {
                solicitud.IdPlantaOrigen.Value,
                solicitud.IdPlantaRegreso.Value,
                solicitud.IdPlantaDescarga
            }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        var plantas = await _context.Plantas.AsNoTracking()
            .Where(p => ids.Contains(p.IdPlanta))
            .ToDictionaryAsync(p => p.IdPlanta, cancellationToken);

        var origen = GetPlanta(plantas, solicitud.IdPlantaOrigen.Value, "origen");
        var regreso = GetPlanta(plantas, solicitud.IdPlantaRegreso.Value, "regreso");
        var descarga = solicitud.IdPlantaDescarga.HasValue
            ? GetPlanta(plantas, solicitud.IdPlantaDescarga.Value, "reciclaje")
            : null;

        var tramoOrigenObra = await _mapboxDirections.CalcularTramoAsync(
            origen.Latitud!.Value, origen.Longitud!.Value,
            latitudObra.Value, longitudObra.Value,
            forzarActualizacion, cancellationToken);

        ResultadoTramoRuta? tramoObraDescarga = descarga is null
            ? null
            : await _mapboxDirections.CalcularTramoAsync(
                latitudObra.Value, longitudObra.Value,
                descarga.Latitud!.Value, descarga.Longitud!.Value,
                forzarActualizacion, cancellationToken);

        var tramoHastaRegreso = await _mapboxDirections.CalcularTramoAsync(
            descarga?.Latitud ?? latitudObra.Value,
            descarga?.Longitud ?? longitudObra.Value,
            regreso.Latitud!.Value, regreso.Longitud!.Value,
            forzarActualizacion, cancellationToken);

        solicitud.LatitudOrigen = origen.Latitud;
        solicitud.LongitudOrigen = origen.Longitud;
        solicitud.LatitudObra = latitudObra;
        solicitud.LongitudObra = longitudObra;
        solicitud.LatitudDescarga = descarga?.Latitud;
        solicitud.LongitudDescarga = descarga?.Longitud;
        solicitud.LatitudRegreso = regreso.Latitud;
        solicitud.LongitudRegreso = regreso.Longitud;

        solicitud.DistanciaOrigenObraMetros = tramoOrigenObra.DistanciaMetros;
        solicitud.DistanciaObraDescargaMetros = tramoObraDescarga?.DistanciaMetros ?? 0;
        solicitud.DistanciaDescargaRegresoMetros = tramoHastaRegreso.DistanciaMetros;
        solicitud.MinutosOrigenObra = AMinutos(tramoOrigenObra.DuracionSegundos);
        solicitud.MinutosObraDescarga = tramoObraDescarga is null ? 0 : AMinutos(tramoObraDescarga.Value.DuracionSegundos);
        solicitud.MinutosDescargaRegreso = AMinutos(tramoHastaRegreso.DuracionSegundos);
        solicitud.DistanciaTotalMetros = tramoOrigenObra.DistanciaMetros + (tramoObraDescarga?.DistanciaMetros ?? 0) + tramoHastaRegreso.DistanciaMetros;
        solicitud.DuracionViajeMinutos = solicitud.MinutosOrigenObra + solicitud.MinutosObraDescarga + solicitud.MinutosDescargaRegreso;
        var duracionOperacion = await ObtenerDuracionOperacionAsync(cancellationToken);
        solicitud.DuracionOperacionMinutos = duracionOperacion;

        if (forzarActualizacion || !solicitud.DuracionModificadaManualmente || solicitud.DuracionPlanificadaMinutos.GetValueOrDefault() <= 0)
        {
            solicitud.DuracionModificadaManualmente = false;
            solicitud.DuracionPlanificadaMinutos = solicitud.DuracionViajeMinutos + duracionOperacion;
        }

        if (solicitud.FechaHoraInicioPlanificada.HasValue && solicitud.DuracionPlanificadaMinutos > 0)
        {
            var redondeoHora = await ObtenerRedondeoHoraAsync(cancellationToken);
            solicitud.FechaHoraInicioPlanificada = RedondearAlIntervalo(solicitud.FechaHoraInicioPlanificada.Value, redondeoHora);
            solicitud.FechaHoraFinPlanificada = RedondearAlIntervalo(solicitud.FechaHoraInicioPlanificada.Value.AddMinutes(solicitud.DuracionPlanificadaMinutos.Value), redondeoHora);
        }

        solicitud.FechaCalculoRuta = DateTime.UtcNow;
        solicitud.ProveedorCalculoRuta = "OSRM / MapLibre Routing";

        var tramos = new[] { tramoOrigenObra, tramoObraDescarga, tramoHastaRegreso }
            .Where(tramo => tramo is not null)
            .Select(tramo => tramo!.Value)
            .ToArray();
        var desdeCache = tramos.Count(t => t.DesdeCache);
        return new CalculoRutaSolicitudResultado
        {
            Calculado = true,
            Mensaje = $"Ruta calculada: {solicitud.DistanciaTotalMetros / 1000d:0.0} km, {solicitud.DuracionViajeMinutos} min de viaje + {duracionOperacion} min de operación = {solicitud.DuracionPlanificadaMinutos} min totales.",
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

    private async Task<int> ObtenerRedondeoHoraAsync(CancellationToken cancellationToken)
    {
        var redondeoHora = await _context.Parametros.AsNoTracking()
            .Select(p => p.RedondeoHora)
            .FirstOrDefaultAsync(cancellationToken);
        return redondeoHora > 0 ? redondeoHora : 5;
    }

    private async Task<int> ObtenerDuracionOperacionAsync(CancellationToken cancellationToken)
    {
        var duracion = await _context.Parametros.AsNoTracking()
            .Select(p => p.DuracionOperacionServicioMinutos)
            .FirstOrDefaultAsync(cancellationToken);
        return Math.Max(0, duracion);
    }

    private static DateTime RedondearAlIntervalo(DateTime value, int intervaloMinutos)
    {
        var intervalo = TimeSpan.FromMinutes(intervaloMinutos > 0 ? intervaloMinutos : 5).Ticks;
        return new DateTime((value.Ticks + intervalo - 1) / intervalo * intervalo, value.Kind);
    }
}
