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
    public DbSet<ContenedorTipo> ContenedoresTipos { get; set; } = null!;
    public DbSet<Contenedor> Contenedores { get; set; } = null!;
    public DbSet<Solicitud> Solicitudes { get; set; } = null!;
    public DbSet<EstadoSolicitud> EstadosSolicitud { get; set; } = null!;
    public DbSet<Prioridad> Prioridades { get; set; } = null!;
    public DbSet<Tarea> Tareas { get; set; } = null!;
    public DbSet<TareaRelacion> TareasRelaciones { get; set; } = null!;
    public DbSet<Planta> Plantas { get; set; } = null!;
    public DbSet<Parametro> Parametros { get; set; } = null!;

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

        // Configure Contenedor NumSerie to be unique
        builder.Entity<Contenedor>()
            .HasIndex(c => c.NumSerie)
            .IsUnique();

        // Configure Decimals to avoid precision warning
        builder.Entity<ContenedorTipo>()
            .Property(ct => ct.CapacidadMetrosCubicos)
            .HasPrecision(5, 2);

        builder.Entity<Solicitud>()
            .Property(s => s.Latitud)
            .HasPrecision(9, 6);

        builder.Entity<Solicitud>()
            .Property(s => s.Longitud)
            .HasPrecision(9, 6);

        builder.Entity<Planta>()
            .Property(p => p.Latitud)
            .HasPrecision(9, 6);

        builder.Entity<Planta>()
            .Property(p => p.Longitud)
            .HasPrecision(9, 6);
    }
}
