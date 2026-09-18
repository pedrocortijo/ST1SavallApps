using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.API.Services;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/periodicidades-obra")]
public class PeriodicidadesObraController(ApplicationDbContext context, PeriodicidadObraService service) : ControllerBase
{
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static bool schemaReady;

    [HttpPost("sincronizar")]
    public async Task<ActionResult<List<PeriodicidadObra>>> Sincronizar()
    {
        await EnsureSchemaAsync();
        await service.SincronizarAsync(HttpContext.RequestAborted);
        var pendientes = await context.PeriodicidadesObra.AsNoTracking()
            .Where(p => p.Activa && p.PendienteConfirmacion && p.ProximaFecha.HasValue)
            .OrderBy(p => p.ProximaFecha)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(pendientes);
    }
    [HttpGet("activas")]
    public async Task<ActionResult<List<PeriodicidadObra>>> GetActivas()
    {
        await EnsureSchemaAsync();
        return Ok(await context.PeriodicidadesObra.AsNoTracking()
            .Where(p => p.Activa)
            .OrderBy(p => p.ObraCodigo)
            .ToListAsync());
    }
    [HttpGet("{obraCodigo}")]
    public async Task<ActionResult<PeriodicidadObra>> Get(string obraCodigo)
    {
        await EnsureSchemaAsync();
        var periodicidad = await context.PeriodicidadesObra.AsNoTracking().FirstOrDefaultAsync(p => p.ObraCodigo.Trim() == obraCodigo.Trim());
        return Ok(periodicidad ?? new PeriodicidadObra { ObraCodigo = obraCodigo.Trim() });
    }

    [HttpGet("{obraCodigo}/ejecuciones")]
    public async Task<ActionResult<List<PeriodicidadObraEjecucion>>> GetEjecuciones(string obraCodigo)
    {
        await EnsureSchemaAsync();
        return Ok(await context.PeriodicidadesObraEjecuciones.AsNoTracking()
            .Where(e => e.Periodicidad!.ObraCodigo.Trim() == obraCodigo.Trim())
            .OrderByDescending(e => e.FechaPrevista).Take(20).ToListAsync());
    }

    [HttpGet("pendientes-confirmacion")]
    public async Task<ActionResult<List<PeriodicidadObra>>> GetPendientesConfirmacion()
    {
        await EnsureSchemaAsync();
        return Ok(await context.PeriodicidadesObra.AsNoTracking()
            .Where(p => p.Activa && p.PendienteConfirmacion && p.ProximaFecha.HasValue)
            .OrderBy(p => p.ProximaFecha).ToListAsync());
    }

    [HttpPost("{idPeriodicidadObra:int}/confirmar")]
    public async Task<IActionResult> Confirmar(int idPeriodicidadObra)
    {
        await EnsureSchemaAsync();
        var periodicidad = await context.PeriodicidadesObra.FindAsync(idPeriodicidadObra);
        if (periodicidad == null) return NotFound();
        if (!periodicidad.PendienteConfirmacion) return Conflict(new { message = "Esta periodicidad no está pendiente de confirmación." });
        var idSolicitud = await service.GenerarSiguienteAsync(idPeriodicidadObra);
        return Ok(new { idSolicitud });
    }
    [HttpPut("{obraCodigo}")]
    public async Task<ActionResult<PeriodicidadObra>> Put(string obraCodigo, PeriodicidadObra entrada)
    {
        await EnsureSchemaAsync();
        if (entrada.Activa && (entrada.Cantidad < 1 || entrada.IdTipoTarea <= 0)) return BadRequest(new { message = "Indique una cantidad válida y el tipo de tarea." });
        if (entrada.Activa && entrada.TipoInicio == InicioPeriodicidad.FechaManual && !entrada.FechaInicioManual.HasValue)
            return BadRequest(new { message = "Indique la fecha de inicio manual." });
        var codigo = obraCodigo.Trim();
        var existente = await context.PeriodicidadesObra.FirstOrDefaultAsync(p => p.ObraCodigo.Trim() == codigo);
        var nueva = existente == null;
        existente ??= new PeriodicidadObra { ObraCodigo = codigo };
        existente.Activa = entrada.Activa;
        existente.Unidad = entrada.Unidad;
        existente.Cantidad = entrada.Cantidad;
        existente.L = entrada.L; existente.M = entrada.M; existente.X = entrada.X; existente.J = entrada.J; existente.V = entrada.V; existente.S = entrada.S; existente.D = entrada.D;
        existente.HoraPrevista = entrada.HoraPrevista;
        existente.TipoInicio = entrada.TipoInicio;
        existente.FechaInicioManual = entrada.TipoInicio == InicioPeriodicidad.FechaManual ? entrada.FechaInicioManual?.Date : null;
        existente.IdTipoTarea = entrada.IdTipoTarea;
        existente.FechaActualizacion = DateTime.UtcNow;
        if (nueva) context.PeriodicidadesObra.Add(existente);
        var pendiente = !nueva && await context.PeriodicidadesObraEjecuciones.AnyAsync(e => e.IdPeriodicidadObra == existente.IdPeriodicidadObra &&
            (e.Estado == EstadoEjecucionPeriodicidad.Pendiente || e.Estado == EstadoEjecucionPeriodicidad.Generada));
        if (!pendiente)
            existente.ProximaFecha = existente.TipoInicio == InicioPeriodicidad.FechaManual ? existente.FechaInicioManual :
                existente.FechaUltimaRealizada is DateTime ultima ? PeriodicidadObraService.SumarPeriodo(ultima, existente) : null;
        await context.SaveChangesAsync();
        if (existente.Activa) await service.MarcarPendienteConfirmacionAsync(existente.IdPeriodicidadObra);
        return Ok(existente);
    }

    private async Task EnsureSchemaAsync()
    {
        if (schemaReady) return;
        await SchemaLock.WaitAsync();
        try
        {
            if (schemaReady) return;
            await context.Database.ExecuteSqlRawAsync(@"
                IF OBJECT_ID(N'PeriodicidadesObra', N'U') IS NULL
                BEGIN
                    CREATE TABLE PeriodicidadesObra (
                        IdPeriodicidadObra INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PeriodicidadesObra PRIMARY KEY,
                        ObraCodigo VARCHAR(5) NOT NULL, Activa BIT NOT NULL CONSTRAINT DF_PeriodicidadesObra_Activa DEFAULT 0, PendienteConfirmacion BIT NOT NULL CONSTRAINT DF_PeriodicidadesObra_PendienteConfirmacion DEFAULT 0,
                        Unidad INT NOT NULL, Cantidad INT NOT NULL CONSTRAINT DF_PeriodicidadesObra_Cantidad DEFAULT 1, L INT NOT NULL CONSTRAINT DF_PeriodicidadesObra_L DEFAULT 0, M INT NOT NULL CONSTRAINT DF_PeriodicidadesObra_M DEFAULT 0, X INT NOT NULL CONSTRAINT DF_PeriodicidadesObra_X DEFAULT 0, J INT NOT NULL CONSTRAINT DF_PeriodicidadesObra_J DEFAULT 0, V INT NOT NULL CONSTRAINT DF_PeriodicidadesObra_V DEFAULT 0, S INT NOT NULL CONSTRAINT DF_PeriodicidadesObra_S DEFAULT 0, D INT NOT NULL CONSTRAINT DF_PeriodicidadesObra_D DEFAULT 0,
                        HoraPrevista TIME NOT NULL, TipoInicio INT NOT NULL, FechaInicioManual DATE NULL,
                        FechaUltimaRealizada DATE NULL, ProximaFecha DATE NULL, IdTipoTarea INT NOT NULL,
                        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_PeriodicidadesObra_FechaCreacion DEFAULT SYSUTCDATETIME(), FechaActualizacion DATETIME2 NULL
                    );
                    CREATE UNIQUE INDEX IX_PeriodicidadesObra_ObraCodigo ON PeriodicidadesObra(ObraCodigo);
                END
                IF OBJECT_ID(N'PeriodicidadesObraEjecuciones', N'U') IS NULL
                BEGIN
                    CREATE TABLE PeriodicidadesObraEjecuciones (
                        IdPeriodicidadObraEjecucion INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PeriodicidadesObraEjecuciones PRIMARY KEY,
                        IdPeriodicidadObra INT NOT NULL, FechaPrevista DATE NOT NULL, HoraPrevista TIME NOT NULL, IdSolicitud INT NULL,
                        Estado INT NOT NULL CONSTRAINT DF_PeriodicidadesObraEjecuciones_Estado DEFAULT 1, FechaRealizacion DATETIME2 NULL,
                        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_PeriodicidadesObraEjecuciones_FechaCreacion DEFAULT SYSUTCDATETIME(),
                        CONSTRAINT FK_PeriodicidadesObraEjecuciones_PeriodicidadesObra FOREIGN KEY (IdPeriodicidadObra) REFERENCES PeriodicidadesObra(IdPeriodicidadObra) ON DELETE CASCADE
                    );
                    CREATE UNIQUE INDEX IX_PeriodicidadesObraEjecuciones_IdPeriodicidadObra_FechaPrevista ON PeriodicidadesObraEjecuciones(IdPeriodicidadObra, FechaPrevista);
                END
                IF COL_LENGTH(N'PeriodicidadesObra', N'PendienteConfirmacion') IS NULL
                    ALTER TABLE PeriodicidadesObra ADD PendienteConfirmacion BIT NOT NULL CONSTRAINT DF_PeriodicidadesObra_PendienteConfirmacion DEFAULT 0;
                IF COL_LENGTH(N'PeriodicidadesObra', N'L') IS NULL
                BEGIN
                    ALTER TABLE PeriodicidadesObra ADD L INT NOT NULL CONSTRAINT DF_PeriodicidadesObra_L DEFAULT 0, M INT NOT NULL CONSTRAINT DF_PeriodicidadesObra_M DEFAULT 0, X INT NOT NULL CONSTRAINT DF_PeriodicidadesObra_X DEFAULT 0, J INT NOT NULL CONSTRAINT DF_PeriodicidadesObra_J DEFAULT 0, V INT NOT NULL CONSTRAINT DF_PeriodicidadesObra_V DEFAULT 0, S INT NOT NULL CONSTRAINT DF_PeriodicidadesObra_S DEFAULT 0, D INT NOT NULL CONSTRAINT DF_PeriodicidadesObra_D DEFAULT 0;
                    EXEC(N'UPDATE PeriodicidadesObra SET L = CASE WHEN DiaSemana = 1 THEN 1 ELSE 0 END, M = CASE WHEN DiaSemana = 2 THEN 1 ELSE 0 END, X = CASE WHEN DiaSemana = 3 THEN 1 ELSE 0 END, J = CASE WHEN DiaSemana = 4 THEN 1 ELSE 0 END, V = CASE WHEN DiaSemana = 5 THEN 1 ELSE 0 END, S = CASE WHEN DiaSemana = 6 THEN 1 ELSE 0 END, D = CASE WHEN DiaSemana = 7 THEN 1 ELSE 0 END;')
                END
                IF COL_LENGTH(N'Solicitudes', N'IdPeriodicidadObraEjecucion') IS NULL
                    ALTER TABLE Solicitudes ADD IdPeriodicidadObraEjecucion INT NULL;
                SET ANSI_NULLS ON;
                SET QUOTED_IDENTIFIER ON;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Solicitudes_IdPeriodicidadObraEjecucion' AND object_id = OBJECT_ID(N'Solicitudes'))
                    EXEC(N'CREATE UNIQUE INDEX IX_Solicitudes_IdPeriodicidadObraEjecucion ON Solicitudes(IdPeriodicidadObraEjecucion) WHERE IdPeriodicidadObraEjecucion IS NOT NULL');");
            schemaReady = true;
        }
        finally
        {
            SchemaLock.Release();
        }
    }
}