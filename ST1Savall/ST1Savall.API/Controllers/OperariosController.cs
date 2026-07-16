using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OperariosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OperariosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Operario>>> GetOperarios()
    {
        return await _context.Operarios.Include(o => o.Cargo).Include(o => o.Planta).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Operario>> GetOperario(int id)
    {
        var operario = await _context.Operarios.Include(o => o.Cargo).Include(o => o.Planta).FirstOrDefaultAsync(o => o.IdOperario == id);
        if (operario == null) return NotFound();
        return operario;
    }

    [HttpPost]
    public async Task<ActionResult<Operario>> PostOperario(Operario operario)
    {
        var validationError = NormalizarYValidarDisponibilidad(operario);
        if (validationError != null) return BadRequest(new { message = validationError });

        _context.Operarios.Add(operario);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetOperario), new { id = operario.IdOperario }, operario);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutOperario(int id, Operario operario)
    {
        if (id != operario.IdOperario) return BadRequest();
        var validationError = NormalizarYValidarDisponibilidad(operario);
        if (validationError != null) return BadRequest(new { message = validationError });

        _context.Entry(operario).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!OperarioExists(id)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOperario(int id)
    {
        var operario = await _context.Operarios.FindAsync(id);
        if (operario == null) return NotFound();
        _context.Operarios.Remove(operario);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool OperarioExists(int id)
    {
        return _context.Operarios.Any(e => e.IdOperario == id);
    }

    private static string? NormalizarYValidarDisponibilidad(Operario operario)
    {
        operario.EstadoLaboral = string.IsNullOrWhiteSpace(operario.EstadoLaboral)
            ? "Activo"
            : operario.EstadoLaboral.Trim();

        if (string.Equals(operario.EstadoLaboral, "Activo", StringComparison.OrdinalIgnoreCase))
        {
            operario.EstadoLaboral = "Activo";
            operario.Activo = true;
            operario.MotivoInactividad = null;
            operario.InactivoDesde = null;
            operario.InactivoHasta = null;
        }
        else if (string.Equals(operario.EstadoLaboral, "Inactivo", StringComparison.OrdinalIgnoreCase))
        {
            operario.EstadoLaboral = "Inactivo";
            operario.Activo = false;
            if (string.IsNullOrWhiteSpace(operario.MotivoInactividad))
                return "Debe indicar el motivo de inactividad.";
            if (!operario.InactivoDesde.HasValue || !operario.InactivoHasta.HasValue)
                return "Debe indicar las fechas Desde y Hasta de la inactividad.";

            var inactivoDesde = operario.InactivoDesde.Value.Date;
            var inactivoHasta = operario.InactivoHasta.Value.Date;
            if (inactivoHasta < inactivoDesde)
                return "La fecha Hasta no puede ser anterior a Desde.";

            // Las fechas del formulario representan días completos e inclusivos.
            operario.InactivoDesde = inactivoDesde;
            operario.InactivoHasta = inactivoHasta.AddDays(1).AddTicks(-1);
        }
        else
        {
            return "El estado laboral debe ser Activo o Inactivo.";
        }

        if (operario.HoraInicioJornada >= operario.HoraFinJornada)
            return "La hora de fin de jornada debe ser posterior a la de inicio.";
        if (operario.MinutosMaximosDiarios <= 0 || operario.MinutosMaximosSemanales <= 0)
            return "Los límites diario y semanal deben ser mayores que cero.";

        return null;
    }
}
