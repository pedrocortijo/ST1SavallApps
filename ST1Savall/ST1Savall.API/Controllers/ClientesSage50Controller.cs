using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesSage50Controller : ControllerBase
{
    private readonly SageGestionDbContext _context;

    public ClientesSage50Controller(SageGestionDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClienteSage50>>> GetClientes()
    {
        var clientes = await _context.Clientes
            .Where(c => c.Codigo.StartsWith("430"))
            .ToListAsync();

        var clientCodes = clientes.Select(c => c.Codigo.Trim()).ToList();

        var predetContacts = await _context.ContlfCli
            .Where(co => clientCodes.Contains(co.Cliente.Trim()) && co.Predet)
            .ToListAsync();

        var contactMap = predetContacts
            .GroupBy(co => co.Cliente.Trim())
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var c in clientes)
        {
            var code = c.Codigo.Trim();
            if (contactMap.TryGetValue(code, out var contact))
            {
                c.Telefono = contact.Telefono?.Trim() ?? "";
            }
            else
            {
                c.Telefono = "";
            }
        }

        return clientes;
    }

    [HttpGet("siguiente-codigo")]
    public async Task<ActionResult<string>> GetSiguienteCodigo()
    {
        var codigos = await _context.Clientes
            .Where(c => c.Codigo.StartsWith("430"))
            .Select(c => c.Codigo)
            .ToListAsync();

        long maxCodigo = 43000000;
        foreach (var cod in codigos)
        {
            var trimmed = cod.Trim();
            if (long.TryParse(trimmed, out long val))
            {
                if (val > maxCodigo)
                {
                    maxCodigo = val;
                }
            }
        }

        var siguiente = (maxCodigo + 1).ToString();
        return Ok(siguiente.PadLeft(8, '0'));
    }

    [HttpGet("{codigo}/{clienteerp?}")]
    public async Task<ActionResult<ClienteSage50>> GetCliente(string codigo, string? clienteerp)
    {
        var targetCodigo = (codigo ?? "").Trim();
        var targetErp = (clienteerp ?? "").Trim();
        var cliente = await _context.Clientes.FirstOrDefaultAsync(e => 
            e.Codigo.Trim() == targetCodigo && 
            e.Clienteerp.Trim() == targetErp);

        if (cliente == null) return NotFound();

        var predetContact = await _context.ContlfCli
            .FirstOrDefaultAsync(co => co.Cliente.Trim() == targetCodigo && co.Predet);

        if (predetContact != null)
        {
            cliente.Telefono = predetContact.Telefono?.Trim() ?? "";
        }
        else
        {
            cliente.Telefono = "";
        }

        return cliente;
    }

    [HttpPost]
    public async Task<ActionResult<ClienteSage50>> PostCliente(ClienteSage50 cliente)
    {
        if (!string.IsNullOrEmpty(cliente.Codigo))
        {
            cliente.Provinerp = cliente.Codigo.Length >= 2 ? cliente.Codigo.Substring(0, 2) : cliente.Codigo;
        }
        if (string.IsNullOrWhiteSpace(cliente.GuidId))
        {
            cliente.GuidId = Guid.NewGuid().ToString();
        }
        if (string.IsNullOrWhiteSpace(cliente.Guid))
        {
            cliente.Guid = Guid.NewGuid().ToString();
        }
        if (cliente.Clienteerp == null)
        {
            cliente.Clienteerp = "";
        }

        _context.Clientes.Add(cliente);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (await ClienteExistsAsync(cliente.Codigo, cliente.Clienteerp))
            {
                return Conflict();
            }
            throw;
        }
        return CreatedAtAction(nameof(GetCliente), new { codigo = cliente.Codigo.Trim(), clienteerp = cliente.Clienteerp.Trim() }, cliente);
    }

    [HttpPut("{codigo}/{clienteerp?}")]
    public async Task<IActionResult> PutCliente(string codigo, string? clienteerp, ClienteSage50 cliente)
    {
        var targetCodigo = (codigo ?? "").Trim();
        var targetErp = (clienteerp ?? "").Trim();

        if (targetCodigo != cliente.Codigo.Trim() || targetErp != cliente.Clienteerp.Trim()) return BadRequest();
        if (!string.IsNullOrEmpty(cliente.Codigo))
        {
            cliente.Provinerp = cliente.Codigo.Length >= 2 ? cliente.Codigo.Substring(0, 2) : cliente.Codigo;
        }
        _context.Entry(cliente).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await ClienteExistsAsync(codigo, clienteerp)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{codigo}/{clienteerp?}")]
    public async Task<IActionResult> DeleteCliente(string codigo, string? clienteerp)
    {
        var targetCodigo = (codigo ?? "").Trim();
        var targetErp = (clienteerp ?? "").Trim();
        var cliente = await _context.Clientes.FirstOrDefaultAsync(e => 
            e.Codigo.Trim() == targetCodigo && 
            e.Clienteerp.Trim() == targetErp);

        if (cliente == null) return NotFound();
        _context.Clientes.Remove(cliente);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> ClienteExistsAsync(string codigo, string? clienteerp)
    {
        var targetCodigo = (codigo ?? "").Trim();
        var targetErp = (clienteerp ?? "").Trim();
        return await _context.Clientes.AnyAsync(e => 
            e.Codigo.Trim() == targetCodigo && 
            e.Clienteerp.Trim() == targetErp);
    }
}
