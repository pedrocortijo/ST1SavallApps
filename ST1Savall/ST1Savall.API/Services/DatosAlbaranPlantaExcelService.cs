using ClosedXML.Excel;
using ST1Savall.Shared.Data;
using System.Globalization;

namespace ST1Savall.API.Services;

/// <summary>Obtiene la fecha y el peso neto del albarán de planta desde sus hojas de control.</summary>
public sealed class DatosAlbaranPlantaExcelService
{
    private static readonly CultureInfo CulturaEspanola = CultureInfo.GetCultureInfo("es-ES");

    public Task<DatosAlbaranPlanta> ObtenerAsync(Parametro parametros, string? nombrePlanta, string? numeroAlbaranPlanta, DateTime fechaAlbaran)
    {
        if (string.IsNullOrWhiteSpace(numeroAlbaranPlanta))
            throw new InvalidOperationException("El servicio no tiene informado el número de albarán de planta.");

        var path = ObtenerRutaExcel(parametros, nombrePlanta);
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException($"No está configurada la ruta del Excel de albaranes para la planta '{nombrePlanta}'.");
        if (!File.Exists(path))
            throw new FileNotFoundException($"No se encontró el Excel de albaranes de planta: {path}", path);

        return Task.Run(() => BuscarEnLibro(path, numeroAlbaranPlanta, fechaAlbaran));
    }

    /// <summary>Busca sin interrumpir el mantenimiento cuando faltan archivo, hoja, fila o datos.</summary>
    public async Task<DatosAlbaranPlanta?> IntentarObtenerAsync(
        Parametro parametros,
        string? nombrePlanta,
        string? numeroAlbaranPlanta,
        DateTime fechaAlbaran)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(numeroAlbaranPlanta)) return null;
            var path = ObtenerRutaExcel(parametros, nombrePlanta);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
            return await Task.Run(() => BuscarEnLibro(path, numeroAlbaranPlanta, fechaAlbaran, buscarTodasLasHojas: true));
        }
        catch
        {
            return null;
        }
    }

    private static string? ObtenerRutaExcel(Parametro parametros, string? nombrePlanta)
    {
        var planta = nombrePlanta?.Trim() ?? string.Empty;
        if (planta.Contains("finestrat", StringComparison.OrdinalIgnoreCase))
            return parametros.ExcelAlbaranesSabospaFinestrat?.Trim();
        if (planta.Contains("monforte", StringComparison.OrdinalIgnoreCase))
            return parametros.ExcelAlbaranesSabospaMonforte?.Trim();
        if (planta.Contains("alicante", StringComparison.OrdinalIgnoreCase))
            return parametros.ExcelAlbaranesSabospaAlicante?.Trim();

        throw new InvalidOperationException($"No se puede identificar el Excel de planta para '{nombrePlanta ?? "sin planta de origen"}'.");
    }

    private static DatosAlbaranPlanta BuscarEnLibro(
        string path,
        string numeroAlbaranPlanta,
        DateTime fechaAlbaran,
        bool buscarTodasLasHojas = false)
    {
        var buscado = NormalizarNumero(numeroAlbaranPlanta);
        using var libro = new XLWorkbook(path);
        var nombreHoja = CulturaEspanola.DateTimeFormat.GetMonthName(fechaAlbaran.Month).ToUpperInvariant();
        var hoja = libro.Worksheets.FirstOrDefault(h => string.Equals(h.Name.Trim(), nombreHoja, StringComparison.OrdinalIgnoreCase));
        if (hoja is not null)
        {
            var datos = BuscarEnHoja(hoja, buscado, numeroAlbaranPlanta);
            if (datos is not null) return datos;
        }

        if (buscarTodasLasHojas)
        {
            foreach (var otraHoja in libro.Worksheets.Where(h => h != hoja))
            {
                var datos = BuscarEnHoja(otraHoja, buscado, numeroAlbaranPlanta);
                if (datos is not null) return datos;
            }
        }

        if (hoja is null)
            throw new InvalidOperationException($"El Excel de albaranes no contiene la hoja '{nombreHoja}'.");
        throw new InvalidOperationException($"El albarán de planta '{numeroAlbaranPlanta.Trim()}' no aparece en la hoja '{nombreHoja}'.");
    }

    private static DatosAlbaranPlanta? BuscarEnHoja(IXLWorksheet hoja, string buscado, string numeroAlbaranPlanta)
    {
        foreach (var fila in hoja.RowsUsed())
        {
            var numero = fila.Cell(1).GetFormattedString();
            if (!string.Equals(NormalizarNumero(numero), buscado, StringComparison.OrdinalIgnoreCase)) continue;
            return new DatosAlbaranPlanta(numeroAlbaranPlanta.Trim(), LeerFecha(fila.Cell(2)), LeerDecimal(fila.Cell(7)));
        }
        return null;
    }

    private static DateTime LeerFecha(IXLCell celda)
    {
        if (celda.TryGetValue<DateTime>(out var fecha)) return fecha.Date;
        if (DateTime.TryParse(celda.GetFormattedString(), CulturaEspanola, DateTimeStyles.AllowWhiteSpaces, out fecha)) return fecha.Date;
        throw new InvalidOperationException("La fecha de la columna B del albarán de planta no es válida.");
    }

    private static decimal LeerDecimal(IXLCell celda)
    {
        if (celda.TryGetValue<decimal>(out var valor)) return valor;
        if (decimal.TryParse(celda.GetFormattedString(), NumberStyles.Number, CulturaEspanola, out valor)) return valor;
        if (decimal.TryParse(celda.GetFormattedString(), NumberStyles.Number, CultureInfo.InvariantCulture, out valor)) return valor;
        throw new InvalidOperationException("El neto de Kg de la columna G del albarán de planta no es válido.");
    }

    private static string NormalizarNumero(string? valor)
    {
        var texto = (valor ?? string.Empty).Trim();
        return long.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero)
            ? numero.ToString(CultureInfo.InvariantCulture)
            : string.Concat(texto.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();
    }
}

public sealed record DatosAlbaranPlanta(string Numero, DateTime Fecha, decimal NetoKg)
{
    public string FechaTexto => Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    public string NetoKgTexto => NetoKg.ToString("0.###", Cultura);
    private static CultureInfo Cultura => CultureInfo.GetCultureInfo("es-ES");
}
