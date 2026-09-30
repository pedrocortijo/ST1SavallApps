using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/obras-ovp")]
public class ObrasOVPController(ApplicationDbContext context) : ControllerBase
{
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static bool schemaReady;

    [HttpGet("por-obra/{idObra}")]
    public async Task<ActionResult<List<ObrasOVP>>> GetPorObra(string idObra)
    {
        await EnsureSchemaAsync();
        var codigo = NormalizarCodigo(idObra);
        return Ok(await context.ObrasOVP.AsNoTracking()
            .Where(o => o.IdObra.Trim() == codigo)
            .OrderByDescending(o => o.FechaInicioOVP)
            .ThenByDescending(o => o.IdObrasOVP)
            .ToListAsync());
    }

    [HttpPost]
    public async Task<ActionResult<ObrasOVP>> Post(ObrasOVP obraOvp)
    {
        await EnsureSchemaAsync();
        Normalizar(obraOvp);
        if (obraOvp.IdObra.Length != 5)
            return BadRequest(new { message = "La obra debe tener un código de 5 caracteres." });

        try
        {
            obraOvp.PathOVP = await TrasladarPdfACarpetaOvpAsync(obraOvp.PathOVP);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return BadRequest(new { message = $"No se ha podido trasladar el PDF OVP: {ex.Message}" });
        }

        context.ObrasOVP.Add(obraOvp);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPorObra), new { idObra = obraOvp.IdObra }, obraOvp);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, ObrasOVP datos)
    {
        await EnsureSchemaAsync();
        if (id != datos.IdObrasOVP) return BadRequest(new { message = "El registro OVP no coincide con la ruta solicitada." });

        var actual = await context.ObrasOVP.FindAsync(id);
        if (actual is null) return NotFound();

        Normalizar(datos);
        try
        {
            datos.PathOVP = await TrasladarPdfACarpetaOvpAsync(datos.PathOVP);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return BadRequest(new { message = $"No se ha podido trasladar el PDF OVP: {ex.Message}" });
        }

        actual.RequiereOVP = datos.RequiereOVP;
        actual.NumLicenciaOVP = datos.NumLicenciaOVP;
        actual.FechaInicioOVP = datos.FechaInicioOVP;
        actual.FechaFinOVP = datos.FechaFinOVP;
        actual.PathOVP = datos.PathOVP;
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await EnsureSchemaAsync();
        var actual = await context.ObrasOVP.FindAsync(id);
        if (actual is null) return NotFound();

        context.ObrasOVP.Remove(actual);
        await context.SaveChangesAsync();
        return NoContent();
    }

    private static string NormalizarCodigo(string? codigo) => (codigo ?? string.Empty).Trim();

    private static void Normalizar(ObrasOVP obraOvp)
    {
        obraOvp.IdObra = NormalizarCodigo(obraOvp.IdObra);
        obraOvp.NumLicenciaOVP = obraOvp.NumLicenciaOVP?.Trim();
        obraOvp.PathOVP = obraOvp.PathOVP?.Trim();
    }

    private async Task<string?> TrasladarPdfACarpetaOvpAsync(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta)) return null;
        if (!string.Equals(Path.GetExtension(ruta), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("El documento OVP debe ser un archivo PDF.");

        var carpetaConfigurada = await context.Parametros.AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => p.PathDocumentosOVP)
            .FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(carpetaConfigurada)) return ruta;

        var origen = Path.GetFullPath(ruta);
        if (!System.IO.File.Exists(origen))
            return ruta;

        var carpetaDestino = Path.GetFullPath(carpetaConfigurada.Trim());
        var prefijoDestino = carpetaDestino.EndsWith(Path.DirectorySeparatorChar)
            ? carpetaDestino
            : carpetaDestino + Path.DirectorySeparatorChar;
        if (origen.StartsWith(prefijoDestino, StringComparison.OrdinalIgnoreCase)) return origen;

        Directory.CreateDirectory(carpetaDestino);
        var nombre = Path.GetFileName(origen);
        var destino = Path.Combine(carpetaDestino, nombre);
        if (System.IO.File.Exists(destino))
        {
            var nombreSinExtension = Path.GetFileNameWithoutExtension(nombre);
            destino = Path.Combine(carpetaDestino, $"{nombreSinExtension}_{Guid.NewGuid():N}.pdf");
        }

        System.IO.File.Move(origen, destino);
        return destino;
    }

    private async Task EnsureSchemaAsync()
    {
        if (schemaReady) return;
        await SchemaLock.WaitAsync();
        try
        {
            if (schemaReady) return;
            await context.Database.ExecuteSqlRawAsync(@"
                IF OBJECT_ID(N'ObrasOVP', N'U') IS NULL
                BEGIN
                    CREATE TABLE ObrasOVP (
                        IdObrasOVP INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ObrasOVP PRIMARY KEY,
                        IdObra CHAR(5) NOT NULL,
                        RequiereOVP BIT NOT NULL CONSTRAINT DF_ObrasOVP_RequiereOVP DEFAULT 0,
                        NumLicenciaOVP NVARCHAR(50) COLLATE Modern_Spanish_CI_AS NULL,
                        FechaInicioOVP DATETIME2(7) NULL,
                        FechaFinOVP DATETIME2(7) NULL,
                        PathOVP VARCHAR(255) NULL
                    );
                    CREATE INDEX IX_ObrasOVP_IdObra ON ObrasOVP(IdObra);
                END");
            schemaReady = true;
        }
        finally
        {
            SchemaLock.Release();
        }
    }
}
