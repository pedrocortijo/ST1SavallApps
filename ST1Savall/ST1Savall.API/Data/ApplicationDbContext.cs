using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cargo> Cargos { get; set; } = null!;
    public DbSet<Operario> Operarios { get; set; } = null!;
    public DbSet<Camion> Camiones { get; set; } = null!;
    public DbSet<ContenedorTipo> ContenedoresTipos { get; set; } = null!;
    public DbSet<Contenedor> Contenedores { get; set; } = null!;
    public DbSet<Solicitud> Solicitudes { get; set; } = null!;
    public DbSet<EstadoSolicitud> EstadosSolicitud { get; set; } = null!;
    public DbSet<Prioridad> Prioridades { get; set; } = null!;
    public DbSet<Tarea> Tareas { get; set; } = null!;
    public DbSet<TareaRelacion> TareasRelaciones { get; set; } = null!;
    public DbSet<Planta> Plantas { get; set; } = null!;
    public DbSet<Parametro> Parametros { get; set; } = null!;
    public DbSet<PrecioEspecialCabecera> PreciosEspecialesCabeceras { get; set; } = null!;
    public DbSet<PrecioEspecialDetalle> PreciosEspecialesDetalles { get; set; } = null!;
    public DbSet<RutaCache> RutasCache { get; set; } = null!;
    public DbSet<Ausencia> Ausencias { get; set; } = null!;
    public DbSet<Motivo> Motivos { get; set; } = null!;
    public DbSet<SolicitudFoto> SolicitudFotos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure TareaRelacion composite key and relationships
        builder.Entity<TareaRelacion>()
            .HasKey(tr => new { tr.IdTareaOrigen, tr.IdTareaDestino });

        builder.Entity<TareaRelacion>()
            .HasOne(tr => tr.TareaOrigen)
            .WithMany()
            .HasForeignKey(tr => tr.IdTareaOrigen)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TareaRelacion>()
            .HasOne(tr => tr.TareaDestino)
            .WithMany()
            .HasForeignKey(tr => tr.IdTareaDestino)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Operario ID to seed from 0
        builder.Entity<Operario>()
            .Property(o => o.IdOperario)
            .UseIdentityColumn(0, 1);

        builder.Entity<Camion>().HasIndex(c => c.Matricula).IsUnique();
        builder.Entity<Camion>().HasIndex(c => c.UnidadWialonId).IsUnique().HasFilter("[UnidadWialonId] IS NOT NULL");
        builder.Entity<Operario>().HasOne(o => o.Camion).WithMany().HasForeignKey(o => o.IdCamion).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<Operario>().HasIndex(o => o.IdCamion).IsUnique().HasFilter("[IdCamion] IS NOT NULL");

        builder.Entity<Ausencia>()
            .HasOne(a => a.Conductor)
            .WithMany()
            .HasForeignKey(a => a.IdConductor)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Ausencia>()
            .HasIndex(a => new { a.IdConductor, a.FechaInicio });

        builder.Entity<Ausencia>()
            .Property(a => a.Tipo)
            .IsUnicode(false);

        // Configure Contenedor NumSerie to be unique
        builder.Entity<Contenedor>()
            .HasIndex(c => c.NumSerie)
            .IsUnique();

        builder.Entity<ContenedorTipo>()
            .Property(ct => ct.CapacidadMetrosCubicos)
            .HasPrecision(5, 2);

        builder.Entity<PrecioEspecialCabecera>()
            .HasIndex(p => p.ObraSage)
            .IsUnique();
        builder.Entity<PrecioEspecialCabecera>()
            .HasMany(p => p.Detalles).WithOne().HasForeignKey(p => p.IdPrecioEspecialCabecera).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<PrecioEspecialDetalle>()
            .HasIndex(p => new { p.IdPrecioEspecialCabecera, p.ArticuloSage }).IsUnique();

        builder.Entity<Solicitud>()
            .Property(s => s.Latitud)
            .HasPrecision(9, 6);

        builder.Entity<Solicitud>()
            .Property(s => s.Longitud)
            .HasPrecision(9, 6);

        foreach (var propertyName in new[]
        {
            nameof(Solicitud.AlbaranPlanta), nameof(Solicitud.AlbaranSerieSage), nameof(Solicitud.AlbaranNumeroSage), nameof(Solicitud.TipoResiduo)
        })
        {
            builder.Entity<Solicitud>().Property<string?>(propertyName).IsUnicode(false);
        }

        builder.Entity<SolicitudFoto>()
            .HasIndex(f => new { f.IdSolicitud, f.FechaCreacion });

        foreach (var propertyName in new[]
        {
            nameof(Solicitud.LatitudOrigen), nameof(Solicitud.LongitudOrigen),
            nameof(Solicitud.LatitudObra), nameof(Solicitud.LongitudObra),
            nameof(Solicitud.LatitudDescarga), nameof(Solicitud.LongitudDescarga),
            nameof(Solicitud.LatitudRegreso), nameof(Solicitud.LongitudRegreso)
        })
        {
            builder.Entity<Solicitud>().Property<decimal?>(propertyName).HasPrecision(9, 6);
        }

        builder.Entity<Solicitud>()
            .HasIndex(s => new { s.IdConductor, s.FechaHoraInicioPlanificada, s.FechaHoraFinPlanificada })
            .HasDatabaseName("IX_Solicitudes_Conductor_Inicio_Fin");

        builder.Entity<Planta>()
            .Property(p => p.Latitud)
            .HasPrecision(9, 6);

        builder.Entity<Planta>()
            .Property(p => p.Longitud)
            .HasPrecision(9, 6);

        builder.Entity<RutaCache>()
            .HasIndex(r => r.ClaveRuta)
            .IsUnique();

        builder.Entity<RutaCache>()
            .HasIndex(r => r.FechaExpiracionUtc);

        foreach (var propertyName in new[]
        {
            nameof(RutaCache.LatitudOrigen), nameof(RutaCache.LongitudOrigen),
            nameof(RutaCache.LatitudDestino), nameof(RutaCache.LongitudDestino)
        })
        {
            builder.Entity<RutaCache>().Property<decimal>(propertyName).HasPrecision(9, 6);
        }
    }
}
