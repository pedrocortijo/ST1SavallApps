using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ObrasComunSage50Controller : ControllerBase
{
    private readonly SageComunDbContext _context;
    private readonly ApplicationDbContext _applicationContext;

    public ObrasComunSage50Controller(SageComunDbContext context, ApplicationDbContext applicationContext)
    {
        _context = context;
        _applicationContext = applicationContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ObraComunSage50>>> GetObras()
    {
        return await _context.Obras.ToListAsync();
    }

    [HttpGet("{codigo}")]
    public async Task<ActionResult<ObraComunSage50>> GetObra(string codigo)
    {
        var obra = await _context.Obras.FindAsync(codigo);
        if (obra == null) return NotFound();
        return obra;
    }

    [HttpGet("servicios-asignados/{codigo}")]
    public async Task<ActionResult<bool>> TieneServiciosAsignados(string codigo)
    {
        var idObra = ParseCodigoToInt(codigo);
        return await _applicationContext.Solicitudes.AnyAsync(s => s.IdCliente == idObra);
    }

    [HttpPost]
    public async Task<ActionResult<ObraComunSage50>> PostObra(ObraComunSage50 obra)
    {
        if (ObraExists(obra.Codigo))
        {
            return Conflict(new { message = $"Ya existe una obra con el código '{obra.Codigo.Trim()}'." });
        }

        _context.Obras.Add(obra);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            if (ObraExists(obra.Codigo))
            {
                return Conflict(new { message = $"Ya existe una obra con el código '{obra.Codigo.Trim()}'." });
            }
            return BadRequest(new { message = ex.GetBaseException().Message });
        }
        return CreatedAtAction(nameof(GetObra), new { codigo = obra.Codigo }, obra);
    }

    [HttpPut("{codigo}")]
    public async Task<IActionResult> PutObra(string codigo, ObraComunSage50 obra)
    {
        if (codigo != obra.Codigo) return BadRequest();

        var obraActual = await _context.Obras.AsNoTracking().FirstOrDefaultAsync(o => o.Codigo == codigo);
        if (obraActual == null) return NotFound();

        var clienteActual = obraActual.Cliente?.Trim() ?? string.Empty;
        var clienteNuevo = obra.Cliente?.Trim() ?? string.Empty;
        if (!string.Equals(clienteActual, clienteNuevo, StringComparison.OrdinalIgnoreCase)
            && await _applicationContext.Solicitudes.AnyAsync(s => s.IdCliente == ParseCodigoToInt(codigo)))
        {
            return Conflict(new { message = "No se puede cambiar el cliente porque la obra ya tiene servicios asignados." });
        }

        _context.Entry(obra).State = EntityState.Modified;
        var entry = _context.Entry(obra);
        entry.Property(o => o.Descuento).IsModified = false;
        entry.Property(o => o.Fax).IsModified = false;
        entry.Property(o => o.Fpag).IsModified = false;
        entry.Property(o => o.Isp).IsModified = false;
        entry.Property(o => o.Marvehic).IsModified = false;
        entry.Property(o => o.Modvehic).IsModified = false;
        entry.Property(o => o.Observacio).IsModified = false;
        entry.Property(o => o.Password).IsModified = false;
        entry.Property(o => o.Pp).IsModified = false;
        entry.Property(o => o.Ruta).IsModified = false;
        entry.Property(o => o.Tarifa).IsModified = false;
        entry.Property(o => o.Vendedor).IsModified = false;
        entry.Property(o => o.Zona).IsModified = false;
        try
        {
            await _context.SaveChangesAsync();
            await ActualizarBloqueoSolicitudesDeObraAsync(ParseCodigoToInt(codigo), obra.Posicion == 1);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ObraExists(codigo)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{codigo}")]
    public async Task<IActionResult> DeleteObra(string codigo)
    {
        var obra = await _context.Obras.FindAsync(codigo);
        if (obra == null) return NotFound();
        _context.Obras.Remove(obra);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool ObraExists(string codigo)
    {
        return _context.Obras.Any(e => e.Codigo == codigo);
    }

    private static int ParseCodigoToInt(string codigo)
    {
        var cleaned = codigo?.Trim() ?? string.Empty;
        if (int.TryParse(cleaned, out var value)) return value;

        unchecked
        {
            uint hash = 2166136261;
            foreach (var character in cleaned)
            {
                hash ^= character;
                hash *= 16777619;
            }
            return Math.Abs((int)hash);
        }
    }

    private async Task ActualizarBloqueoSolicitudesDeObraAsync(int idObra, bool bloqueado)
    {
        var solicitudes = await _applicationContext.Solicitudes
            .Where(s => s.IdCliente == idObra && s.Estado != 5 && s.Estado != 6)
            .ToListAsync();

        if (solicitudes.Count > 0)
        {
            foreach (var s in solicitudes)
            {
                s.Bloqueado = bloqueado;
            }
            await _applicationContext.SaveChangesAsync();
        }
    }
}
