using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContlfCliSage50Controller : ControllerBase
{
    private readonly SageGestionDbContext _context;

    public ContlfCliSage50Controller(SageGestionDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContlfCliSage50>>> GetContlfCli()
    {
        return await _context.ContlfCli.ToListAsync();
    }

    [HttpGet("{cliente}/{linea}")]
    public async Task<ActionResult<ContlfCliSage50>> GetContlfCli(string cliente, int linea)
    {
        var targetCliente = (cliente ?? "").Trim();
        var item = await _context.ContlfCli.FirstOrDefaultAsync(e => 
            e.Cliente.Trim() == targetCliente && 
            e.Linea == linea);

        if (item == null) return NotFound();
        return item;
    }

    [HttpGet("por-cliente/{cliente}")]
    public async Task<ActionResult<IEnumerable<ContlfCliSage50>>> GetByCliente(string cliente)
    {
        var targetCliente = (cliente ?? "").Trim();
        return await _context.ContlfCli.Where(c => c.Cliente.Trim() == targetCliente).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<ContlfCliSage50>> PostContlfCli(ContlfCliSage50 item)
    {
        if (string.IsNullOrWhiteSpace(item.GuidId))
        {
            item.GuidId = Guid.NewGuid().ToString();
        }
        if (string.IsNullOrWhiteSpace(item.Guid))
        {
            item.Guid = Guid.NewGuid().ToString();
        }
        if (item.Cliente == null)
        {
            item.Cliente = "";
        }

        _context.ContlfCli.Add(item);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (await ContlfCliExistsAsync(item.Cliente, item.Linea))
            {
                return Conflict();
            }
            throw;
        }

        return CreatedAtAction(nameof(GetContlfCli), new { cliente = item.Cliente.Trim(), linea = item.Linea }, item);
    }

    [HttpPut("{cliente}/{linea}")]
    public async Task<IActionResult> PutContlfCli(string cliente, int linea, ContlfCliSage50 item)
    {
        var targetCliente = (cliente ?? "").Trim();
        if (targetCliente != item.Cliente.Trim() || linea != item.Linea)
        {
            return BadRequest();
        }

        _context.Entry(item).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await ContlfCliExistsAsync(targetCliente, linea))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{cliente}/{linea}")]
    public async Task<IActionResult> DeleteContlfCli(string cliente, int linea)
    {
        var targetCliente = (cliente ?? "").Trim();
        var item = await _context.ContlfCli.FirstOrDefaultAsync(e => 
            e.Cliente.Trim() == targetCliente && 
            e.Linea == linea);

        if (item == null) return NotFound();

        _context.ContlfCli.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> ContlfCliExistsAsync(string cliente, int linea)
    {
        var targetCliente = (cliente ?? "").Trim();
        return await _context.ContlfCli.AnyAsync(e => 
            e.Cliente.Trim() == targetCliente && 
            e.Linea == linea);
    }
}
