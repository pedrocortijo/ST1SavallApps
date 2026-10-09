using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.API.Services;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/entregas-obra")]
public sealed class EntregasObraController(ApplicationDbContext context, ControlEntregasObraService control) : ControllerBase
{
    [HttpGet("saldo-control/{obraCodigo}")]
    public async Task<ActionResult<decimal>> GetSaldoControl(string obraCodigo, CancellationToken ct)
    {
        await control.AsegurarEsquemaAsync(ct);
        var codigo = Normalizar(obraCodigo);
        if (string.IsNullOrEmpty(codigo)) return NoContent();

        // Una obra entra en el control únicamente al registrar una entrega a cuenta.
        // Los albaranes por sí solos no deben hacer aparecer el saldo en Solicitudes.
        var tieneControl = await context.MovimientosEntregaObra.AsNoTracking().AnyAsync(m =>
            m.ObraCodigo.Trim() == codigo && m.Tipo == "Entrega" && !m.Anulado, ct);
        if (!tieneControl) return NoContent();

        await control.SincronizarObraAsync(codigo, User.Identity?.Name, ct);
        var saldo = await context.MovimientosEntregaObra.AsNoTracking()
            .Where(m => m.ObraCodigo.Trim() == codigo && !m.Anulado && m.Activo)
            .SumAsync(m => m.Haber - m.Debe, ct);
        return Ok(saldo);
    }

    [HttpGet("por-obra/{obraCodigo}")]
    public async Task<ActionResult<ResumenEntregasObra>> Get(string obraCodigo, CancellationToken ct)
    {
        await control.AsegurarEsquemaAsync(ct);
        var codigo = Normalizar(obraCodigo);
        await control.SincronizarObraAsync(codigo, User.Identity?.Name, ct);
        var movimientos = await context.MovimientosEntregaObra.AsNoTracking().Where(m => m.ObraCodigo.Trim() == codigo)
            .OrderBy(m => m.Fecha).ThenBy(m => m.CreadoEn).ThenBy(m => m.IdMovimientoEntregaObra).ToListAsync(ct);
        // Las entregas anuladas y los albaranes facturados se conservan internamente
        // para auditoría, pero no forman parte del extracto visible.
        movimientos = movimientos.Where(m =>
            !(m.Tipo == "Entrega" && m.Anulado)
            && !m.Concepto.Contains("Facturado", StringComparison.OrdinalIgnoreCase)).ToList();
        var vigentes = movimientos.Where(m => !m.Anulado && m.Activo).ToList();
        return Ok(new ResumenEntregasObra { Movimientos = movimientos, TotalEntregado = vigentes.Sum(m => m.Haber), TotalConsumido = vigentes.Sum(m => m.Debe), Saldo = vigentes.Sum(m => m.Haber - m.Debe) });
    }

    [HttpPost("por-obra/{obraCodigo}")]
    public async Task<ActionResult<MovimientoEntregaObra>> CrearEntrega(string obraCodigo, NuevaEntregaObra datos, CancellationToken ct)
    {
        await control.AsegurarEsquemaAsync(ct);
        var codigo = Normalizar(obraCodigo);
        if (codigo.Length != 5) return BadRequest(new { message = "El código de obra debe tener 5 caracteres." });
        if (datos.Importe <= 0 || string.IsNullOrWhiteSpace(datos.Concepto)) return BadRequest(new { message = "Indique un importe positivo y un concepto." });
        var movimiento = new MovimientoEntregaObra { ObraCodigo = codigo, Fecha = datos.Fecha.Date, Tipo = "Entrega", Haber = Math.Round(datos.Importe, 2, MidpointRounding.AwayFromZero), Concepto = datos.Concepto.Trim(), CreadoEn = DateTime.Now, CreadoPor = User.Identity?.Name };
        context.MovimientosEntregaObra.Add(movimiento);
        await context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { obraCodigo = codigo }, movimiento);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarEntrega(int id, NuevaEntregaObra datos, CancellationToken ct)
    {
        await control.AsegurarEsquemaAsync(ct);
        var movimiento = await context.MovimientosEntregaObra.FindAsync([id], ct);
        if (movimiento is null) return NotFound();
        if (movimiento.Tipo != "Entrega" || movimiento.Anulado)
            return BadRequest(new { message = "Solo se pueden editar entregas manuales vigentes." });
        if (datos.Importe <= 0 || string.IsNullOrWhiteSpace(datos.Concepto))
            return BadRequest(new { message = "Indique un importe positivo y un concepto." });
        movimiento.Fecha = datos.Fecha.Date;
        movimiento.Haber = Math.Round(datos.Importe, 2, MidpointRounding.AwayFromZero);
        movimiento.Concepto = datos.Concepto.Trim();
        movimiento.ModificadoEn = DateTime.Now;
        movimiento.ModificadoPor = User.Identity?.Name;
        await context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> EliminarEntrega(int id, CancellationToken ct)
    {
        await control.AsegurarEsquemaAsync(ct);
        var movimiento = await context.MovimientosEntregaObra.FindAsync([id], ct);
        if (movimiento is null) return NotFound();
        if (movimiento.Tipo != "Entrega")
            return BadRequest(new { message = "Solo se pueden borrar entregas manuales." });
        context.MovimientosEntregaObra.Remove(movimiento);
        await context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPut("{id:int}/activo")]
    public async Task<IActionResult> CambiarEstado(int id, EstadoMovimientoEntregaObra datos, CancellationToken ct)
    {
        await control.AsegurarEsquemaAsync(ct);
        var movimiento = await context.MovimientosEntregaObra.FindAsync([id], ct);
        if (movimiento is null) return NotFound();
        if (movimiento.Tipo != "Entrega" || movimiento.Anulado)
            return BadRequest(new { message = "Solo se pueden activar o desactivar entregas manuales vigentes." });
        movimiento.Activo = datos.Activo;
        movimiento.ModificadoEn = DateTime.Now;
        movimiento.ModificadoPor = User.Identity?.Name;
        await context.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string Normalizar(string? valor) => valor?.Trim().ToUpperInvariant() ?? string.Empty;

}
