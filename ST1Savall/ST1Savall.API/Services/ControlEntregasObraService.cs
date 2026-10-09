using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Services;

/// <summary>Mantiene los cargos automáticos de los albaranes sobre el fondo de cada obra.</summary>
public sealed class ControlEntregasObraService(ApplicationDbContext application, SageGestionDbContext sage)
{
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static bool schemaReady;

    public async Task AsegurarEsquemaAsync(CancellationToken ct = default)
    {
        if (schemaReady) return;
        await SchemaLock.WaitAsync(ct);
        try
        {
            if (schemaReady) return;
            await application.Database.ExecuteSqlRawAsync(@"
                IF OBJECT_ID(N'MovimientosEntregaObra', N'U') IS NULL
                BEGIN
                    CREATE TABLE MovimientosEntregaObra (
                        IdMovimientoEntregaObra INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MovimientosEntregaObra PRIMARY KEY,
                        ObraCodigo CHAR(5) NOT NULL, Fecha DATETIME2 NOT NULL, Tipo NVARCHAR(20) NOT NULL,
                        Debe DECIMAL(18,2) NOT NULL CONSTRAINT DF_MovimientosEntregaObra_Debe DEFAULT 0,
                        Haber DECIMAL(18,2) NOT NULL CONSTRAINT DF_MovimientosEntregaObra_Haber DEFAULT 0,
                        Concepto NVARCHAR(250) NOT NULL, EmpresaAlbaran NVARCHAR(2) NULL, NumeroAlbaran NVARCHAR(12) NULL,
                        SerieAlbaran NVARCHAR(2) NULL, Activo BIT NOT NULL CONSTRAINT DF_MovimientosEntregaObra_Activo DEFAULT 1, Anulado BIT NOT NULL CONSTRAINT DF_MovimientosEntregaObra_Anulado DEFAULT 0,
                        CreadoEn DATETIME2 NOT NULL, CreadoPor NVARCHAR(256) NULL, ModificadoEn DATETIME2 NULL, ModificadoPor NVARCHAR(256) NULL
                    );
                    CREATE INDEX IX_MovimientosEntregaObra_Obra_Fecha ON MovimientosEntregaObra(ObraCodigo, Fecha, IdMovimientoEntregaObra);
                    CREATE INDEX IX_MovimientosEntregaObra_Albaran ON MovimientosEntregaObra(EmpresaAlbaran, NumeroAlbaran, SerieAlbaran);
                END
                IF COL_LENGTH('MovimientosEntregaObra', 'Activo') IS NULL
                    ALTER TABLE MovimientosEntregaObra ADD Activo BIT NOT NULL CONSTRAINT DF_MovimientosEntregaObra_Activo DEFAULT 1;", ct);
            schemaReady = true;
        }
        finally { SchemaLock.Release(); }
    }

    public async Task SincronizarAlbaranAsync(AlbaranVentaSage50 albaran, string? usuario, CancellationToken ct = default)
    {
        await AsegurarEsquemaAsync(ct);
        var empresa = Normalizar(albaran.EMPRESA);
        var numero = Normalizar(albaran.NUMERO);
        var serie = Normalizar(albaran.LETRA);
        if (string.IsNullOrEmpty(empresa) || string.IsNullOrEmpty(numero) || string.IsNullOrEmpty(serie)) return;

        var existentes = await application.MovimientosEntregaObra
            .Where(m => m.Tipo == "Albarán" && m.EmpresaAlbaran == empresa && m.NumeroAlbaran == numero && m.SerieAlbaran == serie)
            .ToListAsync(ct);
        var obra = Normalizar(albaran.OBRA);
        var ahora = DateTime.Now;

        if (string.IsNullOrEmpty(obra))
        {
            foreach (var movimiento in existentes.Where(m => !m.Anulado))
                Anular(movimiento, "Albarán sin obra", usuario, ahora);
            await application.SaveChangesAsync(ct);
            return;
        }

        // Los albaranes facturados no participan en el control de fondos de la obra.
        var estaFacturado = !string.IsNullOrWhiteSpace(albaran.FACTURA);
        if (estaFacturado)
        {
            foreach (var movimiento in existentes.Where(m => !m.Anulado))
                Anular(movimiento, "Albarán facturado", usuario, ahora);
            if (existentes.Count > 0) await application.SaveChangesAsync(ct);
            return;
        }

        var activo = existentes.FirstOrDefault(m => !m.Anulado && m.ObraCodigo.Trim() == obra);
        foreach (var movimiento in existentes.Where(m => !m.Anulado && m != activo))
            Anular(movimiento, "Albarán reasignado a otra obra", usuario, ahora);

        if (activo is null)
        {
            application.MovimientosEntregaObra.Add(new MovimientoEntregaObra
            {
                ObraCodigo = obra,
                Fecha = albaran.FECHA.Date,
                Tipo = "Albarán",
                Debe = Math.Round(albaran.TOTALDOC, 2, MidpointRounding.AwayFromZero),
                Haber = 0m,
                Concepto = $"Albarán {serie}-{numero}",
                EmpresaAlbaran = empresa,
                NumeroAlbaran = numero,
                SerieAlbaran = serie,
                CreadoEn = ahora,
                CreadoPor = usuario
            });
        }
        else
        {
            activo.Fecha = albaran.FECHA.Date;
            activo.Debe = Math.Round(albaran.TOTALDOC, 2, MidpointRounding.AwayFromZero);
            activo.Concepto = $"Albarán {serie}-{numero}";
            activo.ModificadoEn = ahora;
            activo.ModificadoPor = usuario;
        }
        await application.SaveChangesAsync(ct);
    }

    public async Task AnularAlbaranAsync(string empresa, string numero, string serie, string? usuario, CancellationToken ct = default)
    {
        await AsegurarEsquemaAsync(ct);
        var ahora = DateTime.Now;
        var movimientos = await application.MovimientosEntregaObra.Where(m => m.Tipo == "Albarán"
            && m.EmpresaAlbaran == Normalizar(empresa) && m.NumeroAlbaran == Normalizar(numero)
            && m.SerieAlbaran == Normalizar(serie) && !m.Anulado).ToListAsync(ct);
        foreach (var movimiento in movimientos) Anular(movimiento, "Albarán eliminado", usuario, ahora);
        if (movimientos.Count > 0) await application.SaveChangesAsync(ct);
    }

    public async Task SincronizarObraAsync(string obraCodigo, string? usuario, CancellationToken ct = default)
    {
        await AsegurarEsquemaAsync(ct);
        var obra = Normalizar(obraCodigo);
        if (string.IsNullOrEmpty(obra)) return;
        var albaranes = await sage.AlbaranesVenta.AsNoTracking().Where(a => a.OBRA.Trim() == obra).ToListAsync(ct);
        foreach (var albaran in albaranes) await SincronizarAlbaranAsync(albaran, usuario, ct);
    }

    public async Task<decimal> ObtenerSaldoDisponibleAsync(string obraCodigo, string? usuario, CancellationToken ct = default)
    {
        var obra = Normalizar(obraCodigo);
        if (string.IsNullOrEmpty(obra)) return decimal.MaxValue;
        await SincronizarObraAsync(obra, usuario, ct);
        return await application.MovimientosEntregaObra.AsNoTracking()
            .Where(m => m.ObraCodigo.Trim() == obra && !m.Anulado && m.Activo)
            .SumAsync(m => m.Haber - m.Debe, ct);
    }

    private static void Anular(MovimientoEntregaObra movimiento, string motivo, string? usuario, DateTime ahora)
    {
        movimiento.Anulado = true;
        movimiento.Activo = false;
        movimiento.ModificadoEn = ahora;
        movimiento.ModificadoPor = usuario;
        movimiento.Concepto = $"{movimiento.Concepto} · {motivo}";
    }

    private static string Normalizar(string? valor) => valor?.Trim().ToUpperInvariant() ?? string.Empty;
}
