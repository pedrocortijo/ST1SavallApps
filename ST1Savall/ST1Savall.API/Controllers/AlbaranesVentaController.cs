using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.API.Services;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/albaranes-venta")]
public class AlbaranesVentaController(
    SageGestionDbContext context,
    SageComunDbContext comunContext,
    ApplicationDbContext applicationContext,
    GeneracionAlbaranServicioService generacionAlbaranServicio) : ControllerBase
{
    private const int LongitudNumeroAlbaranSage = 12;
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlbaranVentaEdicion>>> GetAlbaranes()
    {
        var cabeceras = await context.AlbaranesVenta.AsNoTracking()
            .OrderByDescending(a => a.FECHA).ThenByDescending(a => a.NUMERO)
            .Take(500)
            .ToListAsync();
        var claves = cabeceras.Select(a => $"{a.EMPRESA}|{a.NUMERO}|{a.LETRA}").ToHashSet();
        var empresas = cabeceras.Select(a => a.EMPRESA).Distinct().ToList();
        var numeros = cabeceras.Select(a => a.NUMERO).Distinct().ToList();
        var series = cabeceras.Select(a => a.LETRA).Distinct().ToList();
        var lineas = (await context.LineasAlbaranesVenta.AsNoTracking()
                .Where(l => empresas.Contains(l.EMPRESA) && numeros.Contains(l.NUMERO) && series.Contains(l.LETRA))
                .OrderBy(l => l.LINIA)
                .ToListAsync())
            .Where(l => claves.Contains($"{l.EMPRESA}|{l.NUMERO}|{l.LETRA}"))
            .ToList();
        var codigosCliente = cabeceras.Select(c => c.CLIENTE).Distinct().ToList();
        var clientes = await context.Clientes.AsNoTracking().Where(c => codigosCliente.Contains(c.Codigo)).ToListAsync();
        var tiposIva = await context.TipoIva.AsNoTracking().ToListAsync();
        var direccionesEnvio = await context.DireccionesEnvioClientes.AsNoTracking()
            .Where(d => codigosCliente.Contains(d.CLIENTE) && d.LINEA == 1)
            .ToListAsync();
        var codigosObra = cabeceras.Select(c => c.OBRA).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
        var obras = await comunContext.Obras.AsNoTracking().Where(o => codigosObra.Contains(o.Codigo)).ToListAsync();
        var camposAdicionales = (await context.CamposAdicionalesDocumentosVenta.AsNoTracking()
            .Where(c => c.FICHERO == 1 && empresas.Contains(c.EMPRESA) && numeros.Contains(c.NUMERO) && series.Contains(c.LETRA)
                && (c.CAMPO == "001" || c.CAMPO == "002" || c.CAMPO == "003" || c.CAMPO == "004"))
            .ToListAsync())
            .Where(c => claves.Contains($"{c.EMPRESA}|{c.NUMERO}|{c.LETRA}"))
            .ToList();

        return cabeceras.Select(c => CrearEdicion(c,
            lineas.FirstOrDefault(l => l.EMPRESA == c.EMPRESA && l.NUMERO == c.NUMERO && l.LETRA == c.LETRA),
            clientes.FirstOrDefault(cliente => cliente.Codigo == c.CLIENTE && cliente.Clienteerp == c.CLIENTEERP)
                ?? clientes.FirstOrDefault(cliente => cliente.Codigo == c.CLIENTE),
            obras.FirstOrDefault(obra => obra.Codigo == c.OBRA),
            direccionesEnvio.FirstOrDefault(d => d.CLIENTE == c.CLIENTE && d.LINEA == c.ENV_CLI),
            tiposIva.FirstOrDefault(t => t.Codigo.Trim() == (lineas.FirstOrDefault(l => l.EMPRESA == c.EMPRESA && l.NUMERO == c.NUMERO && l.LETRA == c.LETRA)?.TIPO_IVA.Trim() ?? string.Empty)), camposAdicionales)).ToList();
    }

    [HttpGet("{empresa}/{numero}/{serie}")]
    public async Task<ActionResult<AlbaranVentaEdicion>> GetAlbaran(string empresa, string numero, string serie)
    {
        var cabecera = await context.AlbaranesVenta.AsNoTracking().FirstOrDefaultAsync(a => a.EMPRESA == empresa && a.NUMERO == numero && a.LETRA == serie);
        if (cabecera is null) return NotFound();
        var linea = await context.LineasAlbaranesVenta.AsNoTracking()
            .OrderBy(l => l.LINIA)
            .FirstOrDefaultAsync(l => l.EMPRESA == empresa && l.NUMERO == numero && l.LETRA == serie);
        var cliente = await context.Clientes.AsNoTracking().OrderBy(c => c.Clienteerp)
            .FirstOrDefaultAsync(c => c.Codigo == cabecera.CLIENTE && c.Clienteerp == cabecera.CLIENTEERP)
            ?? await context.Clientes.AsNoTracking().OrderBy(c => c.Clienteerp).FirstOrDefaultAsync(c => c.Codigo == cabecera.CLIENTE);
        var obra = await comunContext.Obras.AsNoTracking().FirstOrDefaultAsync(o => o.Codigo == cabecera.OBRA);
        var direccionEnvio = await context.DireccionesEnvioClientes.AsNoTracking()
            .FirstOrDefaultAsync(d => d.CLIENTE == cabecera.CLIENTE && d.LINEA == cabecera.ENV_CLI);
        var tipoIva = linea is null ? null : await context.TipoIva.AsNoTracking().FirstOrDefaultAsync(t => t.Codigo.Trim() == linea.TIPO_IVA.Trim());
        var camposAdicionales = await context.CamposAdicionalesDocumentosVenta.AsNoTracking()
            .Where(c => c.FICHERO == 1 && c.EMPRESA == cabecera.EMPRESA && c.NUMERO == cabecera.NUMERO && c.LETRA == cabecera.LETRA && (c.CAMPO == "001" || c.CAMPO == "002" || c.CAMPO == "003" || c.CAMPO == "004"))
            .ToListAsync();
        return CrearEdicion(cabecera, linea, cliente, obra, direccionEnvio, tipoIva, camposAdicionales);
    }

    [HttpPost("campos-adicionales/validar-acceso")]
    public async Task<IActionResult> ValidarAccesoCamposAdicionales()
    {
        var error = await ValidarPasswordAdministracionAsync();
        return error is null ? NoContent() : error;
    }

    [HttpPost("campos-adicionales/procesar")]
    public async Task<ActionResult<ResultadoProcesoCamposAdicionalesAlbaranes>> ProcesarCamposAdicionales(
        ProcesoCamposAdicionalesAlbaranesRequest solicitud)
    {
        var errorAcceso = await ValidarPasswordAdministracionAsync();
        if (errorAcceso is not null) return errorAcceso;

        try
        {
            return Ok(await generacionAlbaranServicio.ActualizarCamposAdicionalesAsync(solicitud.FechaDesde, solicitud.FechaHasta));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpPost("kg-plantas/procesar")]
    public async Task<ActionResult<ResultadoProcesoAdjudicarKgPlantas>> ProcesarKgPlantas(
        ProcesoAdjudicarKgPlantasRequest solicitud)
    {
        try
        {
            return Ok(await generacionAlbaranServicio.ActualizarKgPlantasAsync(solicitud.FechaDesde, solicitud.FechaHasta));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet("direccion-envio/{cliente}")]
    public async Task<ActionResult<DireccionEnvioClienteSage50>> GetDireccionEnvio(string cliente)
    {
        var direccion = await context.DireccionesEnvioClientes.AsNoTracking()
            .FirstOrDefaultAsync(d => d.CLIENTE.Trim() == cliente.Trim() && d.LINEA == 1);
        return direccion is null ? NotFound() : Ok(direccion);
    }

    [HttpPost]
    public async Task<ActionResult<AlbaranVentaEdicion>> PostAlbaran(AlbaranVentaEdicion datos)
    {
        var parametros = await applicationContext.Parametros.AsNoTracking().FirstOrDefaultAsync();
        if (parametros is null
            || string.IsNullOrWhiteSpace(parametros.EmpresaAlbaranes)
            || string.IsNullOrWhiteSpace(parametros.SerieAlbaranes)
            || string.IsNullOrWhiteSpace(parametros.AlmacenAlbaranes)
            || string.IsNullOrWhiteSpace(parametros.UsuarioAlbaranes))
            return BadRequest(new { message = "Configure la serie y el almacén de albaranes en Parámetros antes de crear un albarán." });

        datos.Empresa = parametros.EmpresaAlbaranes;
        datos.Serie = parametros.SerieAlbaranes;
        datos.Almacen = parametros.AlmacenAlbaranes;
        datos.Usuario = parametros.UsuarioAlbaranes;
        var error = await ValidarYNormalizarAsync(datos, true);
        if (error is not null) return BadRequest(new { message = error });
        await AplicarPrecioEspecialAsync(datos);

        await using var transaccion = await context.Database.BeginTransactionAsync();
        var siguienteNumero = await ReservarSiguienteNumeroDisponibleAsync(datos.Empresa, datos.Serie);
        if (siguienteNumero is null)
            return BadRequest(new { message = $"No existe contador de Sage para empresa {datos.Empresa}, serie {datos.Serie} y albaranes de venta." });

        datos.Numero = siguienteNumero;
        var cliente = await ObtenerClienteAsync(datos.Cliente);
        var articulo = await context.Articulos.AsNoTracking().FirstAsync(a => a.Codigo == datos.Articulo);
        var calculoFiscal = await CalcularFiscalAsync(datos, cliente);
        var cabecera = CrearCabecera(datos, cliente, calculoFiscal);
        context.AlbaranesVenta.Add(cabecera);
        context.LineasAlbaranesVenta.Add(CrearLinea(datos, cliente, articulo, calculoFiscal));
        await context.SaveChangesAsync();
        await transaccion.CommitAsync();
        return CreatedAtAction(nameof(GetAlbaran), new { empresa = datos.Empresa, numero = datos.Numero, serie = datos.Serie }, datos);
    }

    [HttpPut]
    public async Task<IActionResult> PutAlbaran(AlbaranVentaEdicion datos)
    {
        var error = await ValidarYNormalizarAsync(datos, false);
        if (error is not null) return BadRequest(new { message = error });
        await AplicarPrecioEspecialAsync(datos);

        var cabecera = await context.AlbaranesVenta.FirstOrDefaultAsync(a =>
            a.EMPRESA.Trim() == datos.Empresa && a.NUMERO.Trim() == datos.Numero && a.LETRA.Trim() == datos.Serie);
        if (cabecera is null)
            return NotFound(new { message = $"No se encuentra el albarán Sage {datos.Empresa}/{datos.Serie}/{datos.Numero}." });
        var linea = await context.LineasAlbaranesVenta.OrderBy(l => l.LINIA).FirstOrDefaultAsync(l =>
            l.EMPRESA.Trim() == datos.Empresa && l.NUMERO.Trim() == datos.Numero && l.LETRA.Trim() == datos.Serie);
        if (linea is null) return BadRequest(new { message = "El albarán no tiene línea editable." });

        var cliente = await ObtenerClienteAsync(datos.Cliente);
        var articulo = await context.Articulos.AsNoTracking().FirstAsync(a => a.Codigo == datos.Articulo);
        var calculoFiscal = await CalcularFiscalAsync(datos, cliente);
        AplicarCabecera(cabecera, datos, cliente, calculoFiscal);
        AplicarLinea(linea, datos, cliente, articulo, calculoFiscal);
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAlbaran([FromQuery] string empresa, [FromQuery] string numero, [FromQuery] string? serie)
    {
        serie ??= string.Empty;
        empresa = empresa.Trim();
        numero = numero.Trim();
        serie = serie.Trim();
        var cabecera = await context.AlbaranesVenta.FirstOrDefaultAsync(a =>
            a.EMPRESA.Trim() == empresa && a.NUMERO.Trim() == numero && a.LETRA.Trim() == serie);
        if (cabecera is null)
            return NotFound(new { message = $"No se encuentra el albarán Sage {empresa}/{serie}/{numero}." });
        await using var transaccion = await context.Database.BeginTransactionAsync();
        var lineas = await context.LineasAlbaranesVenta.Where(l =>
            l.EMPRESA.Trim() == empresa && l.NUMERO.Trim() == numero && l.LETRA.Trim() == serie).ToListAsync();
        context.LineasAlbaranesVenta.RemoveRange(lineas);
        context.AlbaranesVenta.Remove(cabecera);
        await context.SaveChangesAsync();
        await transaccion.CommitAsync();
        return NoContent();
    }

    private async Task<string?> ValidarYNormalizarAsync(AlbaranVentaEdicion datos, bool validarClave)
    {
        datos.Empresa = datos.Empresa?.Trim().ToUpperInvariant() ?? string.Empty;
        datos.Numero = datos.Numero?.Trim().ToUpperInvariant() ?? string.Empty;
        datos.Serie = datos.Serie?.Trim().ToUpperInvariant() ?? string.Empty;
        datos.Cliente = datos.Cliente?.Trim().ToUpperInvariant() ?? string.Empty;
        datos.Almacen = datos.Almacen?.Trim().ToUpperInvariant() ?? string.Empty;
        datos.Usuario = datos.Usuario?.Trim() ?? string.Empty;
        datos.Vendedor = datos.Vendedor?.Trim().ToUpperInvariant() ?? string.Empty;
        datos.FormaPago = datos.FormaPago?.Trim().ToUpperInvariant() ?? string.Empty;
        datos.Operario = datos.Operario?.Trim().ToUpperInvariant() ?? string.Empty;
        datos.Obra = datos.Obra?.Trim().ToUpperInvariant() ?? string.Empty;
        datos.Articulo = datos.Articulo?.Trim().ToUpperInvariant() ?? string.Empty;
        if (validarClave && (string.IsNullOrWhiteSpace(datos.Empresa) || string.IsNullOrWhiteSpace(datos.Serie))) return "Debe indicar empresa y serie.";
        if (string.IsNullOrWhiteSpace(datos.Cliente)) return "Debe indicar el cliente Sage.";
        if (string.IsNullOrWhiteSpace(datos.Almacen)) return "Debe indicar el almacén.";
        if (string.IsNullOrWhiteSpace(datos.Usuario)) return "Debe indicar el usuario Sage.";
        if (string.IsNullOrWhiteSpace(datos.Articulo)) return "Debe indicar el artículo.";
        if (datos.Unidades <= 0) return "Las unidades deben ser superiores a cero.";
        if (!await context.Clientes.AsNoTracking().AnyAsync(c => c.Codigo == datos.Cliente)) return "El cliente Sage indicado no existe.";
        if (!await context.Articulos.AsNoTracking().AnyAsync(a => a.Codigo == datos.Articulo)) return "El artículo Sage indicado no existe.";
        return null;
    }

    private async Task<decimal?> ReservarSiguienteNumeroAsync(string empresa, string serie)
    {
        const int tipoDocumentoAlbaranVenta = 4;
        var contador = await context.Series
            .FromSqlInterpolated($"SELECT * FROM series WITH (UPDLOCK, HOLDLOCK) WHERE EMPRESA = {empresa} AND SERIE = {serie} AND TIPODOC = {tipoDocumentoAlbaranVenta}")
            .SingleOrDefaultAsync();

        if (contador is null)
            return null;

        contador.CONTADOR += 1m;
        await context.SaveChangesAsync();
        return contador.CONTADOR;
    }

    private async Task<string?> ReservarSiguienteNumeroDisponibleAsync(string empresa, string serie)
    {
        const int intentosMaximos = 1000;
        for (var intento = 0; intento < intentosMaximos; intento++)
        {
            var contador = await ReservarSiguienteNumeroAsync(empresa, serie);
            if (!contador.HasValue)
                return null;

            var numeroSinRelleno = contador.Value.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
            var numero = numeroSinRelleno.PadLeft(LongitudNumeroAlbaranSage);
            var existe = await context.AlbaranesVenta.AsNoTracking().AnyAsync(a =>
                a.EMPRESA.Trim() == empresa && a.LETRA.Trim() == serie && a.NUMERO.Trim() == numeroSinRelleno);
            if (!existe)
                return numero;
        }

        throw new InvalidOperationException("No se ha encontrado un número libre después de avanzar el contador de albaranes.");
    }

    private async Task<ClienteSage50> ObtenerClienteAsync(string codigo) =>
        await context.Clientes.AsNoTracking().OrderBy(c => c.Clienteerp).FirstAsync(c => c.Codigo == codigo);

    private async Task<CalculoFiscalAlbaran> CalcularFiscalAsync(AlbaranVentaEdicion datos, ClienteSage50 cliente)
    {
        var codigoIva = cliente.TipoIva.Trim();
        var tipoIva = await context.TipoIva.AsNoTracking().FirstOrDefaultAsync(t => t.Codigo.Trim() == codigoIva);
        return CalculoFiscalAlbaran.Crear(datos.Unidades, datos.Precio, datos.Descuento1, datos.Descuento2, tipoIva, cliente);
    }

    private async Task AplicarPrecioEspecialAsync(AlbaranVentaEdicion datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Obra)) return;
        var hoy = datos.Fecha.Date;
        var precio = await applicationContext.PreciosEspecialesDetalles.AsNoTracking()
            .Where(d => d.ArticuloSage == datos.Articulo && d.IdPrecioEspecialCabecera == applicationContext.PreciosEspecialesCabeceras
                .Where(c => c.ObraSage == datos.Obra
                    && (!c.VigenteDesde.HasValue || c.VigenteDesde <= hoy)
                    && (!c.VigenteHasta.HasValue || c.VigenteHasta >= hoy))
                .Select(c => c.IdPrecioEspecialCabecera).FirstOrDefault())
            .Select(d => (decimal?)d.Precio)
            .FirstOrDefaultAsync();
        if (precio.HasValue) datos.Precio = precio.Value;
    }

    private static AlbaranVentaEdicion CrearEdicion(AlbaranVentaSage50 cabecera, LineaAlbaranVentaSage50? linea, ClienteSage50? cliente, ObraComunSage50? obra = null, DireccionEnvioClienteSage50? direccionEnvio = null, TipoIvaSage50? tipoIva = null, IEnumerable<CampoAdicionalDocumentoVentaSage50>? camposAdicionales = null) => new()
    {
        Empresa = cabecera.EMPRESA.Trim(), Numero = cabecera.NUMERO.Trim(), Serie = cabecera.LETRA.Trim(), Fecha = cabecera.FECHA,
        Cliente = cabecera.CLIENTE.Trim(), Almacen = cabecera.ALMACEN.Trim(), Usuario = cabecera.USUARIO.Trim(),
        Vendedor = cabecera.VENDEDOR.Trim(), FormaPago = cabecera.FPAG.Trim(), Factura = cabecera.FACTURA.Trim(), FechaFactura = cabecera.FECHA_FAC, Operario = cabecera.OPERARIO.Trim(), Obra = cabecera.OBRA.Trim(),
        ObraNombre = obra?.Nombre.Trim() ?? string.Empty,
        Tarifa = cliente?.Tarifa.Trim() ?? string.Empty, Articulo = linea?.ARTICULO.Trim() ?? string.Empty, Definicion = linea?.DEFINICION.Trim() ?? string.Empty, Suplido = linea?.SUPLIDO ?? false,
        Unidades = linea?.UNIDADES ?? 0, Precio = linea?.PRECIO ?? 0, Descuento1 = linea?.DTO1 ?? 0, Descuento2 = linea?.DTO2 ?? 0, ImporteLinea = linea?.IMPORTE ?? 0,
        PorcentajeIva = tipoIva?.Iva ?? 0, ImporteIva = Math.Round((linea?.IMPORTE ?? 0) * (tipoIva?.Iva ?? 0) / 100m, 2, MidpointRounding.AwayFromZero), TotalDocumento = cabecera.TOTALDOC,
        ClienteCif = cliente?.Cif.Trim() ?? string.Empty, ClienteNombre = cliente?.Nombre.Trim() ?? string.Empty,
        ClienteDireccion = direccionEnvio?.DIRECCION.Trim() ?? cliente?.Direccion.Trim() ?? string.Empty, ClienteCodigoPostal = direccionEnvio?.CODPOS.Trim() ?? cliente?.Codpost.Trim() ?? string.Empty,
        ClientePoblacion = direccionEnvio?.POBLACION.Trim() ?? cliente?.Poblacion.Trim() ?? string.Empty, ClienteProvincia = direccionEnvio?.PROVINCIA.Trim() ?? cliente?.Provincia.Trim() ?? string.Empty,
        ClienteTelefono = cliente?.Telefono.Trim() ?? string.Empty, ClienteEmail = cliente?.Email.Trim() ?? string.Empty,
        Matricula = ObtenerCampoAdicional(camposAdicionales, "001"), AlbaranPlanta = ObtenerCampoAdicional(camposAdicionales, "002"),
        FechaPlanta = ObtenerCampoAdicional(camposAdicionales, "003"), NetoKg = ObtenerCampoAdicional(camposAdicionales, "004")
    };

    private static string ObtenerCampoAdicional(IEnumerable<CampoAdicionalDocumentoVentaSage50>? campos, string codigo) =>
        campos?.FirstOrDefault(c => c.CAMPO.Trim() == codigo)?.VALOR.Trim() ?? string.Empty;

    private static AlbaranVentaSage50 CrearCabecera(AlbaranVentaEdicion d, ClienteSage50 cliente, CalculoFiscalAlbaran calculoFiscal)
    {
        var cabecera = new AlbaranVentaSage50 { EMPRESA = d.Empresa, NUMERO = FormatearNumeroSage(d.Numero), LETRA = d.Serie };
        AplicarCabecera(cabecera, d, cliente, calculoFiscal);
        return cabecera;
    }

    private static void AplicarCabecera(AlbaranVentaSage50 c, AlbaranVentaEdicion d, ClienteSage50 cliente, CalculoFiscalAlbaran calculoFiscal)
    {
        var ahora = DateTime.Now;
        c.USUARIO = d.Usuario; c.FECHA = d.Fecha.Date; c.CLIENTE = d.Cliente; c.CLIENTEERP = cliente.Clienteerp.Trim(); c.ALMACEN = d.Almacen;
        c.FPAG = string.IsNullOrWhiteSpace(d.FormaPago) ? cliente.Fpag.Trim() : d.FormaPago; c.VENDEDOR = "     ";
        c.OPERARIO = "01"; c.OBRA = d.Obra; c.RUTA = cliente.Ruta.Trim(); c.PRONTO = cliente.Pronto; c.ENV_CLI = 1;
        c.DIVISA = "000"; c.CAMBIO = 1m; c.STOCK_COEF = 1m; c.CANAL = "MATRICULA";
        c.IMPORTE = calculoFiscal.BaseImponible; c.TOTALDOC = calculoFiscal.TotalDocumento; c.TOTALDIV = calculoFiscal.TotalDocumento; c.IMPDIVISA = calculoFiscal.TotalDocumento;
        c.PORCEN_RET = calculoFiscal.PorcentajeRetencion; c.MODO_RET = calculoFiscal.ModoRetencion; c.TPCRETNOFI = calculoFiscal.PorcentajeRetencion;
        c.IVA_INC = false; c.FACTURABLE = true; c.GASTOS = true; c.TRASPERP = true; c.VISTA = true; c.FECHASTOCK = d.Fecha.Date;
        c.CREATED = c.CREATED == default ? ahora : c.CREATED; c.MODIFIED = ahora;
        c.GUID_ID = string.IsNullOrWhiteSpace(c.GUID_ID) ? Guid.NewGuid().ToString() : c.GUID_ID;
    }

    private static LineaAlbaranVentaSage50 CrearLinea(AlbaranVentaEdicion d, ClienteSage50 cliente, ArticuloSage50 articulo, CalculoFiscalAlbaran calculoFiscal)
    {
        var linea = new LineaAlbaranVentaSage50 { EMPRESA = d.Empresa, NUMERO = FormatearNumeroSage(d.Numero), LETRA = d.Serie, LINIA = 1 };
        AplicarLinea(linea, d, cliente, articulo, calculoFiscal);
        return linea;
    }

    private static void AplicarLinea(LineaAlbaranVentaSage50 l, AlbaranVentaEdicion d, ClienteSage50 cliente, ArticuloSage50 articulo, CalculoFiscalAlbaran calculoFiscal)
    {
        var ahora = DateTime.Now;
        l.USUARIO = d.Usuario; l.ARTICULO = d.Articulo; l.DEFINICION = string.IsNullOrWhiteSpace(d.Definicion) ? articulo.Nombre.Trim() : d.Definicion.Trim(); l.SUPLIDO = d.Suplido; l.UNIDADES = d.Unidades;
        l.DTO1 = d.Descuento1; l.DTO2 = d.Descuento2; l.PRECIO = d.Precio; l.IMPORTE = calculoFiscal.BaseImponible;
        l.PRECIOIVA = d.Unidades == 0m ? 0m : calculoFiscal.TotalAntesRetencion / d.Unidades; l.IMPORTEIVA = calculoFiscal.TotalAntesRetencion;
        l.CLIENTE = d.Cliente; l.CLIENTEERP = cliente.Clienteerp.Trim(); l.TIPO_IVA = cliente.TipoIva.Trim(); l.TIPO_IVAV = cliente.TipoIva.Trim();
        l.FAMILIA = articulo.Familia.Trim(); l.ALMACEN = d.Almacen; l.FECHA = d.Fecha.Date; l.VISTA = true; l.FACTURABLE = true;
        l.CREATED = l.CREATED == default ? ahora : l.CREATED; l.MODIFIED = ahora;
        l.GUID_ID = string.IsNullOrWhiteSpace(l.GUID_ID) ? Guid.NewGuid().ToString() : l.GUID_ID;
    }

    private static string FormatearNumeroSage(string numero) => (numero ?? string.Empty).Trim().PadLeft(10, ' ');
    private static decimal CalcularImporteLinea(AlbaranVentaEdicion datos) =>
        Math.Round(datos.Unidades * datos.Precio * (1m - datos.Descuento1 / 100m) * (1m - datos.Descuento2 / 100m), 6, MidpointRounding.AwayFromZero);
    private async Task<ObjectResult?> ValidarPasswordAdministracionAsync()
    {
        var parametro = await applicationContext.Parametros.AsNoTracking().FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(parametro?.AdminPassword))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Configure la contraseña de administración en Parámetros." });

        if (!Request.Headers.TryGetValue("X-Admin-Password", out var password)
            || !string.Equals(password.ToString(), parametro.AdminPassword, StringComparison.Ordinal))
            return Unauthorized(new { message = "La contraseña de administración no es correcta." });

        return null;
    }
}




