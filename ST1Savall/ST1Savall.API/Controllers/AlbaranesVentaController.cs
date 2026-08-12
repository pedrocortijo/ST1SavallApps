using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/albaranes-venta")]
public class AlbaranesVentaController(SageGestionDbContext context) : ControllerBase
{
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

        return cabeceras.Select(c => CrearEdicion(c, lineas.FirstOrDefault(l => l.EMPRESA == c.EMPRESA && l.NUMERO == c.NUMERO && l.LETRA == c.LETRA))).ToList();
    }

    [HttpGet("{empresa}/{numero}/{serie}")]
    public async Task<ActionResult<AlbaranVentaEdicion>> GetAlbaran(string empresa, string numero, string serie)
    {
        var cabecera = await context.AlbaranesVenta.AsNoTracking().FirstOrDefaultAsync(a => a.EMPRESA == empresa && a.NUMERO == numero && a.LETRA == serie);
        if (cabecera is null) return NotFound();
        var linea = await context.LineasAlbaranesVenta.AsNoTracking()
            .OrderBy(l => l.LINIA)
            .FirstOrDefaultAsync(l => l.EMPRESA == empresa && l.NUMERO == numero && l.LETRA == serie);
        return CrearEdicion(cabecera, linea);
    }

    [HttpPost]
    public async Task<ActionResult<AlbaranVentaEdicion>> PostAlbaran(AlbaranVentaEdicion datos)
    {
        var error = await ValidarYNormalizarAsync(datos, true);
        if (error is not null) return BadRequest(new { message = error });

        var yaExiste = await context.AlbaranesVenta.AnyAsync(a => a.EMPRESA == datos.Empresa && a.NUMERO == datos.Numero && a.LETRA == datos.Serie);
        if (yaExiste) return Conflict(new { message = "Ya existe un albarán con esa empresa, número y serie." });

        await using var transaccion = await context.Database.BeginTransactionAsync();
        var cliente = await ObtenerClienteAsync(datos.Cliente);
        var articulo = await context.Articulos.AsNoTracking().FirstAsync(a => a.Codigo == datos.Articulo);
        var cabecera = CrearCabecera(datos, cliente);
        context.AlbaranesVenta.Add(cabecera);
        context.LineasAlbaranesVenta.Add(CrearLinea(datos, cliente, articulo));
        await context.SaveChangesAsync();
        await transaccion.CommitAsync();
        return CreatedAtAction(nameof(GetAlbaran), new { empresa = datos.Empresa, numero = datos.Numero, serie = datos.Serie }, datos);
    }

    [HttpPut("{empresa}/{numero}/{serie}")]
    public async Task<IActionResult> PutAlbaran(string empresa, string numero, string serie, AlbaranVentaEdicion datos)
    {
        if (!CoincideClave(datos, empresa, numero, serie)) return BadRequest(new { message = "La clave del albarán no se puede modificar." });
        var error = await ValidarYNormalizarAsync(datos, false);
        if (error is not null) return BadRequest(new { message = error });

        var cabecera = await context.AlbaranesVenta.FirstOrDefaultAsync(a => a.EMPRESA == empresa && a.NUMERO == numero && a.LETRA == serie);
        if (cabecera is null) return NotFound();
        var linea = await context.LineasAlbaranesVenta.OrderBy(l => l.LINIA).FirstOrDefaultAsync(l => l.EMPRESA == empresa && l.NUMERO == numero && l.LETRA == serie);
        if (linea is null) return BadRequest(new { message = "El albarán no tiene línea editable." });

        var cliente = await ObtenerClienteAsync(datos.Cliente);
        var articulo = await context.Articulos.AsNoTracking().FirstAsync(a => a.Codigo == datos.Articulo);
        AplicarCabecera(cabecera, datos, cliente);
        AplicarLinea(linea, datos, cliente, articulo);
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{empresa}/{numero}/{serie}")]
    public async Task<IActionResult> DeleteAlbaran(string empresa, string numero, string serie)
    {
        var cabecera = await context.AlbaranesVenta.FirstOrDefaultAsync(a => a.EMPRESA == empresa && a.NUMERO == numero && a.LETRA == serie);
        if (cabecera is null) return NotFound();
        await using var transaccion = await context.Database.BeginTransactionAsync();
        var lineas = await context.LineasAlbaranesVenta.Where(l => l.EMPRESA == empresa && l.NUMERO == numero && l.LETRA == serie).ToListAsync();
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
        datos.Articulo = datos.Articulo?.Trim().ToUpperInvariant() ?? string.Empty;
        if (validarClave && (string.IsNullOrWhiteSpace(datos.Empresa) || string.IsNullOrWhiteSpace(datos.Numero) || string.IsNullOrWhiteSpace(datos.Serie))) return "Debe indicar empresa, número y serie.";
        if (string.IsNullOrWhiteSpace(datos.Cliente)) return "Debe indicar el cliente Sage.";
        if (string.IsNullOrWhiteSpace(datos.Almacen)) return "Debe indicar el almacén.";
        if (string.IsNullOrWhiteSpace(datos.Usuario)) return "Debe indicar el usuario Sage.";
        if (string.IsNullOrWhiteSpace(datos.Articulo)) return "Debe indicar el artículo.";
        if (datos.Unidades <= 0) return "Las unidades deben ser superiores a cero.";
        if (!await context.Clientes.AsNoTracking().AnyAsync(c => c.Codigo == datos.Cliente)) return "El cliente Sage indicado no existe.";
        if (!await context.Articulos.AsNoTracking().AnyAsync(a => a.Codigo == datos.Articulo)) return "El artículo Sage indicado no existe.";
        return null;
    }

    private async Task<ClienteSage50> ObtenerClienteAsync(string codigo) =>
        await context.Clientes.AsNoTracking().OrderBy(c => c.Clienteerp).FirstAsync(c => c.Codigo == codigo);

    private static bool CoincideClave(AlbaranVentaEdicion datos, string empresa, string numero, string serie) =>
        string.Equals(datos.Empresa?.Trim(), empresa.Trim(), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(datos.Numero?.Trim(), numero.Trim(), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(datos.Serie?.Trim(), serie.Trim(), StringComparison.OrdinalIgnoreCase);

    private static AlbaranVentaEdicion CrearEdicion(AlbaranVentaSage50 cabecera, LineaAlbaranVentaSage50? linea) => new()
    {
        Empresa = cabecera.EMPRESA.Trim(), Numero = cabecera.NUMERO.Trim(), Serie = cabecera.LETRA.Trim(), Fecha = cabecera.FECHA,
        Cliente = cabecera.CLIENTE.Trim(), Almacen = cabecera.ALMACEN.Trim(), Usuario = cabecera.USUARIO.Trim(),
        Articulo = linea?.ARTICULO.Trim() ?? string.Empty, Unidades = linea?.UNIDADES ?? 0, Precio = linea?.PRECIO ?? 0
    };

    private static AlbaranVentaSage50 CrearCabecera(AlbaranVentaEdicion d, ClienteSage50 cliente)
    {
        var cabecera = new AlbaranVentaSage50 { EMPRESA = d.Empresa, NUMERO = d.Numero, LETRA = d.Serie };
        AplicarCabecera(cabecera, d, cliente);
        return cabecera;
    }

    private static void AplicarCabecera(AlbaranVentaSage50 c, AlbaranVentaEdicion d, ClienteSage50 cliente)
    {
        var ahora = DateTime.Now;
        c.USUARIO = d.Usuario; c.FECHA = d.Fecha; c.CLIENTE = d.Cliente; c.CLIENTEERP = cliente.Clienteerp.Trim(); c.ALMACEN = d.Almacen;
        c.FPAG = cliente.Fpag.Trim(); c.VENDEDOR = cliente.Vendedor.Trim(); c.RUTA = cliente.Ruta.Trim(); c.PRONTO = cliente.Pronto;
        c.IVA_INC = false; c.FACTURABLE = true; c.GASTOS = true; c.TRASPERP = true; c.VISTA = true; c.FECHASTOCK = d.Fecha;
        c.CREATED = c.CREATED == default ? ahora : c.CREATED; c.MODIFIED = ahora;
        c.GUID_ID = string.IsNullOrWhiteSpace(c.GUID_ID) ? Guid.NewGuid().ToString() : c.GUID_ID;
    }

    private static LineaAlbaranVentaSage50 CrearLinea(AlbaranVentaEdicion d, ClienteSage50 cliente, ArticuloSage50 articulo)
    {
        var linea = new LineaAlbaranVentaSage50 { EMPRESA = d.Empresa, NUMERO = d.Numero, LETRA = d.Serie, LINIA = 1 };
        AplicarLinea(linea, d, cliente, articulo);
        return linea;
    }

    private static void AplicarLinea(LineaAlbaranVentaSage50 l, AlbaranVentaEdicion d, ClienteSage50 cliente, ArticuloSage50 articulo)
    {
        var ahora = DateTime.Now;
        l.USUARIO = d.Usuario; l.ARTICULO = d.Articulo; l.DEFINICION = articulo.Nombre.Trim(); l.UNIDADES = d.Unidades;
        l.PRECIO = d.Precio; l.IMPORTE = d.Unidades * d.Precio; l.PRECIOIVA = d.Precio; l.IMPORTEIVA = l.IMPORTE;
        l.CLIENTE = d.Cliente; l.CLIENTEERP = cliente.Clienteerp.Trim(); l.TIPO_IVA = cliente.TipoIva.Trim(); l.TIPO_IVAV = cliente.TipoIva.Trim();
        l.FAMILIA = articulo.Familia.Trim(); l.ALMACEN = d.Almacen; l.FECHA = d.Fecha; l.VISTA = true; l.FACTURABLE = true;
        l.CREATED = l.CREATED == default ? ahora : l.CREATED; l.MODIFIED = ahora;
        l.GUID_ID = string.IsNullOrWhiteSpace(l.GUID_ID) ? Guid.NewGuid().ToString() : l.GUID_ID;
    }
}
