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
public class ObrasController : ControllerBase
{
    private readonly SageComunDbContext _comunContext;
    private readonly SageGestionDbContext _gestionContext;

    public ObrasController(SageComunDbContext comunContext, SageGestionDbContext gestionContext)
    {
        _comunContext = comunContext;
        _gestionContext = gestionContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Obra>>> GetObras()
    {
        var sageObras = await _comunContext.Obras.ToListAsync();
        
        // Trim client codes to retrieve them accurately
        var clientCodes = sageObras
            .Select(o => (o.Cliente ?? "").Trim())
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .ToList();

        var clients = await _gestionContext.Clientes
            .Where(c => clientCodes.Contains(c.Codigo.Trim()))
            .ToListAsync();

        var clientMap = clients
            .GroupBy(c => c.Codigo.Trim())
            .ToDictionary(g => g.Key, g => g.First());

        var result = new List<Obra>();
        foreach (var so in sageObras)
        {
            var obra = new Obra
            {
                IdObra = ParseCodigoToInt(so.Codigo),
                Codigo = so.Codigo.Trim(),
                Descripcion = so.Nombre.Trim(),
                Ubicacion = so.Direccion.Trim(),
                Poblacion = so.Poblacion.Trim(),
                CodigoPostal = so.Codpost.Trim(),
                Provincia = so.Provincia.Trim(),
                Finalizada = so.Terminada,
                Visible = so.Vista,
                Nima = so.Libre3.Trim(),
                Telefono = so.Telefono.Trim(),
                Movil = so.Movil.Trim(),
                Cliente = so.Cliente.Trim()
            };

            var clientCode = (so.Cliente ?? "").Trim();
            if (clientMap.TryGetValue(clientCode, out var client))
            {
                obra.NombreCliente = client.Nombre.Trim();
                obra.DireccionCliente = client.Direccion.Trim();
                obra.PoblacionCliente = client.Poblacion.Trim();
                obra.CodigoPostalCliente = client.Codpost.Trim();
                obra.TelefonoContactoCliente = client.Telefono.Trim();
                obra.IdEmpresa = 1;
            }
            else
            {
                obra.NombreCliente = "";
                obra.DireccionCliente = "";
                obra.PoblacionCliente = "";
                obra.CodigoPostalCliente = "";
                obra.IdEmpresa = 1;
            }
            result.Add(obra);
        }

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Obra>> GetObra(int id)
    {
        var so = await FindSageObraByIdAsync(id);
        if (so == null) return NotFound();

        var obra = new Obra
        {
            IdObra = id,
            Codigo = so.Codigo.Trim(),
            Descripcion = so.Nombre.Trim(),
            Ubicacion = so.Direccion.Trim(),
            Poblacion = so.Poblacion.Trim(),
            CodigoPostal = so.Codpost.Trim(),
            Provincia = so.Provincia.Trim(),
            Finalizada = so.Terminada,
            Visible = so.Vista,
            Nima = so.Libre3.Trim(),
            Telefono = so.Telefono.Trim(),
            Movil = so.Movil.Trim(),
            Cliente = so.Cliente.Trim()
        };

        var clientCode = (so.Cliente ?? "").Trim();
        if (!string.IsNullOrEmpty(clientCode))
        {
            var client = await _gestionContext.Clientes.FirstOrDefaultAsync(c => c.Codigo.Trim() == clientCode);
            if (client != null)
            {
                obra.NombreCliente = client.Nombre.Trim();
                obra.DireccionCliente = client.Direccion.Trim();
                obra.PoblacionCliente = client.Poblacion.Trim();
                obra.CodigoPostalCliente = client.Codpost.Trim();
                obra.TelefonoContactoCliente = client.Telefono.Trim();
                obra.IdEmpresa = 1;
            }
        }

        return Ok(obra);
    }

    [HttpPost]
    public async Task<ActionResult<Obra>> PostObra(Obra obra)
    {
        var codigo = obra.Codigo?.Trim();
        if (string.IsNullOrWhiteSpace(codigo))
        {
            var maxCodigo = 0;
            var currentCodigos = await _comunContext.Obras.Select(o => o.Codigo).ToListAsync();
            foreach (var c in currentCodigos)
            {
                if (int.TryParse(c.Trim(), out int val))
                {
                    if (val > maxCodigo) maxCodigo = val;
                }
            }
            codigo = (maxCodigo + 1).ToString().PadLeft(5, '0');
        }
        else
        {
            if (int.TryParse(codigo, out int val))
            {
                codigo = val.ToString().PadLeft(5, '0');
            }
        }

        // Check for duplicates
        if (await _comunContext.Obras.AnyAsync(o => o.Codigo.Trim() == codigo.Trim()))
        {
            return Conflict($"La obra con código '{codigo}' ya existe.");
        }

        var so = new ObraComunSage50
        {
            Codigo = codigo,
            Nombre = obra.Descripcion ?? "",
            Direccion = obra.Ubicacion ?? "",
            Poblacion = obra.Poblacion ?? "",
            Codpost = obra.CodigoPostal ?? "",
            Provincia = obra.Provincia ?? "",
            Telefono = obra.Telefono ?? "",
            Movil = obra.Movil ?? "",
            Terminada = obra.Finalizada ?? false,
            Vista = obra.Visible ?? true,
            Libre3 = obra.Nima ?? "",
            Created = DateTime.Now,
            Modified = DateTime.Now,
            GuidId = Guid.NewGuid().ToString()
        };

        // Resolve client code in Sage50
        string? resolvedClientCode = (obra.Cliente ?? "").Trim();
        if (string.IsNullOrEmpty(resolvedClientCode) && !string.IsNullOrWhiteSpace(obra.NombreCliente))
        {
            var client = await _gestionContext.Clientes
                .FirstOrDefaultAsync(c => c.Nombre.Trim() == obra.NombreCliente.Trim() && c.Codigo.StartsWith("430"));
            if (client != null)
            {
                resolvedClientCode = client.Codigo;
            }
            else
            {
                var clientPart = await _gestionContext.Clientes
                    .FirstOrDefaultAsync(c => c.Nombre.Contains(obra.NombreCliente.Trim()) && c.Codigo.StartsWith("430"));
                if (clientPart != null)
                {
                    resolvedClientCode = clientPart.Codigo;
                }
            }
        }

        if (string.IsNullOrEmpty(resolvedClientCode))
        {
            var defaultClient = await _gestionContext.Clientes
                .FirstOrDefaultAsync(c => c.Codigo.StartsWith("430"));
            resolvedClientCode = defaultClient?.Codigo ?? "43000001";
        }

        so.Cliente = resolvedClientCode;

        _comunContext.Obras.Add(so);
        await _comunContext.SaveChangesAsync();

        obra.IdObra = ParseCodigoToInt(codigo);
        obra.Codigo = codigo;
        obra.Cliente = resolvedClientCode;
        return CreatedAtAction(nameof(GetObra), new { id = obra.IdObra }, obra);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutObra(int id, Obra obra)
    {
        if (id != obra.IdObra) return BadRequest();

        // First attempt: search by the Code sent in the body
        ObraComunSage50? so = null;
        if (!string.IsNullOrWhiteSpace(obra.Codigo))
        {
            so = await _comunContext.Obras.FirstOrDefaultAsync(o => o.Codigo.Trim() == obra.Codigo.Trim());
        }

        // Fallback: search by id-based lookup
        if (so == null)
        {
            so = await FindSageObraByIdAsync(id);
        }

        if (so == null) return NotFound();

        so.Nombre = obra.Descripcion ?? "";
        so.Direccion = obra.Ubicacion ?? "";
        so.Poblacion = obra.Poblacion ?? "";
        so.Codpost = obra.CodigoPostal ?? "";
        so.Provincia = obra.Provincia ?? "";
        so.Telefono = obra.Telefono ?? "";
        so.Movil = obra.Movil ?? "";
        so.Terminada = obra.Finalizada ?? false;
        so.Vista = obra.Visible ?? true;
        so.Libre3 = obra.Nima ?? "";
        so.Modified = DateTime.Now;

        // Resolve client code
        string? resolvedClientCode = (obra.Cliente ?? "").Trim();
        if (string.IsNullOrEmpty(resolvedClientCode) && !string.IsNullOrWhiteSpace(obra.NombreCliente))
        {
            var client = await _gestionContext.Clientes
                .FirstOrDefaultAsync(c => c.Nombre.Trim() == obra.NombreCliente.Trim() && c.Codigo.StartsWith("430"));
            if (client != null)
            {
                resolvedClientCode = client.Codigo;
            }
        }

        if (!string.IsNullOrEmpty(resolvedClientCode))
        {
            so.Cliente = resolvedClientCode;
        }

        _comunContext.Entry(so).State = EntityState.Modified;
        try
        {
            await _comunContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ObraExists(id)) return NotFound();
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteObra(int id)
    {
        var so = await FindSageObraByIdAsync(id);
        if (so == null) return NotFound();

        _comunContext.Obras.Remove(so);
        await _comunContext.SaveChangesAsync();
        return NoContent();
    }

    private bool ObraExists(int id)
    {
        if (id >= 0 && id <= 99999)
        {
            var codigoNum = FormatIntToCodigo(id);
            if (_comunContext.Obras.Any(e => e.Codigo.Trim() == codigoNum)) return true;
        }
        return _comunContext.Obras.AsEnumerable().Any(e => ParseCodigoToInt(e.Codigo) == id);
    }

    private async Task<ObraComunSage50?> FindSageObraByIdAsync(int id)
    {
        if (id >= 0 && id <= 99999)
        {
            var codigoNum = FormatIntToCodigo(id);
            var so = await _comunContext.Obras.FirstOrDefaultAsync(o => o.Codigo.Trim() == codigoNum);
            if (so != null) return so;
        }

        var allObras = await _comunContext.Obras.ToListAsync();
        return allObras.FirstOrDefault(o => ParseCodigoToInt(o.Codigo) == id);
    }

    private int ParseCodigoToInt(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return 0;
        var cleaned = codigo.Trim();
        if (int.TryParse(cleaned, out int val))
        {
            return val;
        }
        return Math.Abs(cleaned.GetHashCode());
    }

    private string FormatIntToCodigo(int idObra)
    {
        return idObra.ToString().PadLeft(5, '0');
    }
}
