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
    private readonly SageComunDbContext _comunContext;
    private readonly ApplicationDbContext _applicationContext;

    public ClientesSage50Controller(
        SageGestionDbContext context,
        SageComunDbContext comunContext,
        ApplicationDbContext applicationContext)
    {
        _context = context;
        _comunContext = comunContext;
        _applicationContext = applicationContext;
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

    [HttpGet("paginados")]
    public async Task<ActionResult<ResultadoPaginado<ClienteSage50>>> GetClientesPaginados(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 100,
        [FromQuery] string? buscar = null)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 25, 250);
        var texto = buscar?.Trim();

        IQueryable<ClienteSage50> consulta = _context.Clientes
            .AsNoTracking()
            .Where(c => c.Codigo.StartsWith("430"));

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var patron = $"%{texto}%";
            consulta = consulta.Where(c =>
                EF.Functions.Like(EF.Functions.Collate(c.Codigo, "Modern_Spanish_CI_AI"), patron) ||
                EF.Functions.Like(EF.Functions.Collate(c.Nombre, "Modern_Spanish_CI_AI"), patron) ||
                EF.Functions.Like(EF.Functions.Collate(c.Cif, "Modern_Spanish_CI_AI"), patron) ||
                EF.Functions.Like(EF.Functions.Collate(c.Direccion, "Modern_Spanish_CI_AI"), patron) ||
                EF.Functions.Like(EF.Functions.Collate(c.Poblacion, "Modern_Spanish_CI_AI"), patron));
        }

        var totalRegistros = await consulta.CountAsync();
        var clientes = await consulta
            .OrderBy(c => c.Codigo)
            .ThenBy(c => c.Clienteerp)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        await CompletarTelefonosPredeterminadosAsync(clientes);

        return Ok(new ResultadoPaginado<ClienteSage50>
        {
            Datos = clientes,
            TotalRegistros = totalRegistros,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
    }

    private async Task CompletarTelefonosPredeterminadosAsync(List<ClienteSage50> clientes)
    {
        if (clientes.Count == 0) return;

        var codigos = clientes.Select(c => c.Codigo.Trim()).Distinct().ToList();
        var contactos = await _context.ContlfCli
            .AsNoTracking()
            .Where(co => codigos.Contains(co.Cliente.Trim()) && co.Predet)
            .ToListAsync();
        var telefonos = contactos
            .GroupBy(co => co.Cliente.Trim())
            .ToDictionary(g => g.Key, g => g.First().Telefono?.Trim() ?? string.Empty);

        foreach (var cliente in clientes)
        {
            cliente.Telefono = telefonos.GetValueOrDefault(cliente.Codigo.Trim(), string.Empty);
        }
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
        cliente.Codigo = (cliente.Codigo ?? "").Trim();
        cliente.Clienteerp = (cliente.Clienteerp ?? "").Trim();
        cliente.TipoIva = (cliente.TipoIva ?? "").Trim();

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

        _context.Clientes.Add(cliente);
        try
        {
            await _context.SaveChangesAsync();
            await ActualizarBloqueoObrasYSolicitudesAsync(cliente.Codigo, cliente.BloqCli);
        }
        catch (DbUpdateException)
        {
            if (await ClienteExistsAsync(cliente.Codigo, cliente.Clienteerp))
            {
                return Conflict(new { message = $"El código de cliente '{cliente.Codigo}' ya ha sido registrado." });
            }
            throw;
        }
        return CreatedAtAction(nameof(GetCliente), new { codigo = cliente.Codigo, clienteerp = cliente.Clienteerp }, cliente);
    }

    [HttpPut("{codigo}/{clienteerp?}")]
    public async Task<IActionResult> PutCliente(string codigo, string? clienteerp, ClienteSage50 cliente)
    {
        var targetCodigo = (codigo ?? "").Trim();
        var targetErp = (clienteerp ?? "").Trim();

        cliente.Codigo = (cliente.Codigo ?? "").Trim();
        cliente.Clienteerp = (cliente.Clienteerp ?? "").Trim();
        cliente.TipoIva = (cliente.TipoIva ?? "").Trim();

        if (!string.Equals(targetCodigo, cliente.Codigo, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(targetErp, cliente.Clienteerp, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = $"El código del cliente ('{cliente.Codigo}') o ERP ('{cliente.Clienteerp}') no coincide con la ruta invocada." });
        }

        if (!string.IsNullOrEmpty(cliente.Codigo))
        {
            cliente.Provinerp = cliente.Codigo.Length >= 2 ? cliente.Codigo.Substring(0, 2) : cliente.Codigo;
        }
        var clienteExiste = await _context.Clientes
            .AsNoTracking()
            .AnyAsync(c => c.Codigo.Trim() == targetCodigo && c.Clienteerp.Trim() == targetErp);

        if (!clienteExiste) return NotFound(new { message = "El cliente especificado no existe." });

        _context.Entry(cliente).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();

            // Sincronizar siempre para corregir también obras que pudieran haber
            // quedado desincronizadas por datos antiguos o códigos con espacios.
            await ActualizarBloqueoObrasYSolicitudesAsync(cliente.Codigo, cliente.BloqCli);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await ClienteExistsAsync(codigo, clienteerp)) return NotFound(new { message = "El cliente no existe o fue eliminado." });
            throw;
        }
        return NoContent();
    }

    private async Task ActualizarBloqueoObrasYSolicitudesAsync(string codigoCliente, bool bloqueado)
    {
        var codigo = codigoCliente.Trim();
        var obrasEnProceso = await _comunContext.Obras
            .Where(o => o.Cliente.Trim() == codigo && o.Terminada != true)
            .ToListAsync();

        // Ejecutar la actualización directamente sobre la tabla Sage para
        // garantizar que el campo POSICION se modifica (1 = Bloqueada, 0 = Desbloqueada)
        // aunque el contexto tenga entidades previamente cacheadas.
        await _comunContext.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE obra
            SET POSICION = {(bloqueado ? 1 : 0)}
            WHERE LTRIM(RTRIM(CLIENTE)) = {codigo}
              AND (TERMINADA = 0 OR TERMINADA IS NULL)");

        var idsObras = obrasEnProceso
            .Select(o => ParseCodigoToInt(o.Codigo))
            .Distinct()
            .ToList();

        if (idsObras.Count == 0) return;

        var solicitudes = await _applicationContext.Solicitudes
            .Where(s => idsObras.Contains(s.IdCliente) && s.Estado != 5 && s.Estado != 6)
            .ToListAsync();

        foreach (var solicitud in solicitudes)
        {
            solicitud.Bloqueado = bloqueado;
        }

        await _applicationContext.SaveChangesAsync();
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
