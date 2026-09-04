using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Services;

/// <summary>Genera el albarán Sage asociado al cierre de un servicio.</summary>
public sealed class GeneracionAlbaranServicioService(
    SageGestionDbContext sage,
    SageComunDbContext comun,
    ApplicationDbContext aplicacion,
    DatosAlbaranPlantaExcelService datosAlbaranPlantaExcel,
    ILogger<GeneracionAlbaranServicioService> logger)
{
    private const int TipoDocumentoAlbaranVenta = 4;
    private const int FicheroCamposAdicionalesAlbaranVenta = 1;
    private const int LongitudNumeroAlbaranSage = 12;

    public async Task<ResultadoGeneracionAlbaran> GenerarAsync(Solicitud solicitud)
    {
        try
        {
            var parametros = await aplicacion.Parametros.AsNoTracking().FirstOrDefaultAsync();
            if (parametros is null || string.IsNullOrWhiteSpace(parametros.EmpresaAlbaranes)
                || string.IsNullOrWhiteSpace(parametros.SerieAlbaranes)
                || string.IsNullOrWhiteSpace(parametros.AlmacenAlbaranes)
                || string.IsNullOrWhiteSpace(parametros.UsuarioAlbaranes))
                return ResultadoGeneracionAlbaran.Fallido("Faltan los parámetros de empresa, serie, almacén o usuario Sage para los albaranes.");

            var obra = await comun.Obras.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Codigo.Trim() == solicitud.IdCliente.ToString("D5"));
            if (obra is null)
                return ResultadoGeneracionAlbaran.Fallido("No se ha encontrado la obra Sage asociada al servicio.");

            var clienteCodigo = obra.Cliente.Trim();
            if (string.IsNullOrWhiteSpace(clienteCodigo))
                return ResultadoGeneracionAlbaran.Fallido("La obra Sage no tiene un cliente asignado.");

            var articuloCodigo = solicitud.TipoResiduo?.Trim().ToUpperInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(articuloCodigo))
                return ResultadoGeneracionAlbaran.Fallido("El servicio no tiene un tipo de residuo (artículo Sage).");

            var cliente = await sage.Clientes.AsNoTracking().OrderBy(c => c.Clienteerp)
                .FirstOrDefaultAsync(c => c.Codigo.Trim() == clienteCodigo);
            if (cliente is null)
                return ResultadoGeneracionAlbaran.Fallido($"No existe el cliente Sage '{clienteCodigo}' de la obra.");

            var articulo = await sage.Articulos.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Codigo.Trim() == articuloCodigo);
            if (articulo is null)
                return ResultadoGeneracionAlbaran.Fallido($"No existe el artículo Sage '{articuloCodigo}'.");



            var matricula = solicitud.IdConductor.HasValue
                ? await aplicacion.Operarios.AsNoTracking()
                    .Where(o => o.IdOperario == solicitud.IdConductor.Value)
                    .Select(o => o.Camion == null ? null : o.Camion.Matricula)
                    .FirstOrDefaultAsync()
                : null;
            var idPlantaPesaje = solicitud.IdPlantaDescarga ?? solicitud.IdPlantaOrigen;
            var plantaPesaje = idPlantaPesaje.HasValue
                ? await aplicacion.Plantas.AsNoTracking().FirstOrDefaultAsync(p => p.IdPlanta == idPlantaPesaje.Value)
                : null;
            DatosAlbaranPlanta? datosPlanta = null;
            try
            {
                if (plantaPesaje is null)
                    throw new InvalidOperationException("El servicio no tiene una planta de reciclaje configurada.");

                datosPlanta = await datosAlbaranPlantaExcel.ObtenerAsync(parametros, plantaPesaje.Nombre, solicitud.AlbaranPlanta, DateTime.Today);
                solicitud.KgAlbaran = decimal.ToInt32(decimal.Round(datosPlanta.NetoKg, 0, MidpointRounding.AwayFromZero));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "No se pudieron obtener los datos opcionales del albarán de planta {AlbaranPlanta} para el servicio {Solicitud}",
                    solicitud.AlbaranPlanta, solicitud.IdSolicitud);
            }

            var empresa = parametros.EmpresaAlbaranes.Trim().ToUpperInvariant();
            var serie = parametros.SerieAlbaranes.Trim().ToUpperInvariant();
            var datos = new AlbaranVentaEdicion
            {
                Empresa = empresa,
                Serie = serie,
                Almacen = parametros.AlmacenAlbaranes.Trim().ToUpperInvariant(),
                Usuario = parametros.UsuarioAlbaranes.Trim(),
                Cliente = clienteCodigo,
                Obra = obra.Codigo.Trim(),
                Articulo = articuloCodigo,
                Unidades = 1m,
                Precio = 0m,
                Fecha = DateTime.Today
            };

            await AplicarPrecioObraAsync(datos, obra.Tarifa);
            var calculoFiscal = await CalcularFiscalAsync(datos, cliente);
            await using var transaccion = await sage.Database.BeginTransactionAsync();
            var numero = await ReservarSiguienteNumeroDisponibleAsync(empresa, serie);
            if (numero is null)
                return ResultadoGeneracionAlbaran.Fallido($"No existe contador de Sage para empresa {empresa}, serie {serie} y albaranes de venta.");

            datos.Numero = numero;
            var cabecera = CrearCabecera(datos, cliente, calculoFiscal);
            var linea = CrearLinea(datos, cliente, articulo, calculoFiscal);
            sage.AlbaranesVenta.Add(cabecera);
            sage.LineasAlbaranesVenta.Add(linea);
            foreach (var campo in CrearCamposAdicionales(cabecera, matricula?.Trim() ?? string.Empty, solicitud.AlbaranPlanta, datosPlanta))
                sage.CamposAdicionalesDocumentosVenta.Add(campo);
            await sage.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoGeneracionAlbaran.Correcto(serie, numero.Trim());
        }
        catch (Exception ex)
        {
            return ResultadoGeneracionAlbaran.Fallido(ex.GetBaseException().Message);
        }
    }

    public async Task<ResultadoProcesoAdjudicarKgPlantas> ActualizarKgPlantasAsync(DateTime fechaDesde, DateTime fechaHasta)
    {
        if (fechaHasta.Date < fechaDesde.Date)
            throw new InvalidOperationException("La fecha final no puede ser anterior a la fecha inicial.");

        var resultado = new ResultadoProcesoAdjudicarKgPlantas();
        var parametros = await aplicacion.Parametros.AsNoTracking().FirstOrDefaultAsync();
        if (parametros is null) return resultado;

        var hastaExclusiva = fechaHasta.Date.AddDays(1);
        var solicitudes = await aplicacion.Solicitudes
            .Where(s => s.KgAlbaran == null
                && !string.IsNullOrWhiteSpace(s.AlbaranPlanta)
                && !string.IsNullOrWhiteSpace(s.AlbaranSerieSage)
                && !string.IsNullOrWhiteSpace(s.AlbaranNumeroSage)
                && s.IdPlantaDescarga.HasValue
                && ((s.FechaTarea.HasValue && s.FechaTarea >= fechaDesde.Date && s.FechaTarea < hastaExclusiva)
                    || (!s.FechaTarea.HasValue && s.FechaSolicitud.HasValue && s.FechaSolicitud >= fechaDesde.Date && s.FechaSolicitud < hastaExclusiva)))
            .OrderBy(s => s.FechaTarea ?? s.FechaSolicitud)
            .ToListAsync();

        resultado.Revisados = solicitudes.Count;
        if (solicitudes.Count == 0) return resultado;

        var idsPlanta = solicitudes.Select(s => s.IdPlantaDescarga!.Value).Distinct().ToList();
        var plantas = await aplicacion.Plantas.AsNoTracking()
            .Where(p => idsPlanta.Contains(p.IdPlanta))
            .ToDictionaryAsync(p => p.IdPlanta, p => p.Nombre);

        foreach (var solicitud in solicitudes)
        {
            if (!plantas.TryGetValue(solicitud.IdPlantaDescarga!.Value, out var nombrePlanta))
            {
                resultado.SinDatos++;
                continue;
            }

            var albaran = await sage.AlbaranesVenta.FirstOrDefaultAsync(a =>
                a.LETRA.Trim() == solicitud.AlbaranSerieSage!.Trim()
                && a.NUMERO.Trim() == solicitud.AlbaranNumeroSage!.Trim());
            if (albaran is null)
            {
                resultado.SinDatos++;
                continue;
            }

            var datosPesaje = await datosAlbaranPlantaExcel.IntentarObtenerAsync(
                parametros, nombrePlanta, solicitud.AlbaranPlanta, albaran.FECHA.Date);
            if (datosPesaje is null)
            {
                resultado.SinDatos++;
                continue;
            }

            var kg = decimal.ToInt32(decimal.Round(datosPesaje.NetoKg, 0, MidpointRounding.AwayFromZero));
            solicitud.KgAlbaran = kg;

            var campoNetoKg = await sage.CamposAdicionalesDocumentosVenta.FirstOrDefaultAsync(c =>
                c.EMPRESA.Trim() == albaran.EMPRESA.Trim()
                && c.NUMERO.Trim() == albaran.NUMERO.Trim()
                && c.LETRA.Trim() == albaran.LETRA.Trim()
                && c.FICHERO == FicheroCamposAdicionalesAlbaranVenta
                && c.CAMPO.Trim() == "004");
            if (campoNetoKg is null)
            {
                sage.CamposAdicionalesDocumentosVenta.Add(new CampoAdicionalDocumentoVentaSage50
                {
                    EMPRESA = albaran.EMPRESA,
                    NUMERO = albaran.NUMERO,
                    LETRA = albaran.LETRA,
                    FICHERO = FicheroCamposAdicionalesAlbaranVenta,
                    CAMPO = "004",
                    VALOR = datosPesaje.NetoKgTexto,
                    VISTA = true,
                    GUID_ID = Guid.NewGuid().ToString(),
                    CREATED = DateTime.Now,
                    MODIFIED = DateTime.Now
                });
            }
            else
            {
                campoNetoKg.VALOR = datosPesaje.NetoKgTexto;
                campoNetoKg.VISTA = true;
                campoNetoKg.MODIFIED = DateTime.Now;
            }

            resultado.Actualizados++;
            resultado.Detalles.Add(new DetalleProcesoAdjudicarKgPlantas
            {
                IdSolicitud = solicitud.IdSolicitud,
                FechaServicio = solicitud.FechaTarea ?? solicitud.FechaSolicitud,
                AlbaranPlanta = solicitud.AlbaranPlanta?.Trim() ?? string.Empty,
                AlbaranSage = $"{albaran.LETRA.Trim()}-{albaran.NUMERO.Trim()}",
                Kg = kg,
                Actualizado = true
            });
        }

        await sage.SaveChangesAsync();
        await aplicacion.SaveChangesAsync();
        return resultado;
    }
    public async Task<ResultadoProcesoCamposAdicionalesAlbaranes> ActualizarCamposAdicionalesAsync(DateTime fechaDesde, DateTime fechaHasta)
    {
        if (fechaHasta.Date < fechaDesde.Date)
            throw new InvalidOperationException("La fecha final no puede ser anterior a la fecha inicial.");

        var resultado = new ResultadoProcesoCamposAdicionalesAlbaranes();
        var hastaExclusiva = fechaHasta.Date.AddDays(1);
        var albaranes = await sage.AlbaranesVenta
            .Where(a => a.FECHA >= fechaDesde.Date && a.FECHA < hastaExclusiva)
            .OrderBy(a => a.FECHA).ThenBy(a => a.LETRA).ThenBy(a => a.NUMERO)
            .ToListAsync();
        resultado.Total = albaranes.Count;

        foreach (var albaran in albaranes)
        {
            var detalle = new DetalleProcesoCamposAdicionalesAlbaran
            {
                Fecha = albaran.FECHA.Date,
                Serie = albaran.LETRA.Trim(),
                Numero = albaran.NUMERO.Trim()
            };
            var valores = ObtenerValoresObservacion(albaran.OBSERVACIO);
            if (valores is null)
            {
                detalle.Aviso = "OBSERVACIO debe contener Kg, número de albarán de planta y fecha separados por comas.";
                resultado.ConAviso++;
                resultado.Detalles.Add(detalle);
                continue;
            }

            detalle.NetoKg = valores.NetoKg;
            detalle.AlbaranPlanta = valores.AlbaranPlanta;
            detalle.FechaPlanta = valores.Fecha;
            foreach (var valor in new[] { (Campo: "001", Valor: albaran.LIBRE_1), (Campo: "002", Valor: valores.AlbaranPlanta), (Campo: "003", Valor: valores.Fecha), (Campo: "004", Valor: valores.NetoKg) })
            {
                var campo = await sage.CamposAdicionalesDocumentosVenta.FirstOrDefaultAsync(c =>
                    c.EMPRESA.Trim() == albaran.EMPRESA.Trim()
                    && c.NUMERO.Trim() == albaran.NUMERO.Trim()
                    && c.LETRA.Trim() == albaran.LETRA.Trim()
                    && c.FICHERO == FicheroCamposAdicionalesAlbaranVenta
                    && c.CAMPO.Trim() == valor.Campo);
                if (campo is null)
                {
                    campo = new CampoAdicionalDocumentoVentaSage50
                    {
                        EMPRESA = albaran.EMPRESA, NUMERO = albaran.NUMERO, LETRA = albaran.LETRA,
                        FICHERO = FicheroCamposAdicionalesAlbaranVenta, CAMPO = valor.Campo, VALOR = valor.Valor, VISTA = true,
                        GUID_ID = Guid.NewGuid().ToString(), CREATED = DateTime.Now, MODIFIED = DateTime.Now
                    };
                    sage.CamposAdicionalesDocumentosVenta.Add(campo);
                }
                else
                {
                    campo.VALOR = valor.Valor;
                    campo.VISTA = true;
                    campo.MODIFIED = DateTime.Now;
                }
            }
            detalle.Actualizado = true;
            resultado.Actualizados++;
            resultado.Detalles.Add(detalle);
        }
        await sage.SaveChangesAsync();
        return resultado;
    }

    private static ValoresObservacionAlbaran? ObtenerValoresObservacion(string? observacion)
    {
        var valores = (observacion ?? string.Empty).Split(',', StringSplitOptions.TrimEntries);
        return valores.Length >= 3 && valores.Take(3).All(v => !string.IsNullOrWhiteSpace(v))
            ? new ValoresObservacionAlbaran(valores[0], valores[1], valores[2])
            : null;
    }
    private async Task<decimal?> ReservarSiguienteNumeroAsync(string empresa, string serie)
    {
        var contador = await sage.Series.FromSqlInterpolated(
                $"SELECT * FROM series WITH (UPDLOCK, HOLDLOCK) WHERE EMPRESA = {empresa} AND SERIE = {serie} AND TIPODOC = {TipoDocumentoAlbaranVenta}")
            .SingleOrDefaultAsync();
        if (contador is null) return null;
        contador.CONTADOR += 1m;
        await sage.SaveChangesAsync();
        return contador.CONTADOR;
    }

    private async Task<string?> ReservarSiguienteNumeroDisponibleAsync(string empresa, string serie)
    {
        for (var intento = 0; intento < 1000; intento++)
        {
            var contador = await ReservarSiguienteNumeroAsync(empresa, serie);
            if (!contador.HasValue) return null;
            var numeroSinRelleno = contador.Value.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
            var numero = numeroSinRelleno.PadLeft(LongitudNumeroAlbaranSage);
            var existe = await sage.AlbaranesVenta.AsNoTracking().AnyAsync(a =>
                a.EMPRESA.Trim() == empresa && a.LETRA.Trim() == serie && a.NUMERO.Trim() == numeroSinRelleno);
            if (!existe) return numero;
        }
        throw new InvalidOperationException("No se ha encontrado un número libre después de avanzar el contador de albaranes.");
    }

    private async Task AplicarPrecioObraAsync(AlbaranVentaEdicion datos, string? tarifaObra)
    {
        var fecha = datos.Fecha.Date;
        if (!string.IsNullOrWhiteSpace(tarifaObra))
        {
            var precioTarifa = await aplicacion.TarifasLineas.AsNoTracking()
                .Where(l => l.Tarifa == tarifaObra
                    && l.Articulo.Trim() == datos.Articulo.Trim()
                    && l.Cabecera != null
                    && l.Cabecera.Desde <= fecha
                    && l.Cabecera.Hasta >= fecha)
                .Select(l => (decimal?)l.Precio)
                .FirstOrDefaultAsync();
            if (precioTarifa.HasValue) datos.Precio = precioTarifa.Value;
        }
        var precio = await aplicacion.PreciosEspecialesDetalles.AsNoTracking()
            .Where(d => d.ArticuloSage.Trim() == datos.Articulo.Trim() && d.IdPrecioEspecialCabecera == aplicacion.PreciosEspecialesCabeceras
                .Where(c => c.ObraSage.Trim() == datos.Obra.Trim() && (!c.VigenteDesde.HasValue || c.VigenteDesde <= fecha)
                    && (!c.VigenteHasta.HasValue || c.VigenteHasta >= fecha))
                .Select(c => c.IdPrecioEspecialCabecera).FirstOrDefault())
            .Select(d => (decimal?)d.Precio).FirstOrDefaultAsync();
        if (precio.HasValue) datos.Precio = precio.Value;
    }

    private async Task<CalculoFiscalAlbaran> CalcularFiscalAsync(AlbaranVentaEdicion datos, ClienteSage50 cliente)
    {
        var codigoIva = cliente.TipoIva.Trim();
        var tipoIva = await sage.TipoIva.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Codigo.Trim() == codigoIva);
        return CalculoFiscalAlbaran.Crear(datos.Unidades, datos.Precio, datos.Descuento1, datos.Descuento2, tipoIva, cliente);
    }

    private static AlbaranVentaSage50 CrearCabecera(AlbaranVentaEdicion d, ClienteSage50 cliente, CalculoFiscalAlbaran calculoFiscal)
    {
        var ahora = DateTime.Now;
        return new AlbaranVentaSage50
        {
            EMPRESA = d.Empresa, NUMERO = FormatearNumeroSage(d.Numero), LETRA = d.Serie, USUARIO = d.Usuario, FECHA = d.Fecha.Date,
            CLIENTE = d.Cliente, CLIENTEERP = cliente.Clienteerp.Trim(), ALMACEN = d.Almacen,
            FPAG = cliente.Fpag.Trim(), VENDEDOR = "     ", OPERARIO = "01", ENV_CLI = 1,
            DIVISA = "000", CAMBIO = 1m, STOCK_COEF = 1m, CANAL = "MATRICULA",
            OBRA = d.Obra, RUTA = cliente.Ruta.Trim(), PRONTO = cliente.Pronto,
            IMPORTE = calculoFiscal.BaseImponible, TOTALDOC = calculoFiscal.TotalDocumento,
            TOTALDIV = calculoFiscal.TotalDocumento, IMPDIVISA = calculoFiscal.TotalDocumento,
            PORCEN_RET = calculoFiscal.PorcentajeRetencion, MODO_RET = calculoFiscal.ModoRetencion,
            TPCRETNOFI = calculoFiscal.PorcentajeRetencion,
            IVA_INC = false, FACTURABLE = true, GASTOS = true, TRASPERP = true, VISTA = true,
            FECHASTOCK = d.Fecha.Date, CREATED = ahora, MODIFIED = ahora, GUID_ID = Guid.NewGuid().ToString()
        };
    }

    private static LineaAlbaranVentaSage50 CrearLinea(AlbaranVentaEdicion d, ClienteSage50 cliente, ArticuloSage50 articulo, CalculoFiscalAlbaran calculoFiscal)
    {
        var ahora = DateTime.Now;
        return new LineaAlbaranVentaSage50
        {
            EMPRESA = d.Empresa, NUMERO = FormatearNumeroSage(d.Numero), LETRA = d.Serie, LINIA = 1, USUARIO = d.Usuario,
            ARTICULO = d.Articulo, DEFINICION = articulo.Nombre.Trim(), UNIDADES = d.Unidades, PRECIO = d.Precio,
            IMPORTE = calculoFiscal.BaseImponible,
            PRECIOIVA = d.Unidades == 0m ? 0m : calculoFiscal.TotalAntesRetencion / d.Unidades,
            IMPORTEIVA = calculoFiscal.TotalAntesRetencion, CLIENTE = d.Cliente,
            CLIENTEERP = cliente.Clienteerp.Trim(), TIPO_IVA = cliente.TipoIva.Trim(), TIPO_IVAV = cliente.TipoIva.Trim(),
            FAMILIA = articulo.Familia.Trim(), ALMACEN = d.Almacen, FECHA = d.Fecha.Date, VISTA = true, FACTURABLE = true,
            CREATED = ahora, MODIFIED = ahora, GUID_ID = Guid.NewGuid().ToString()
        };
    }

    private static IEnumerable<CampoAdicionalDocumentoVentaSage50> CrearCamposAdicionales(
        AlbaranVentaSage50 cabecera,
        string matricula,
        string? numeroAlbaranPlanta,
        DatosAlbaranPlanta? datosPlanta)
    {
        var ahora = DateTime.Now;
        var valores = ObtenerValoresCamposAdicionales(matricula, numeroAlbaranPlanta, datosPlanta, cabecera.FECHA);

        return valores.Select(valor => new CampoAdicionalDocumentoVentaSage50
        {
            EMPRESA = cabecera.EMPRESA,
            NUMERO = cabecera.NUMERO,
            LETRA = cabecera.LETRA,
            FICHERO = FicheroCamposAdicionalesAlbaranVenta,
            CAMPO = valor.Campo,
            VALOR = valor.Valor,
            VISTA = true,
            GUID_ID = Guid.NewGuid().ToString(),
            CREATED = ahora,
            MODIFIED = ahora
        });
    }

    private static (string Campo, string Valor)[] ObtenerValoresCamposAdicionales(
        string matricula,
        string? numeroAlbaranPlanta,
        DatosAlbaranPlanta? datosPlanta,
        DateTime fechaAlbaran) =>
    [
        ("001", matricula),
        ("002", numeroAlbaranPlanta?.Trim() ?? string.Empty),
        ("003", fechaAlbaran.ToString("dd-MM-yy", CultureInfo.InvariantCulture)),
        ("004", datosPlanta?.NetoKgTexto ?? string.Empty)
    ];


    private sealed record ValoresObservacionAlbaran(string NetoKg, string AlbaranPlanta, string Fecha);

    private static string FormatearNumeroSage(string numero) => (numero ?? string.Empty).Trim().PadLeft(10, ' ');
}

public sealed record ResultadoGeneracionAlbaran(bool Generado, string? Serie, string? Numero, string? Error)
{
    public static ResultadoGeneracionAlbaran Correcto(string serie, string numero) => new(true, serie, numero, null);
    public static ResultadoGeneracionAlbaran Fallido(string error) => new(false, null, null, error);
}





