using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;

namespace ST1Savall.API.Services;

public class EstadoOperariosAusenciasService(ApplicationDbContext context)
{
    public async Task<int> SincronizarAsync(DateOnly? fecha = null)
    {
        var hoy = fecha ?? DateOnly.FromDateTime(DateTime.Today);
        var ausenciasVigentes = await context.Ausencias.AsNoTracking()
            .Where(a => a.FechaInicio <= hoy && a.FechaFin >= hoy)
            .OrderByDescending(a => a.FechaInicio)
            .ToListAsync();
        var ausenciaPorConductor = ausenciasVigentes
            .GroupBy(a => a.IdConductor)
            .ToDictionary(g => g.Key, g => g.First());
        var operarios = await context.Operarios.ToListAsync();
        var actualizados = 0;

        foreach (var operario in operarios)
        {
            if (ausenciaPorConductor.TryGetValue(operario.IdOperario, out var ausencia))
            {
                if (operario.EstadoLaboral != "Inactivo" || operario.Activo != false ||
                    operario.MotivoInactividad != ausencia.Tipo ||
                    operario.InactivoDesde?.Date != ausencia.FechaInicio.ToDateTime(TimeOnly.MinValue).Date ||
                    operario.InactivoHasta?.Date != ausencia.FechaFin.ToDateTime(TimeOnly.MinValue).Date)
                {
                    operario.EstadoLaboral = "Inactivo";
                    operario.Activo = false;
                    operario.MotivoInactividad = ausencia.Tipo;
                    operario.InactivoDesde = ausencia.FechaInicio.ToDateTime(TimeOnly.MinValue);
                    operario.InactivoHasta = ausencia.FechaFin.AddDays(1).ToDateTime(TimeOnly.MinValue).AddTicks(-1);
                    actualizados++;
                }
            }
            else if (operario.EstadoLaboral != "Activo" || operario.Activo != true ||
                     operario.MotivoInactividad is not null || operario.InactivoDesde is not null || operario.InactivoHasta is not null)
            {
                operario.EstadoLaboral = "Activo";
                operario.Activo = true;
                operario.MotivoInactividad = null;
                operario.InactivoDesde = null;
                operario.InactivoHasta = null;
                actualizados++;
            }
        }

        if (actualizados > 0) await context.SaveChangesAsync();
        return actualizados;
    }
}
