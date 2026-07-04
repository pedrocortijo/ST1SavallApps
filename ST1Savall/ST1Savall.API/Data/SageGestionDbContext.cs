using Microsoft.EntityFrameworkCore;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Data;

public class SageGestionDbContext : DbContext
{
    public SageGestionDbContext(DbContextOptions<SageGestionDbContext> options)
        : base(options)
    {
    }

    public DbSet<ClienteSage50> Clientes { get; set; } = null!;
    public DbSet<CodPosSage50> Codpos { get; set; } = null!;
    public DbSet<FpagSage50> Fpag { get; set; } = null!;
    public DbSet<ContlfCliSage50> ContlfCli { get; set; } = null!;
    public DbSet<TipoIvaSage50> TipoIva { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ClienteSage50>(entity =>
        {
            entity.ToTable("clientes");
            entity.HasKey(c => new { c.Codigo, c.Clienteerp });

            entity.Property(c => c.Agencia).HasColumnName("AGENCIA").HasMaxLength(3).IsFixedLength();
            entity.Property(c => c.Albafra).HasColumnName("ALBAFRA");
            entity.Property(c => c.BancoPrev).HasColumnName("BANCO_PREV").HasMaxLength(8).IsFixedLength();
            entity.Property(c => c.BloqCli).HasColumnName("BLOQ_CLI");
            entity.Property(c => c.BloqVen).HasColumnName("BLOQ_VEN");
            entity.Property(c => c.Bloqalbvta).HasColumnName("BLOQALBVTA");
            entity.Property(c => c.Bloqdepvta).HasColumnName("BLOQDEPVTA");
            entity.Property(c => c.Bloqpedvta).HasColumnName("BLOQPEDVTA");
            entity.Property(c => c.Bloqprevta).HasColumnName("BLOQPREVTA");
            entity.Property(c => c.CEnt).HasColumnName("C_ENT").HasMaxLength(3).IsFixedLength();
            entity.Property(c => c.Cambio).HasColumnName("CAMBIO").HasPrecision(20, 6);
            entity.Property(c => c.Canal).HasColumnName("CANAL").HasMaxLength(10).IsFixedLength();
            entity.Property(c => c.CiaCred).HasColumnName("CIA_CRED").HasMaxLength(2).IsFixedLength();
            entity.Property(c => c.Cif).HasColumnName("CIF").HasMaxLength(16).IsFixedLength();
            entity.Property(c => c.Clienteerp).HasColumnName("CLIENTEERP").HasMaxLength(15).IsFixedLength();
            entity.Property(c => c.Clifinal).HasColumnName("CLIFINAL").HasMaxLength(8).IsFixedLength();
            entity.Property(c => c.Codigo).HasColumnName("CODIGO").HasMaxLength(8).IsFixedLength();
            entity.Property(c => c.Codpost).HasColumnName("CODPOST").HasMaxLength(10).IsFixedLength();
            entity.Property(c => c.Comunitari).HasColumnName("COMUNITARI");
            entity.Property(c => c.Contado).HasColumnName("CONTADO");
            entity.Property(c => c.Contrapar).HasColumnName("CONTRAPAR").HasMaxLength(8).IsFixedLength();
            entity.Property(c => c.CopiaFra).HasColumnName("COPIA_FRA");
            entity.Property(c => c.Created).HasColumnName("CREATED");
            entity.Property(c => c.Credito).HasColumnName("CREDITO").HasPrecision(20, 6);
            entity.Property(c => c.Csb).HasColumnName("CSB");
            entity.Property(c => c.Ctaerp).HasColumnName("CTAERP").HasMaxLength(15).IsFixedLength();
            entity.Property(c => c.Delegerp).HasColumnName("DELEGERP").HasMaxLength(10).IsFixedLength();
            entity.Property(c => c.Descu1).HasColumnName("DESCU1").HasPrecision(20, 6);
            entity.Property(c => c.Descu2).HasColumnName("DESCU2").HasPrecision(20, 6);
            entity.Property(c => c.Dia1).HasColumnName("DIA1");
            entity.Property(c => c.Dia2).HasColumnName("DIA2");
            entity.Property(c => c.Dia3).HasColumnName("DIA3");
            entity.Property(c => c.Dia4).HasColumnName("DIA4");
            entity.Property(c => c.Dia5).HasColumnName("DIA5");
            entity.Property(c => c.Dia6).HasColumnName("DIA6");
            entity.Property(c => c.Dia7).HasColumnName("DIA7");
            entity.Property(c => c.Diapag).HasColumnName("DIAPAG");
            entity.Property(c => c.Diapag2).HasColumnName("DIAPAG2");
            entity.Property(c => c.Dire).HasColumnName("DIRE").HasMaxLength(30).IsFixedLength();
            entity.Property(c => c.Direcc2erp).HasColumnName("DIRECC2ERP").HasMaxLength(40).IsFixedLength();
            entity.Property(c => c.Direccion).HasColumnName("DIRECCION").HasMaxLength(80).IsFixedLength();
            entity.Property(c => c.EfAdjfact).HasColumnName("EF_ADJFACT");
            entity.Property(c => c.EfCuebanc).HasColumnName("EF_CUEBANC").HasMaxLength(3).IsFixedLength();
            entity.Property(c => c.EfEnvmail).HasColumnName("EF_ENVMAIL");
            entity.Property(c => c.EfFormat).HasColumnName("EF_FORMAT");
            entity.Property(c => c.EfTexmail).HasColumnName("EF_TEXMAIL");
            entity.Property(c => c.EfTipodoc).HasColumnName("EF_TIPODOC");
            entity.Property(c => c.Email).HasColumnName("EMAIL").HasMaxLength(150).IsFixedLength();
            entity.Property(c => c.EmailF).HasColumnName("EMAIL_F").HasMaxLength(150).IsFixedLength();
            entity.Property(c => c.EnvCli).HasColumnName("ENV_CLI");
            entity.Property(c => c.EsGrupo).HasColumnName("ES_GRUPO");
            entity.Property(c => c.Eticomu).HasColumnName("ETICOMU").HasMaxLength(100).IsFixedLength();
            entity.Property(c => c.Excluir349).HasColumnName("EXCLUIR349");
            entity.Property(c => c.Exportar).HasColumnName("EXPORTAR");
            entity.Property(c => c.FAlta).HasColumnName("F_ALTA");
            entity.Property(c => c.Facebook).HasColumnName("FACEBOOK").HasMaxLength(254).IsFixedLength();
            entity.Property(c => c.Fbloqnocar).HasColumnName("FBLOQNOCAR");
            entity.Property(c => c.Fbloqnoema).HasColumnName("FBLOQNOEMA");
            entity.Property(c => c.Fbloqnosms).HasColumnName("FBLOQNOSMS");
            entity.Property(c => c.FecCam).HasColumnName("FEC_CAM");
            entity.Property(c => c.FechaBaj).HasColumnName("FECHA_BAJ");
            entity.Property(c => c.Fpag).HasColumnName("FPAG").HasMaxLength(2).IsFixedLength();
            entity.Property(c => c.Fraesi).HasColumnName("FRAESI");
            entity.Property(c => c.Fraped).HasColumnName("FRAPED");
            entity.Property(c => c.Girmescomp).HasColumnName("GIRMESCOMP");
            entity.Property(c => c.Guid).HasColumnName("GUID").HasMaxLength(50).IsFixedLength();
            entity.Property(c => c.GuidExp).HasColumnName("GUID_EXP").HasMaxLength(50).IsFixedLength();
            entity.Property(c => c.GuidId).HasColumnName("GUID_ID").HasMaxLength(50).IsFixedLength();
            entity.Property(c => c.Http).HasColumnName("HTTP").HasMaxLength(60).IsFixedLength();
            entity.Property(c => c.Idioma).HasColumnName("IDIOMA").HasMaxLength(3).IsFixedLength();
            entity.Property(c => c.IdiomaImp).HasColumnName("IDIOMA_IMP").HasMaxLength(3).IsFixedLength();
            entity.Property(c => c.Importar).HasColumnName("IMPORTAR");
            entity.Property(c => c.Isp).HasColumnName("ISP");
            entity.Property(c => c.Letdefrect).HasColumnName("LETDEFRECT").HasMaxLength(2).IsFixedLength();
            entity.Property(c => c.Letdefven).HasColumnName("LETDEFVEN").HasMaxLength(2).IsFixedLength();
            entity.Property(c => c.Libre1).HasColumnName("LIBRE1").HasMaxLength(80).IsFixedLength();
            entity.Property(c => c.LimMon).HasColumnName("LIM_MON");
            entity.Property(c => c.LinDes).HasColumnName("LIN_DES").HasMaxLength(2).IsFixedLength();
            entity.Property(c => c.Mensaje).HasColumnName("MENSAJE").HasMaxLength(50).IsFixedLength();
            entity.Property(c => c.Modified).HasColumnName("MODIFIED");
            entity.Property(c => c.ModoRet).HasColumnName("MODO_RET");
            entity.Property(c => c.Modretnofi).HasColumnName("MODRETNOFI");
            entity.Property(c => c.Nocomucar).HasColumnName("NOCOMUCAR");
            entity.Property(c => c.Nocomuema).HasColumnName("NOCOMUEMA");
            entity.Property(c => c.Nocomuobs).HasColumnName("NOCOMUOBS");
            entity.Property(c => c.Nocomusms).HasColumnName("NOCOMUSMS");
            entity.Property(c => c.Nombre).HasColumnName("NOMBRE").HasMaxLength(120).IsFixedLength();
            entity.Property(c => c.Nombre2).HasColumnName("NOMBRE2").HasMaxLength(120).IsFixedLength();
            entity.Property(c => c.Nombre3erp).HasColumnName("NOMBRE3ERP").HasMaxLength(40).IsFixedLength();
            entity.Property(c => c.Observacio).HasColumnName("OBSERVACIO");
            entity.Property(c => c.Oferta).HasColumnName("OFERTA");
            entity.Property(c => c.Operacio).HasColumnName("OPERACIO").HasMaxLength(15).IsFixedLength();
            entity.Property(c => c.Pais).HasColumnName("PAIS").HasMaxLength(3).IsFixedLength();
            entity.Property(c => c.Plefact).HasColumnName("PLEFACT").HasMaxLength(10).IsFixedLength();
            entity.Property(c => c.Poblacerp).HasColumnName("POBLACERP").HasMaxLength(10).IsFixedLength();
            entity.Property(c => c.Poblacion).HasColumnName("POBLACION").HasMaxLength(30).IsFixedLength();
            entity.Property(c => c.Portcomp).HasColumnName("PORTCOMP").HasPrecision(20, 6);
            entity.Property(c => c.Portes).HasColumnName("PORTES").HasMaxLength(10).IsFixedLength();
            entity.Property(c => c.Posicion).HasColumnName("POSICION").HasPrecision(20, 6);
            entity.Property(c => c.Pregvac).HasColumnName("PREGVAC");
            entity.Property(c => c.Pronto).HasColumnName("PRONTO").HasPrecision(20, 6);
            entity.Property(c => c.Provincia).HasColumnName("PROVINCIA").HasMaxLength(30).IsFixedLength();
            entity.Property(c => c.Provinerp).HasColumnName("PROVINERP").HasMaxLength(10).IsFixedLength();
            entity.Property(c => c.Pverde).HasColumnName("PVERDE");
            entity.Property(c => c.Recarfin).HasColumnName("RECARFIN").HasPrecision(20, 6);
            entity.Property(c => c.Recargo).HasColumnName("RECARGO");
            entity.Property(c => c.Recc).HasColumnName("RECC");
            entity.Property(c => c.ReferCat).HasColumnName("REFER_CAT").HasMaxLength(25).IsFixedLength();
            entity.Property(c => c.Refundir).HasColumnName("REFUNDIR");
            entity.Property(c => c.Regcaja).HasColumnName("REGCAJA");
            entity.Property(c => c.Retencion).HasColumnName("RETENCION");
            entity.Property(c => c.Retnofisc).HasColumnName("RETNOFISC");
            entity.Property(c => c.Ruta).HasColumnName("RUTA").HasMaxLength(2).IsFixedLength();
            entity.Property(c => c.Skype).HasColumnName("SKYPE").HasMaxLength(100).IsFixedLength();
            entity.Property(c => c.SyncCtc).HasColumnName("SYNC_CTC");
            entity.Property(c => c.Tarifa).HasColumnName("TARIFA").HasMaxLength(2).IsFixedLength();
            entity.Property(c => c.Territerp).HasColumnName("TERRITERP");
            entity.Property(c => c.Tipcredit).HasColumnName("TIPCREDIT").HasMaxLength(3).IsFixedLength();
            entity.Property(c => c.TipoCli).HasColumnName("TIPO_CLI");
            entity.Property(c => c.TipoIva).HasColumnName("TIPO_IVA").HasMaxLength(2).IsFixedLength();
            entity.Property(c => c.TipoRet).HasColumnName("TIPO_RET").HasMaxLength(2).IsFixedLength();
            entity.Property(c => c.Tipofac).HasColumnName("TIPOFAC").HasMaxLength(2).IsFixedLength();
            entity.Property(c => c.Tpcretnofi).HasColumnName("TPCRETNOFI").HasPrecision(20, 6);
            entity.Property(c => c.Twitter).HasColumnName("TWITTER").HasMaxLength(254).IsFixedLength();
            entity.Property(c => c.ValPunt).HasColumnName("VAL_PUNT").HasPrecision(20, 6);
            entity.Ignore(c => c.Telefono);
            entity.Property(c => c.Validcheck).HasColumnName("VALIDCHECK").HasMaxLength(64).IsFixedLength();
            entity.Property(c => c.ValorAlb).HasColumnName("VALOR_ALB");
            entity.Property(c => c.Valportes).HasColumnName("VALPORTES").HasPrecision(20, 6);
            entity.Property(c => c.Vendedor).HasColumnName("VENDEDOR").HasMaxLength(5).IsFixedLength();
            entity.Property(c => c.Vista).HasColumnName("VISTA");
            entity.Property(c => c.Zona).HasColumnName("ZONA").HasMaxLength(4).IsFixedLength();
        });

        modelBuilder.Entity<CodPosSage50>(entity =>
        {
            entity.ToTable("codpos");
            entity.HasKey(c => new { c.Codigo, c.Linea });

            entity.Property(c => c.Codigo).HasColumnName("CODIGO").HasMaxLength(10).IsFixedLength();
            entity.Property(c => c.Cpostalm).HasColumnName("CPOSTALM").HasMaxLength(5).IsFixedLength();
            entity.Property(c => c.Created).HasColumnName("CREATED");
            entity.Property(c => c.GuidId).HasColumnName("GUID_ID").HasMaxLength(50).IsFixedLength();
            entity.Property(c => c.Lati).HasColumnName("LATI").HasMaxLength(30).IsFixedLength();
            entity.Property(c => c.Linea).HasColumnName("LINEA").HasMaxLength(5).IsFixedLength();
            entity.Property(c => c.Longi).HasColumnName("LONGI").HasMaxLength(30).IsFixedLength();
            entity.Property(c => c.Modified).HasColumnName("MODIFIED");
            entity.Property(c => c.Poblacerp).HasColumnName("POBLACERP").HasMaxLength(10).IsFixedLength();
            entity.Property(c => c.Poblacion).HasColumnName("POBLACION").HasMaxLength(30).IsFixedLength();
            entity.Property(c => c.Provincia).HasColumnName("PROVINCIA").HasMaxLength(30).IsFixedLength();
            entity.Property(c => c.Provinerp).HasColumnName("PROVINERP").HasMaxLength(10).IsFixedLength();
            entity.Property(c => c.Vista).HasColumnName("VISTA");
        });

        modelBuilder.Entity<FpagSage50>(entity =>
        {
            entity.ToTable("fpag");
            entity.HasKey(e => e.Codigo);

            entity.Property(e => e.Codigo).HasColumnName("CODIGO").HasMaxLength(2).IsFixedLength();
            entity.Property(e => e.Nombre).HasColumnName("NOMBRE").HasMaxLength(50).IsFixedLength();
            entity.Property(e => e.RecFinan).HasColumnName("REC_FINAN").HasPrecision(20, 2);
            entity.Property(e => e.Vista).HasColumnName("VISTA");
            entity.Property(e => e.Grupo).HasColumnName("GRUPO").HasMaxLength(2).IsFixedLength();
            entity.Property(e => e.Efectivo).HasColumnName("EFECTIVO");
            entity.Property(e => e.Csb34).HasColumnName("CSB34");
            entity.Property(e => e.DiasRiesgo).HasColumnName("DIAS_RIESGO");
            entity.Property(e => e.Contado).HasColumnName("CONTADO");
            entity.Property(e => e.Guid).HasColumnName("GUID").HasMaxLength(50).IsFixedLength();
            entity.Property(e => e.Importar).HasColumnName("IMPORTAR");
            entity.Property(e => e.Fraefpag).HasColumnName("FRAEFPAG").HasMaxLength(2).IsFixedLength();
            entity.Property(e => e.GuidId).HasColumnName("GUID_ID").HasMaxLength(50).IsFixedLength();
            entity.Property(e => e.Created).HasColumnName("CREATED");
            entity.Property(e => e.Modified).HasColumnName("MODIFIED");
            entity.Property(e => e.Domibanc).HasColumnName("DOMIBANC");
            entity.Property(e => e.Girmescomp).HasColumnName("GIRMESCOMP");
            entity.Property(e => e.InfoAdi).HasColumnName("INFO_ADI").HasMaxLength(100).IsFixedLength();
        });

        modelBuilder.Entity<ContlfCliSage50>(entity =>
        {
            entity.ToTable("contlf_cli");
            entity.HasKey(c => new { c.Cliente, c.Linea });

            entity.Property(c => c.Cliente).HasColumnName("CLIENTE").HasMaxLength(8).IsFixedLength();
            entity.Property(c => c.Linea).HasColumnName("LINEA");
            entity.Property(c => c.Predet).HasColumnName("PREDET");
            entity.Property(c => c.Persona).HasColumnName("PERSONA").HasMaxLength(30).IsFixedLength();
            entity.Property(c => c.Cargo).HasColumnName("CARGO").HasMaxLength(30).IsFixedLength();
            entity.Property(c => c.Telefono).HasColumnName("TELEFONO").HasMaxLength(15).IsFixedLength();
            entity.Property(c => c.Observa).HasColumnName("OBSERVA").HasMaxLength(150).IsFixedLength();
            entity.Property(c => c.Email).HasColumnName("EMAIL").HasMaxLength(150).IsFixedLength();
            entity.Property(c => c.Skype).HasColumnName("SKYPE").HasMaxLength(100).IsFixedLength();
            entity.Property(c => c.Facebook).HasColumnName("FACEBOOK").HasMaxLength(254).IsFixedLength();
            entity.Property(c => c.Twitter).HasColumnName("TWITTER").HasMaxLength(254).IsFixedLength();
            entity.Property(c => c.Lincontcli).HasColumnName("LINCONTCLI");
            entity.Property(c => c.Lintelfcli).HasColumnName("LINTELFCLI");
            entity.Property(c => c.Vista).HasColumnName("VISTA");
            entity.Property(c => c.Guid).HasColumnName("GUID").HasMaxLength(50).IsFixedLength();
            entity.Property(c => c.GuidExp).HasColumnName("GUID_EXP").HasMaxLength(50).IsFixedLength();
            entity.Property(c => c.Exportar).HasColumnName("EXPORTAR");
            entity.Property(c => c.Importar).HasColumnName("IMPORTAR");
            entity.Property(c => c.GuidId).HasColumnName("GUID_ID").HasMaxLength(50).IsFixedLength();
            entity.Property(c => c.Created).HasColumnName("CREATED");
            entity.Property(c => c.Modified).HasColumnName("MODIFIED");
            entity.Property(c => c.Tipo).HasColumnName("TIPO");
        });

        modelBuilder.Entity<TipoIvaSage50>(entity =>
        {
            entity.ToTable("tipo_iva");
            entity.HasKey(t => t.Codigo);

            entity.Property(t => t.Codigo).HasColumnName("CODIGO").HasMaxLength(2).IsFixedLength();
            entity.Property(t => t.Nombre).HasColumnName("NOMBRE").HasMaxLength(50).IsFixedLength();
            entity.Property(t => t.Iva).HasColumnName("IVA").HasPrecision(20, 2);
            entity.Property(t => t.Recarg).HasColumnName("RECARG").HasPrecision(20, 2);
            entity.Property(t => t.CtaIvSop).HasColumnName("CTA_IV_SOP").HasMaxLength(8).IsFixedLength();
            entity.Property(t => t.CtaIvRep).HasColumnName("CTA_IV_REP").HasMaxLength(8).IsFixedLength();
            entity.Property(t => t.CtaReSop).HasColumnName("CTA_RE_SOP").HasMaxLength(8).IsFixedLength();
            entity.Property(t => t.CtaReRep).HasColumnName("CTA_RE_REP").HasMaxLength(8).IsFixedLength();
            entity.Property(t => t.Vista).HasColumnName("VISTA");
            entity.Property(t => t.Comunitari).HasColumnName("COMUNITARI");
            entity.Property(t => t.Inmovil).HasColumnName("INMOVIL");
            entity.Property(t => t.IvaCee).HasColumnName("IVA_CEE").HasMaxLength(2).IsFixedLength();
            entity.Property(t => t.Deduce).HasColumnName("DEDUCE");
            entity.Property(t => t.Exento).HasColumnName("EXENTO");
            entity.Property(t => t.AgViaje).HasColumnName("AG_VIAJE");
            entity.Property(t => t.Pendevrep).HasColumnName("PENDEVREP").HasMaxLength(8).IsFixedLength();
            entity.Property(t => t.Pendedsop).HasColumnName("PENDEDSOP").HasMaxLength(8).IsFixedLength();
            entity.Property(t => t.Guid).HasColumnName("GUID").HasMaxLength(50).IsFixedLength();
            entity.Property(t => t.Importar).HasColumnName("IMPORTAR");
            entity.Property(t => t.Recsopcdev).HasColumnName("RECSOPCDEV").HasMaxLength(8).IsFixedLength();
            entity.Property(t => t.Recrepcdev).HasColumnName("RECREPCDEV").HasMaxLength(8).IsFixedLength();
            entity.Property(t => t.Grupoiva).HasColumnName("GRUPOIVA");
            entity.Property(t => t.Ivaequierp).HasColumnName("IVAEQUIERP").HasMaxLength(2).IsFixedLength();
            entity.Property(t => t.Territerp).HasColumnName("TERRITERP");
            entity.Property(t => t.GuidId).HasColumnName("GUID_ID").HasMaxLength(50).IsFixedLength();
            entity.Property(t => t.Created).HasColumnName("CREATED");
            entity.Property(t => t.Modified).HasColumnName("MODIFIED");
            entity.Property(t => t.Tipo).HasColumnName("TIPO");
            entity.Property(t => t.IgicImpli).HasColumnName("IGIC_IMPLI");
            entity.Property(t => t.Prtivsopnd).HasColumnName("PRTIVSOPND").HasMaxLength(8).IsFixedLength();
            entity.Property(t => t.Prtivsndpd).HasColumnName("PRTIVSNDPD").HasMaxLength(8).IsFixedLength();
            entity.Property(t => t.TipoImp).HasColumnName("TIPO_IMP");
            entity.Property(t => t.Cero).HasColumnName("CERO");
            entity.Property(t => t.BInv).HasColumnName("B_INV");
        });
    }
}