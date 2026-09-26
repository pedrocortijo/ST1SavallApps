using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Services;

/// <summary>
/// Servicio de emisión y almacenamiento del Documento Electrónico de Control Administrativo (DeCA).
/// Se invoca de forma automática al iniciar la marcha en entrega o retirada.
/// </summary>
public sealed class DeCaEmisionService(
    ApplicationDbContext db,
    SageGestionDbContext sage,
    SageComunDbContext comun,
    DeCaPdfService pdfService,
    ILogger<DeCaEmisionService> logger)
{
    public async Task<GenerarDeCaResultadoDto> EmitirDeCaAsync(
        int idSolicitud,
        string tipoOperacion, // "ENTREGA" | "RETIRADA"
        string? urlBaseServidor = null)
    {
        try
        {
            var solicitud = await db.Solicitudes.FirstOrDefaultAsync(s => s.IdSolicitud == idSolicitud);
            if (solicitud is null)
            {
                return new GenerarDeCaResultadoDto
                {
                    Exito = false,
                    Mensaje = $"No se ha encontrado la solicitud #{idSolicitud}."
                };
            }

            var parametros = await db.Parametros.AsNoTracking().FirstOrDefaultAsync();
            if (parametros is null)
            {
                return new GenerarDeCaResultadoDto
                {
                    Exito = false,
                    Mensaje = "No hay parámetros de aplicación configurados."
                };
            }

            // Asignar o conservar GUID único no predecible para inspección pública sin login
            if (string.IsNullOrWhiteSpace(solicitud.GuidDeCa))
            {
                solicitud.GuidDeCa = Guid.NewGuid().ToString("N");
            }

            // Datos del cliente y obra
            string? clienteNombre = solicitud.NombreCliente;
            string? clienteCif = null;

            if (solicitud.IdCliente > 0)
            {
                var obraSage = await comun.Obras.AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Codigo.Trim() == solicitud.IdCliente.ToString("D5"));

                if (obraSage is not null && !string.IsNullOrWhiteSpace(obraSage.Cliente))
                {
                    var clienteSage = await sage.Clientes.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Codigo.Trim() == obraSage.Cliente.Trim());

                    if (clienteSage is not null)
                    {
                        clienteNombre = clienteSage.Nombre.Trim();
                        clienteCif = clienteSage.Cif.Trim();
                    }
                }
            }

            // Datos del vehículo y conductor
            string? matricula = null;
            string? conductorNombre = solicitud.ConductorNombre;

            if (solicitud.IdConductor.HasValue)
            {
                var operario = await db.Operarios.AsNoTracking()
                    .Include(o => o.Camion)
                    .FirstOrDefaultAsync(o => o.IdOperario == solicitud.IdConductor.Value);

                if (operario is not null)
                {
                    conductorNombre = operario.Nombre;
                    matricula = operario.Camion?.Matricula;
                }
            }

            // Destino (Planta para retirada o dirección de obra para entrega)
            Planta? plantaDestino = null;
            var idPlanta = solicitud.IdPlantaDescarga ?? solicitud.IdPlantaOrigen;
            if (idPlanta.HasValue)
            {
                plantaDestino = await db.Plantas.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdPlanta == idPlanta.Value);
            }

            // Cubicaje del contenedor (buscar tipo de contenedor seleccionado)
            decimal? cubicajeM3 = solicitud.CubicajeM3DeCa;
            if (!cubicajeM3.HasValue)
            {
                var serieContenedor = string.Equals(tipoOperacion, "ENTREGA", StringComparison.OrdinalIgnoreCase)
                    ? (solicitud.CodigoEntrega ?? solicitud.CodigoAmbosEntrega)
                    : (solicitud.CodigoRecogida ?? solicitud.CodigoAmbosRecogida);

                if (!string.IsNullOrWhiteSpace(serieContenedor))
                {
                    var cont = await db.Contenedores.AsNoTracking()
                        .Include(c => c.Tipo)
                        .FirstOrDefaultAsync(c => c.NumSerie.Trim() == serieContenedor.Trim());

                    cubicajeM3 = cont?.Tipo?.CapacidadMetrosCubicos;
                }

                // Por defecto para contenedores de obra estándar si no está informado en el tipo
                cubicajeM3 ??= 6.0m;
            }

            // Construcción de la URL de descarga para el QR
            var urlBase = !string.IsNullOrWhiteSpace(parametros.UrlBasePublicaDeCa)
                ? parametros.UrlBasePublicaDeCa.TrimEnd('/')
                : (!string.IsNullOrWhiteSpace(urlBaseServidor) ? urlBaseServidor.TrimEnd('/') : "https://savall.app");

            var urlDescargaQr = $"{urlBase}/api/deca/publico/descarga/{solicitud.GuidDeCa}";

            // Firma física/digital
            string? rutaFirma = solicitud.FirmaPath;
            if (string.IsNullOrWhiteSpace(rutaFirma) && !string.IsNullOrWhiteSpace(parametros.PathFirmas))
            {
                var rutaAlt = Path.Combine(parametros.PathFirmas.Trim(), $"{solicitud.IdSolicitud}.png");
                if (File.Exists(rutaAlt)) rutaFirma = rutaAlt;
            }

            // Sello temporal previo al inicio de la marcha
            var ahora = DateTime.Now;
            solicitud.FechaHoraEmisionDeCa = ahora;
            solicitud.TipoDeCaEmitido = tipoOperacion.ToUpperInvariant();
            solicitud.CubicajeM3DeCa = cubicajeM3;

            // Generación física del PDF nativo
            var bytesPdf = pdfService.Crear(
                solicitud,
                parametros,
                tipoOperacion,
                clienteNombre,
                clienteCif,
                matricula,
                conductorNombre,
                plantaDestino,
                cubicajeM3,
                rutaFirma,
                urlDescargaQr);

            // Guardar archivo en disco para conservación obligatoria de 1 año
            var carpetaBase = !string.IsNullOrWhiteSpace(parametros.PathDocumentos)
                ? parametros.PathDocumentos.Trim()
                : Path.Combine(AppContext.BaseDirectory, "Documentos");

            var carpetaDeCa = Path.Combine(carpetaBase, "DeCA", ahora.Year.ToString());
            Directory.CreateDirectory(carpetaDeCa);

            var nombreArchivo = $"DECA_{solicitud.IdSolicitud}_{tipoOperacion}_{solicitud.GuidDeCa}.pdf";
            var rutaCompleta = Path.Combine(carpetaDeCa, nombreArchivo);

            await File.WriteAllBytesAsync(rutaCompleta, bytesPdf);
            solicitud.RutaArchivoDeCa = rutaCompleta;

            await db.SaveChangesAsync();

            logger.LogInformation(
                "Documento DeCA emitido con éxito para la solicitud #{IdSolicitud} ({TipoOperacion}) con GUID {Guid}",
                idSolicitud, tipoOperacion, solicitud.GuidDeCa);

            return new GenerarDeCaResultadoDto
            {
                Exito = true,
                GuidDeCa = solicitud.GuidDeCa,
                UrlDescarga = urlDescargaQr,
                FechaHoraEmision = ahora,
                Tipo = tipoOperacion,
                Mensaje = "Documento DeCA emitido y sellado digitalmente con éxito."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al emitir DeCA para la solicitud #{IdSolicitud}", idSolicitud);
            return new GenerarDeCaResultadoDto
            {
                Exito = false,
                Mensaje = $"Error al emitir el documento DeCA: {ex.Message}"
            };
        }
    }

    public async Task<DeCaDetalleDto?> ObtenerDetalleDeCaAsync(int idSolicitud, string? urlBaseServidor = null)
    {
        var solicitud = await db.Solicitudes.AsNoTracking().FirstOrDefaultAsync(s => s.IdSolicitud == idSolicitud);
        if (solicitud is null) return null;

        var parametros = await db.Parametros.AsNoTracking().FirstOrDefaultAsync();
        var urlBase = !string.IsNullOrWhiteSpace(parametros?.UrlBasePublicaDeCa)
            ? parametros!.UrlBasePublicaDeCa!.TrimEnd('/')
            : (!string.IsNullOrWhiteSpace(urlBaseServidor) ? urlBaseServidor.TrimEnd('/') : "");

        string? matricula = null;
        string? conductorNombre = solicitud.ConductorNombre;

        if (solicitud.IdConductor.HasValue)
        {
            var operario = await db.Operarios.AsNoTracking()
                .Include(o => o.Camion)
                .FirstOrDefaultAsync(o => o.IdOperario == solicitud.IdConductor.Value);

            if (operario is not null)
            {
                conductorNombre = operario.Nombre;
                matricula = operario.Camion?.Matricula;
            }
        }

        string? nomPlanta = null;
        var idPlanta = solicitud.IdPlantaDescarga ?? solicitud.IdPlantaOrigen;
        if (idPlanta.HasValue)
        {
            var p = await db.Plantas.AsNoTracking().FirstOrDefaultAsync(pl => pl.IdPlanta == idPlanta.Value);
            nomPlanta = p?.Nombre;
        }

        var tienePdf = !string.IsNullOrWhiteSpace(solicitud.RutaArchivoDeCa) && File.Exists(solicitud.RutaArchivoDeCa);
        var urlDescarga = !string.IsNullOrWhiteSpace(solicitud.GuidDeCa)
            ? $"{urlBase}/api/deca/publico/descarga/{solicitud.GuidDeCa}"
            : null;

        var esEntrega = string.Equals(solicitud.TipoDeCaEmitido, "ENTREGA", StringComparison.OrdinalIgnoreCase);

        return new DeCaDetalleDto
        {
            IdSolicitud = solicitud.IdSolicitud,
            GuidDeCa = solicitud.GuidDeCa,
            FechaHoraEmision = solicitud.FechaHoraEmisionDeCa,
            Tipo = solicitud.TipoDeCaEmitido,
            UrlDescarga = urlDescarga,
            TienePdf = tienePdf,
            CargadorNombre = solicitud.NombreCliente,
            ObraNombre = solicitud.NombreObra,
            ObraDireccion = $"{solicitud.DireccionCliente}, {solicitud.PoblacionCliente}".Trim(',', ' '),
            TransportistaNombre = parametros?.Empresa ?? "SAVALL CONTENEDORES S.L.",
            TransportistaCif = "B54099023",
            AutorizacionTransporte = parametros?.AutorizacionTransporte ?? "AUTORIZACIÓN PÚBLICA DE TRANSPORTE",
            Matricula = matricula,
            ConductorNombre = conductorNombre,
            OrigenDireccion = esEntrega ? "Instalaciones Savall (Alicante)" : $"{solicitud.NombreObra} ({solicitud.PoblacionCliente})",
            DestinoDireccion = esEntrega ? $"{solicitud.NombreObra} ({solicitud.PoblacionCliente})" : (nomPlanta ?? "Planta de Reciclaje Autorizada"),
            DescripcionCarga = esEntrega ? "Contenedores de obra vacíos" : $"RCDs ({solicitud.TipoResiduo ?? "Escombros"})",
            CubicajeM3 = solicitud.CubicajeM3DeCa,
            Contenedores = esEntrega
                ? string.Join(", ", new[] { solicitud.CodigoEntrega, solicitud.CodigoAmbosEntrega }.Where(c => !string.IsNullOrWhiteSpace(c)))
                : string.Join(", ", new[] { solicitud.CodigoRecogida, solicitud.CodigoAmbosRecogida }.Where(c => !string.IsNullOrWhiteSpace(c)))
        };
    }

    public async Task<(byte[] Contenido, string NombreArchivo)?> ObtenerPdfPorGuidAsync(string guid)
    {
        if (string.IsNullOrWhiteSpace(guid)) return null;

        var solicitud = await db.Solicitudes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.GuidDeCa == guid);

        if (solicitud is null) return null;

        if (!string.IsNullOrWhiteSpace(solicitud.RutaArchivoDeCa) && File.Exists(solicitud.RutaArchivoDeCa))
        {
            var bytes = await File.ReadAllBytesAsync(solicitud.RutaArchivoDeCa);
            var nombre = Path.GetFileName(solicitud.RutaArchivoDeCa);
            return (bytes, nombre);
        }

        // Si por alguna razón el archivo no existiera en disco, se regenera sobre la marcha
        var resultado = await EmitirDeCaAsync(solicitud.IdSolicitud, solicitud.TipoDeCaEmitido ?? "RETIRADA");
        if (resultado.Exito)
        {
            var solicitudActualizada = await db.Solicitudes.AsNoTracking().FirstOrDefaultAsync(s => s.IdSolicitud == solicitud.IdSolicitud);
            if (!string.IsNullOrWhiteSpace(solicitudActualizada?.RutaArchivoDeCa) && File.Exists(solicitudActualizada.RutaArchivoDeCa))
            {
                var bytes = await File.ReadAllBytesAsync(solicitudActualizada.RutaArchivoDeCa);
                var nombre = Path.GetFileName(solicitudActualizada.RutaArchivoDeCa);
                return (bytes, nombre);
            }
        }

        return null;
    }

    public byte[] GenerarQrPng(string texto) => pdfService.GenerarQrPng(texto);
}
