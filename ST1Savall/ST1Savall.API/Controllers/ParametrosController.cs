using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParametrosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ParametrosController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Parametro>>> GetParametros()
    {
        var parametros = await _context.Parametros.AsNoTracking().OrderBy(p => p.Empresa).ToListAsync();
        foreach (var parametro in parametros) CargarSecretosPlanos(parametro);
        return parametros;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Parametro>> GetParametro(int id)
    {
        var parametro = await _context.Parametros.FindAsync(id);
        if (parametro is null) return NotFound();
        CargarSecretosPlanos(parametro);
        return parametro;
    }

    [HttpPost]
    public async Task<ActionResult<Parametro>> PostParametro(Parametro parametro)
    {
        GuardarSecretosPlanos(parametro);
        _context.Parametros.Add(parametro);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetParametro), new { id = parametro.Id }, parametro);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutParametro(int id, Parametro parametro)
    {
        if (id != parametro.Id) return BadRequest();

        var existente = await _context.Parametros.FindAsync(id);
        if (existente is null) return NotFound();

        // Los Excel se guardan al cargarlos. Un formulario que aún no ha recibido
        // sus rutas no debe borrar las que ya están persistidas.
        parametro.ExcelAlbaranesSabospaAlicante = ConservarRutaExcel(
            parametro.ExcelAlbaranesSabospaAlicante, existente.ExcelAlbaranesSabospaAlicante);
        parametro.ExcelAlbaranesSabospaFinestrat = ConservarRutaExcel(
            parametro.ExcelAlbaranesSabospaFinestrat, existente.ExcelAlbaranesSabospaFinestrat);
        parametro.ExcelAlbaranesSabospaMonforte = ConservarRutaExcel(
            parametro.ExcelAlbaranesSabospaMonforte, existente.ExcelAlbaranesSabospaMonforte);
        parametro.ExcelAlbaranesSabospaAlicanteNombre = ConservarRutaExcel(
            parametro.ExcelAlbaranesSabospaAlicanteNombre, existente.ExcelAlbaranesSabospaAlicanteNombre);
        parametro.ExcelAlbaranesSabospaFinestratNombre = ConservarRutaExcel(
            parametro.ExcelAlbaranesSabospaFinestratNombre, existente.ExcelAlbaranesSabospaFinestratNombre);
        parametro.ExcelAlbaranesSabospaMonforteNombre = ConservarRutaExcel(
            parametro.ExcelAlbaranesSabospaMonforteNombre, existente.ExcelAlbaranesSabospaMonforteNombre);
        ConservarSecretosPlanos(parametro, existente);

        _context.Entry(existente).CurrentValues.SetValues(parametro);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/excel-albaranes/{planta}")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<ArchivoExcelAlbaranesResponse>> SubirExcelAlbaranes(
        int id,
        string planta,
        IFormFile? archivo)
    {
        var parametro = await _context.Parametros.FindAsync(id);
        if (parametro is null)
            return NotFound(new { message = "No se encuentra el registro de parámetros." });

        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { message = "Seleccione un archivo de Excel." });

        if (!string.Equals(Path.GetExtension(archivo.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "El archivo debe tener extensión .xlsx." });

        var plantaNormalizada = planta.Trim().ToLowerInvariant();
        var nombreSeguro = plantaNormalizada switch
        {
            "alicante" => "sabospa-alicante.xlsx",
            "finestrat" => "sabospa-finestrat.xlsx",
            "monforte" => "sabospa-monforte.xlsx",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(nombreSeguro))
            return BadRequest(new { message = "La planta indicada no es válida." });

        var carpeta = Path.Combine(AppContext.BaseDirectory, "App_Data", "AlbaranesPlanta");
        Directory.CreateDirectory(carpeta);
        var rutaDestino = Path.Combine(carpeta, nombreSeguro);

        await using (var destino = System.IO.File.Create(rutaDestino))
        await using (var origen = archivo.OpenReadStream())
        {
            await origen.CopyToAsync(destino);
        }

        switch (plantaNormalizada)
        {
            case "alicante":
                parametro.ExcelAlbaranesSabospaAlicante = rutaDestino;
                parametro.ExcelAlbaranesSabospaAlicanteNombre = archivo.FileName;
                break;
            case "finestrat":
                parametro.ExcelAlbaranesSabospaFinestrat = rutaDestino;
                parametro.ExcelAlbaranesSabospaFinestratNombre = archivo.FileName;
                break;
            case "monforte":
                parametro.ExcelAlbaranesSabospaMonforte = rutaDestino;
                parametro.ExcelAlbaranesSabospaMonforteNombre = archivo.FileName;
                break;
        }

        await _context.SaveChangesAsync();
        return Ok(new ArchivoExcelAlbaranesResponse(rutaDestino, archivo.FileName));
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteParametro(int id)
    {
        var parametro = await _context.Parametros.FindAsync(id);
        if (parametro is null)
        {
            return NotFound();
        }

        _context.Parametros.Remove(parametro);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Los tokens se almacenan como texto plano por decisión de configuración.
    private static void CargarSecretosPlanos(Parametro parametro)
    {
        parametro.MapboxAccessToken = parametro.MapboxAccessTokenProtegido;
        parametro.WialonAccessToken = parametro.WialonAccessTokenProtegido;
        parametro.WialonPassword = parametro.WialonPasswordProtegida;
    }

    private static void ConservarSecretosPlanos(Parametro parametro, Parametro existente)
    {
        parametro.MapboxAccessTokenProtegido = string.IsNullOrWhiteSpace(parametro.MapboxAccessToken)
            ? existente.MapboxAccessTokenProtegido
            : parametro.MapboxAccessToken.Trim();
        parametro.WialonAccessTokenProtegido = string.IsNullOrWhiteSpace(parametro.WialonAccessToken)
            ? existente.WialonAccessTokenProtegido
            : parametro.WialonAccessToken.Trim();
        parametro.WialonPasswordProtegida = string.IsNullOrWhiteSpace(parametro.WialonPassword)
            ? existente.WialonPasswordProtegida
            : parametro.WialonPassword.Trim();
        parametro.MapboxAccessToken = null;
        parametro.WialonAccessToken = null;
        parametro.WialonPassword = null;
    }

    private static void GuardarSecretosPlanos(Parametro parametro)
    {
        parametro.MapboxAccessTokenProtegido = string.IsNullOrWhiteSpace(parametro.MapboxAccessToken) ? null : parametro.MapboxAccessToken.Trim();
        parametro.WialonAccessTokenProtegido = string.IsNullOrWhiteSpace(parametro.WialonAccessToken) ? null : parametro.WialonAccessToken.Trim();
        parametro.WialonPasswordProtegida = string.IsNullOrWhiteSpace(parametro.WialonPassword) ? null : parametro.WialonPassword.Trim();
        parametro.MapboxAccessToken = null;
        parametro.WialonAccessToken = null;
        parametro.WialonPassword = null;
    }
    private static string? ConservarRutaExcel(string? rutaFormulario, string? rutaExistente) =>
        string.IsNullOrWhiteSpace(rutaFormulario) ? rutaExistente : rutaFormulario;
    private bool ParametroExists(int id) => _context.Parametros.Any(p => p.Id == id);
}

public sealed record ArchivoExcelAlbaranesResponse(string Ruta, string NombreArchivo);
