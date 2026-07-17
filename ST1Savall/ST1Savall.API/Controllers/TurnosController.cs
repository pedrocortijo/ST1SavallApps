using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
namespace ST1Savall.API.Controllers;
[ApiController, Route("api/[controller]")]
public class TurnosController(ApplicationDbContext context) : ControllerBase {
    [HttpGet] public Task<List<Turno>> Get() => context.Turnos.OrderBy(t => t.NombreTurno).ToListAsync();
    [HttpPost] public async Task<ActionResult<Turno>> Post(Turno item) { context.Turnos.Add(item); await context.SaveChangesAsync(); return CreatedAtAction(nameof(Get), item); }
    [HttpPost("{id}/duplicar")]
    public async Task<ActionResult<Turno>> Duplicar(int id)
    {
        var origen = await context.Turnos.AsNoTracking().FirstOrDefaultAsync(t => t.IdTurno == id);
        if (origen == null) return NotFound();

        var copia = new Turno
        {
            NombreTurno = $"{origen.NombreTurno} (Copia)",
            HoraEntrada = origen.HoraEntrada,
            HoraSalida = origen.HoraSalida,
            HoraInicioBreak = origen.HoraInicioBreak,
            HoraFinBreak = origen.HoraFinBreak,
            TiempoAlmuerzoMinutos = origen.TiempoAlmuerzoMinutos,
            ToleranciaEntradaMinutos = origen.ToleranciaEntradaMinutos
        };
        context.Turnos.Add(copia);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), copia);
    }
    [HttpPut("{id}")] public async Task<IActionResult> Put(int id, Turno item) { if (id != item.IdTurno) return BadRequest(); context.Entry(item).State = EntityState.Modified; await context.SaveChangesAsync(); return NoContent(); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { var item=await context.Turnos.FindAsync(id); if(item==null)return NotFound(); context.Turnos.Remove(item); await context.SaveChangesAsync(); return NoContent(); }
}
