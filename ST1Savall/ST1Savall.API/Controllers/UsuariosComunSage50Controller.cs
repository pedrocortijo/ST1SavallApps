using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosComunSage50Controller : ControllerBase
{
    private readonly SageComunDbContext _context;

    public UsuariosComunSage50Controller(SageComunDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioComunSage50>>> GetUsuarios()
    {
        return await _context.Usuarios.ToListAsync();
    }

    [HttpGet("{codigo}")]
    public async Task<ActionResult<UsuarioComunSage50>> GetUsuario(string codigo)
    {
        var usuario = await _context.Usuarios.FindAsync(codigo);
        if (usuario == null) return NotFound();
        return usuario;
    }
}
