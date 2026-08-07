using Microsoft.EntityFrameworkCore;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Data;

public class SageComunDbContext : DbContext
{
    public SageComunDbContext(DbContextOptions<SageComunDbContext> options)
        : base(options)
    {
    }

    public DbSet<ObraComunSage50> Obras { get; set; } = null!;
    public DbSet<UsuarioComunSage50> Usuarios { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ObraComunSage50>(entity =>
        {
            entity.ToTable("obra");
            entity.HasKey(o => o.Codigo);

            entity.Property(o => o.Cif).HasColumnName("CIF").HasMaxLength(16).IsFixedLength();
            entity.Property(o => o.Cliente).HasColumnName("CLIENTE").HasMaxLength(8).IsFixedLength();
            entity.Property(o => o.Codigo).HasColumnName("CODIGO").HasMaxLength(5).IsFixedLength();
            entity.Property(o => o.Codpost).HasColumnName("CODPOST").HasMaxLength(13).IsFixedLength();
            entity.Property(o => o.Created).HasColumnName("CREATED");
            entity.Property(o => o.Descuento).HasColumnName("DESCUENTO").HasPrecision(10, 4);
            entity.Property(o => o.Direccion).HasColumnName("DIRECCION").HasMaxLength(50).IsFixedLength();
            entity.Property(o => o.Encargado).HasColumnName("ENCARGADO").HasMaxLength(30).IsFixedLength();
            entity.Property(o => o.Fax).HasColumnName("FAX").HasMaxLength(15).IsFixedLength();
            entity.Property(o => o.Fpag).HasColumnName("FPAG").HasMaxLength(2).IsFixedLength();
            entity.Property(o => o.GuidId).HasColumnName("GUID_ID").HasMaxLength(50).IsFixedLength();
            entity.Property(o => o.Isp).HasColumnName("ISP");
            entity.Property(o => o.Libre1).HasColumnName("LIBRE_1").HasMaxLength(10).IsFixedLength();
            entity.Property(o => o.Libre2).HasColumnName("LIBRE_2").HasMaxLength(10).IsFixedLength();
            entity.Property(o => o.Libre3).HasColumnName("LIBRE_3").HasMaxLength(30).IsFixedLength();
            entity.Property(o => o.Marvehic).HasColumnName("MARVEHIC").HasMaxLength(2).IsFixedLength();
            entity.Property(o => o.Modified).HasColumnName("MODIFIED");
            entity.Property(o => o.Modvehic).HasColumnName("MODVEHIC").HasMaxLength(4).IsFixedLength();
            entity.Property(o => o.Movil).HasColumnName("MOVIL").HasMaxLength(15).IsFixedLength();
            entity.Property(o => o.Nombre).HasColumnName("NOMBRE").HasMaxLength(50).IsFixedLength();
            entity.Property(o => o.Observacio).HasColumnName("OBSERVACIO");
            entity.Property(o => o.Password).HasColumnName("PASSWORD");
            entity.Property(o => o.Poblacion).HasColumnName("POBLACION").HasMaxLength(30).IsFixedLength();
            entity.Property(o => o.Posicion).HasColumnName("POSICION");
            entity.Property(o => o.Pp).HasColumnName("PP").HasPrecision(10, 4);
            entity.Property(o => o.Provincia).HasColumnName("PROVINCIA").HasMaxLength(30).IsFixedLength();
            entity.Property(o => o.Ruta).HasColumnName("RUTA").HasMaxLength(2).IsFixedLength();
            entity.Property(o => o.Tarifa).HasColumnName("TARIFA").HasMaxLength(2).IsFixedLength();
            entity.Property(o => o.Telefono).HasColumnName("TELEFONO").HasMaxLength(15).IsFixedLength();
            entity.Property(o => o.Terminada).HasColumnName("TERMINADA");
            entity.Property(o => o.TipoIva).HasColumnName("TIPO_IVA").HasMaxLength(2).IsFixedLength();
            entity.Property(o => o.Usuario).HasColumnName("USUARIO").HasMaxLength(15).IsFixedLength();
            entity.Property(o => o.Vendedor).HasColumnName("VENDEDOR").HasMaxLength(5).IsFixedLength();
            entity.Property(o => o.Vista).HasColumnName("VISTA");
            entity.Property(o => o.Zona).HasColumnName("ZONA").HasMaxLength(4).IsFixedLength();
        });

        modelBuilder.Entity<UsuarioComunSage50>(entity =>
        {
            entity.ToTable("usuarios");
            entity.HasKey(u => u.Codigo);

            entity.Property(u => u.Codigo).HasColumnName("CODIGO").HasMaxLength(15).IsFixedLength();
            entity.Property(u => u.Nombre).HasColumnName("NOMBRE").HasMaxLength(100).IsFixedLength();
        });
    }
}
