using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
namespace ST1Savall.API.Controllers;
[ApiController, Route("api/[controller]")]
public class HorariosObraController(ApplicationDbContext application, SageComunDbContext sage) : ControllerBase {
    [HttpGet("{codigo}")]
    public async Task<ActionResult<HorarioObra>> Get(string codigo) {
        var horario = await application.HorariosObra.AsNoTracking().FirstOrDefaultAsync(x => x.Codigo == Codigo(codigo));
        return horario is null ? NotFound() : Ok(horario);
    }
    [HttpPut("{codigo}")]
    public async Task<IActionResult> Put(string codigo, HorarioObra horario) {
        var clave = Codigo(codigo);
        if (clave.Length != 5 || clave != Codigo(horario.Codigo)) return BadRequest(new { message = "El código de horario no es válido." });
        if (horario.HoraFin <= horario.HoraInicio) return BadRequest(new { message = "La hora de fin debe ser posterior a la hora de inicio." });
        if (!await sage.Obras.AsNoTracking().AnyAsync(x => x.Codigo == clave)) return BadRequest(new { message = "El código debe corresponder a una obra de Sage." });
        var actual = await application.HorariosObra.FindAsync(clave);
        if (actual is null) { horario.Codigo = clave; application.HorariosObra.Add(horario); } else { actual.HoraInicio = horario.HoraInicio; actual.HoraFin = horario.HoraFin; }
        await application.SaveChangesAsync(); return NoContent();
    }
    private static string Codigo(string? codigo) => (codigo ?? string.Empty).Trim();
}