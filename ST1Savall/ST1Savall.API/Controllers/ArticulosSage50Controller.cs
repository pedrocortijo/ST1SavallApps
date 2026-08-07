using Microsoft.AspNetCore.Mvc;
using ST1Savall.API.Services;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/articulos-sage50")]
public class ArticulosSage50Controller : ControllerBase
{
    private readonly ArticulosSage50Service _articulosService;

    public ArticulosSage50Controller(ArticulosSage50Service articulosService)
    {
        _articulosService = articulosService;
    }

    [HttpGet("contenedores")]
    public async Task<ActionResult<IEnumerable<ArticuloSage50>>> GetArticulosContenedores()
    {
        return await _articulosService.ObtenerArticulosContenedoresAsync();
    }
}
