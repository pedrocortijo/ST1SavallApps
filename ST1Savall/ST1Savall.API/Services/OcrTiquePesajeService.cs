using System.Globalization;
using System.Text.RegularExpressions;

namespace ST1Savall.API.Services;

public sealed record DatosTiqueExtraidos(
    string? NumeroAlbaran,
    string? Matricula,
    DateTime? Fecha,
    TimeSpan? Hora,
    int? PesoNetoKg);

public sealed class TiquePesajeParser
{
    private static readonly Regex NumeroAlbaranRegex = new(
        @"(?:albar[aá]n|ticket|tique|(?:n[uú]mero|n[º°o]|num\.?)\s*(?:de\s*)?pesada)(?:\s*(?:n[º°o]?|n[uú]mero))?\s*[:#\-]?\s*([A-Z0-9][A-Z0-9./\-]{1,19})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MatriculaModernaRegex = new(
        @"\b(\d{4}\s*[- ]?\s*[BCDFGHJKLMNPRSTVWXYZ]{3})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MatriculaAntiguaRegex = new(
        @"\b([A-Z]{1,2}\s*[- ]?\s*\d{4}\s*[- ]?\s*[A-Z]{1,2})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FechaRegex = new(
        @"\b(\d{1,2}[./-]\d{1,2}[./-]\d{2,4}|\d{4}[./-]\d{1,2}[./-]\d{1,2})\b",
        RegexOptions.Compiled);
    private static readonly Regex HoraRegex = new(
        @"\b([01]?\d|2[0-3])[:.]([0-5]\d)(?:[:.]([0-5]\d))?\b",
        RegexOptions.Compiled);
    private static Regex PatronPeso(string etiqueta) => new(
        @"\b(?:peso\s*)?(?:" + etiqueta + @")\b\s*[:#\-]?\s*(\d{1,3}(?:[. ]\d{3})+(?:,\d+)?|\d{1,6}(?:[,.]\d+)?)(?![\d.,])\s*(kgs?|kilos?|tn|t)?\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PesoNetoRegex = PatronPeso("net[o0]");
    private static readonly Regex PesoBrutoRegex = PatronPeso("brut[o0]");
    private static readonly Regex PesoTaraRegex = PatronPeso("tara");

    private static Regex PatronPesoInverso(string etiqueta) => new(
        @"(?<![\d.,])(\d{1,3}(?:[. ]\d{3})+(?:,\d+)?|\d{1,6}(?:[,.]\d+)?)(?![\d.,])\s*(kgs?|kilos?|tn|t)\b\s*(?:peso\s*)?(?:" + etiqueta + @")\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NetoInversoRegex = PatronPesoInverso("net[o0]");
    private static readonly Regex BrutoInversoRegex = PatronPesoInverso("brut[o0]");
    private static readonly Regex TaraInversaRegex = PatronPesoInverso("tara");

    public DatosTiqueExtraidos Extraer(string texto)
    {
        var lineas = texto.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        var todo = Regex.Replace(string.Join(" ", lineas), @"\s+", " ");

        var numero = NumeroAlbaranRegex.Match(todo).Groups[1].Value;
        var matriculaMatch = MatriculaModernaRegex.Match(todo);
        if (!matriculaMatch.Success) matriculaMatch = MatriculaAntiguaRegex.Match(todo);
        var matricula = matriculaMatch.Success ? NormalizarMatricula(matriculaMatch.Groups[1].Value) : null;

        DateTime? fecha = null;
        var fechaMatch = FechaRegex.Match(todo);
        if (fechaMatch.Success)
        {
            var formatos = new[] { "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy", "d.M.yyyy", "dd.MM.yyyy", "yyyy-M-d", "yyyy-MM-dd" };
            if (DateTime.TryParseExact(fechaMatch.Value, formatos, CultureInfo.GetCultureInfo("es-ES"),
                DateTimeStyles.None, out var valorFecha))
                fecha = valorFecha.Date;
        }

        TimeSpan? hora = null;
        var horaMatch = HoraRegex.Match(todo);
        if (horaMatch.Success && int.TryParse(horaMatch.Groups[1].Value, out var h)
            && int.TryParse(horaMatch.Groups[2].Value, out var m))
        {
            var s = int.TryParse(horaMatch.Groups[3].Value, out var segundos) ? segundos : 0;
            hora = new TimeSpan(h, m, s);
        }

        // Los tiques pueden repetir el resumen de pesos; se toma el último
        // valor válido que figure expresamente junto a «Peso neto».
        var peso = ExtraerPeso(todo, PesoNetoRegex);
        var bruto = ExtraerPeso(todo, PesoBrutoRegex);
        var tara = ExtraerPeso(todo, PesoTaraRegex);
        // Cuando están los tres pesos, una lectura contradictoria requiere revisión.
        // Si falta únicamente el neto, se puede obtener de dos pesos etiquetados.
        if (bruto.HasValue && tara.HasValue)
        {
            var calculado = bruto.Value - tara.Value;
            peso = calculado > 0 && (!peso.HasValue || peso.Value == calculado)
                ? calculado : null;
        }

        // Algunos motores devuelven las líneas de abajo hacia arriba:
        // «3780 kg Peso neto: 8760 kg Peso tara: 12540 kg Peso bruto:».
        // Solo aceptamos esa orientación con los tres pesos y su comprobación;
        // buscar simplemente un número anterior confundiría tiques normales.
        var netoInverso = ExtraerPeso(todo, NetoInversoRegex);
        var brutoInverso = ExtraerPeso(todo, BrutoInversoRegex);
        var taraInversa = ExtraerPeso(todo, TaraInversaRegex);
        if (netoInverso.HasValue && brutoInverso.HasValue && taraInversa.HasValue)
        {
            peso = brutoInverso.Value - taraInversa.Value == netoInverso.Value
                ? netoInverso : null;
        }

        return new DatosTiqueExtraidos(
            string.IsNullOrWhiteSpace(numero) ? null : numero.Trim(), matricula, fecha, hora, peso);
    }

    public static string NormalizarMatricula(string? matricula) =>
        string.Concat((matricula ?? string.Empty).Where(char.IsLetterOrDigit)).ToUpperInvariant();

    private static int? ExtraerPeso(string texto, Regex patron)
    {
        var coincidencias = patron.Matches(texto);
        for (var indice = coincidencias.Count - 1; indice >= 0; indice--)
        {
            var peso = ConvertirPeso(coincidencias[indice]);
            if (peso.HasValue) return peso;
        }

        return null;
    }
    private static int? ConvertirPeso(Match match)
    {
        var texto = match.Groups[1].Value.Replace(" ", string.Empty);
        // Punto de millares (12.540 kg) frente a decimal (3.78 t).
        if (Regex.IsMatch(texto, @"^\d{1,3}(?:\.\d{3})+(?:,\d+)?$"))
            texto = texto.Replace(".", string.Empty);
        if (!decimal.TryParse(texto.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var valor))
            return null;
        if (match.Groups[2].Value.Equals("t", StringComparison.OrdinalIgnoreCase)
            || match.Groups[2].Value.Equals("tn", StringComparison.OrdinalIgnoreCase)) valor *= 1000;
        return valor is > 0 and <= 100000
            ? decimal.ToInt32(decimal.Round(valor, 0, MidpointRounding.AwayFromZero)) : null;
    }
}
