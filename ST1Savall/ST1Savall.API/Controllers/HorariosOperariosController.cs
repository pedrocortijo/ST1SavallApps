using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
namespace ST1Savall.API.Controllers;
[ApiController, Route("api/[controller]")]
public class HorariosOperariosController(ApplicationDbContext context) : ControllerBase {
    [HttpGet] public Task<List<HorarioOperario>> Get() => context.HorariosOperarios.Include(h=>h.Operario).Include(h=>h.Turno).ToListAsync();
    [HttpPost] public async Task<ActionResult<HorarioOperario>> Post(HorarioOperario item) { context.HorariosOperarios.Add(item); await context.SaveChangesAsync(); return Ok(item); }
    [HttpPut("{id}")] public async Task<IActionResult> Put(int id, HorarioOperario item) { if(id!=item.IdAsignacion)return BadRequest(); context.Entry(item).State=EntityState.Modified; await context.SaveChangesAsync(); return NoContent(); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { var item=await context.HorariosOperarios.FindAsync(id); if(item==null)return NotFound(); context.HorariosOperarios.Remove(item); await context.SaveChangesAsync(); return NoContent(); }
}
