using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.API.Services;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/articulos-sage50")]
public class ArticulosSage50Controller(SageGestionDbContext context, ArticulosSage50Service articulosService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ArticuloSage50>>> Get() =>
        await context.Articulos.AsNoTracking().OrderBy(a => a.Codigo).ToListAsync();

    [HttpGet("contenedores")]
    public async Task<ActionResult<IEnumerable<ArticuloSage50>>> GetContenedores() =>
        await articulosService.ObtenerArticulosContenedoresAsync();
}
