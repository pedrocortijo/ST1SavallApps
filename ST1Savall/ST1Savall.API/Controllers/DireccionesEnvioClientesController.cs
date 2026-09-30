using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/direcciones-envio-clientes")]
public class DireccionesEnvioClientesController(SageGestionDbContext context) : ControllerBase
{
    [HttpGet("por-cliente/{cliente}")]
    public async Task<ActionResult<IEnumerable<DireccionEnvioClienteSage50>>> GetPorCliente(string cliente)
    {
        var codigo = cliente.Trim();
        return await context.DireccionesEnvioClientes
            .AsNoTracking()
            .Where(d => d.CLIENTE.Trim() == codigo)
            .OrderBy(d => d.LINEA)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<DireccionEnvioClienteSage50>> Post(DireccionEnvioClienteSage50 direccion)
    {
        Normalizar(direccion);
        if (await ExisteAsync(direccion.CLIENTE, direccion.LINEA))
            return Conflict(new { message = "Ya existe una dirección con esa línea para el cliente." });

        context.DireccionesEnvioClientes.Add(direccion);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPorCliente), new { cliente = direccion.CLIENTE }, direccion);
    }

    [HttpPut("{cliente}/{linea:int}")]
    public async Task<IActionResult> Put(string cliente, int linea, DireccionEnvioClienteSage50 direccion)
    {
        var codigo = cliente.Trim();
        if (!string.Equals(codigo, direccion.CLIENTE?.Trim(), StringComparison.OrdinalIgnoreCase) || linea != direccion.LINEA)
            return BadRequest(new { message = "La dirección no coincide con la ruta solicitada." });

        var existente = await context.DireccionesEnvioClientes
            .FirstOrDefaultAsync(d => d.CLIENTE.Trim() == codigo && d.LINEA == linea);
        if (existente is null) return NotFound();

        Normalizar(direccion);
        existente.NOMBRE = direccion.NOMBRE;
        existente.DIRECCION = direccion.DIRECCION;
        existente.CODPOS = direccion.CODPOS;
        existente.POBLACION = direccion.POBLACION;
        existente.PROVINCIA = direccion.PROVINCIA;
        existente.VISTA = direccion.VISTA;
        existente.TIPO = direccion.TIPO;
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{cliente}/{linea:int}")]
    public async Task<IActionResult> Delete(string cliente, int linea)
    {
        var codigo = cliente.Trim();
        if (linea == 1)
            return BadRequest(new { message = "La dirección principal no se puede eliminar." });

        var existente = await context.DireccionesEnvioClientes
            .FirstOrDefaultAsync(d => d.CLIENTE.Trim() == codigo && d.LINEA == linea);
        if (existente is null) return NotFound();

        context.DireccionesEnvioClientes.Remove(existente);
        await context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> ExisteAsync(string cliente, int linea) =>
        await context.DireccionesEnvioClientes.AnyAsync(d => d.CLIENTE.Trim() == cliente.Trim() && d.LINEA == linea);

    private static void Normalizar(DireccionEnvioClienteSage50 direccion)
    {
        direccion.CLIENTE = direccion.CLIENTE?.Trim() ?? string.Empty;
        direccion.NOMBRE = direccion.NOMBRE?.Trim() ?? string.Empty;
        direccion.DIRECCION = direccion.DIRECCION?.Trim() ?? string.Empty;
        direccion.CODPOS = direccion.CODPOS?.Trim() ?? string.Empty;
        direccion.POBLACION = direccion.POBLACION?.Trim() ?? string.Empty;
        direccion.PROVINCIA = direccion.PROVINCIA?.Trim() ?? string.Empty;
        direccion.VISTA ??= true;
    }
}
