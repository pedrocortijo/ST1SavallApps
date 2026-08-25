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
    private readonly ApplicationDbContext _applicationContext;

    public ObrasController(
        SageComunDbContext comunContext,
        SageGestionDbContext gestionContext,
        ApplicationDbContext applicationContext)
    {
        _comunContext = comunContext;
        _gestionContext = gestionContext;
        _applicationContext = applicationContext;
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
                Visible = so.Posicion == 0,
                Posicion = so.Posicion,
                Nima = so.Libre3.Trim(),
                Libre1 = so.Libre1?.Trim(),
                Libre2 = so.Libre2?.Trim(),
                Telefono = so.Telefono.Trim(),
                Movil = so.Movil.Trim(),
                Cliente = so.Cliente.Trim(),
                Encargado = so.Encargado.Trim(),
                Observaciones = so.Observacio
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
            Visible = so.Posicion == 0,
            Posicion = so.Posicion,
            Nima = so.Libre3.Trim(),
            Libre1 = so.Libre1?.Trim(),
            Libre2 = so.Libre2?.Trim(),
            Telefono = so.Telefono.Trim(),
            Movil = so.Movil.Trim(),
            Cliente = so.Cliente.Trim(),
            Encargado = so.Encargado.Trim(),
            Observaciones = so.Observacio
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
        try
        {
        var validationError = ValidarObra(obra);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

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
            Encargado = obra.Encargado ?? "",
            Terminada = obra.Finalizada ?? false,
            Posicion = obra.Posicion ?? (obra.Visible == false ? 1 : 0),
            Vista = obra.Visible ?? true,
            Libre1 = obra.Libre1 ?? "",
            Libre2 = obra.Libre2 ?? "",
            Libre3 = obra.Nima ?? "",
            Observacio = obra.Observaciones,
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
        try
        {
            await _comunContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            return Problem(
                title: "No se pudo crear la obra en Sage.",
                detail: ex.GetBaseException().Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        obra.IdObra = ParseCodigoToInt(codigo);
        obra.Codigo = codigo;
        obra.Cliente = resolvedClientCode;
        return CreatedAtAction(nameof(GetObra), new { id = obra.IdObra }, obra);
        }
        catch (Exception ex)
        {
            return Problem(
                title: "No se pudo crear la obra.",
                detail: ex.GetBaseException().Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
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
        so.Encargado = obra.Encargado ?? "";
        so.Terminada = obra.Finalizada ?? false;
        so.Posicion = obra.Posicion ?? (obra.Visible == false ? 1 : 0);
        so.Vista = obra.Visible ?? true;
        if (obra.Libre1 != null) so.Libre1 = obra.Libre1;
        if (obra.Libre2 != null) so.Libre2 = obra.Libre2;
        so.Libre3 = obra.Nima ?? "";
        so.Observacio = obra.Observaciones;
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
            await ActualizarBloqueoSolicitudesDeObraAsync(id, so.Posicion == 1);
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

    private static string? ValidarObra(Obra obra)
    {
        if (string.IsNullOrWhiteSpace(obra.Descripcion))
            return "El nombre de la obra es obligatorio.";

        var limites = new (string Campo, string? Valor, int Maximo)[]
        {
            ("Código", obra.Codigo, 5),
            ("Nombre", obra.Descripcion, 50),
            ("Dirección", obra.Ubicacion, 50),
            ("Población", obra.Poblacion, 30),
            ("Código postal", obra.CodigoPostal, 13),
            ("Provincia", obra.Provincia, 30),
            ("Teléfono", obra.Telefono, 15),
            ("Móvil", obra.Movil, 15),
            ("Encargado", obra.Encargado, 30),
            ("NIMA", obra.Nima, 30),
            ("Cliente", obra.Cliente, 8)
        };

        var excedido = limites.FirstOrDefault(x => x.Valor?.Trim().Length > x.Maximo);
        return excedido.Campo is null
            ? null
            : $"El campo {excedido.Campo} no puede superar {excedido.Maximo} caracteres.";
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
        
        // Stable FNV-1a hash to ensure consistent IDs across process restarts
        unchecked
        {
            uint hash = 2166136261;
            foreach (char c in cleaned)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return Math.Abs((int)hash);
        }
    }

    private string FormatIntToCodigo(int idObra)
    {
        return idObra.ToString().PadLeft(5, '0');
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
