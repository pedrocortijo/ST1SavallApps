using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Services;

public class ArticulosSage50Service
{
    private static readonly string[] FamiliasContenedores = ["00002", "00003"];
    private readonly SageGestionDbContext _context;

    public ArticulosSage50Service(SageGestionDbContext context)
    {
        _context = context;
    }

    public Task<List<ArticuloSage50>> ObtenerArticulosContenedoresAsync() =>
        _context.Articulos.AsNoTracking()
            .Where(a => FamiliasContenedores.Contains(a.Familia.Trim())
                && !string.IsNullOrWhiteSpace(a.Nombre))
            .OrderBy(a => a.Codigo)
            .Select(a => new ArticuloSage50
            {
                Codigo = a.Codigo.Trim(),
                Nombre = a.Nombre.Trim(),
                Familia = a.Familia.Trim()
            })
            .ToListAsync();

    public Task<bool> EsArticuloContenedorAsync(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return Task.FromResult(false);

        return _context.Articulos.AnyAsync(a => a.Codigo.Trim() == codigo.Trim()
            && FamiliasContenedores.Contains(a.Familia.Trim()));
    }
}
