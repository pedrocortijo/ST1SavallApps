using System;
using System.ComponentModel.DataAnnotations;

namespace ST1Savall.Shared.Data;

public class ClienteSage50
{
    [MaxLength(3)] public string Agencia { get; set; } = string.Empty;
    public bool Albafra { get; set; } = false;
    [MaxLength(8)] public string BancoPrev { get; set; } = string.Empty;
    public bool BloqCli { get; set; } = false;
    public bool BloqVen { get; set; } = false;
    public bool Bloqalbvta { get; set; } = false;
    public bool Bloqdepvta { get; set; } = false;
    public bool Bloqpedvta { get; set; } = false;
    public bool Bloqprevta { get; set; } = false;
    [MaxLength(3)] public string CEnt { get; set; } = string.Empty;
    public decimal Cambio { get; set; } = 0.0m;
    [MaxLength(10)] public string Canal { get; set; } = string.Empty;
    [MaxLength(2)] public string CiaCred { get; set; } = string.Empty;
    [MaxLength(15)] public string Cif { get; set; } = string.Empty;
    [MaxLength(15)] public string Clienteerp { get; set; } = string.Empty;
    [MaxLength(8)] public string Clifinal { get; set; } = string.Empty;
    [MaxLength(8)] public string Codigo { get; set; } = string.Empty;
    [MaxLength(10)] public string Codpost { get; set; } = string.Empty;
    public int Comunitari { get; set; } = 0;
    public bool Contado { get; set; } = false;
    [MaxLength(8)] public string Contrapar { get; set; } = string.Empty;
    public int CopiaFra { get; set; } = 0;
    public DateTime Created { get; set; } = DateTime.Now;
    public decimal Credito { get; set; } = 0.0m;
    public bool Csb { get; set; } = false;
    [MaxLength(15)] public string Ctaerp { get; set; } = string.Empty;
    [MaxLength(10)] public string Delegerp { get; set; } = string.Empty;
    public decimal Descu1 { get; set; } = 0.0m;
    public decimal Descu2 { get; set; } = 0.0m;
    public bool Dia1 { get; set; } = false;
    public bool Dia2 { get; set; } = false;
    public bool Dia3 { get; set; } = false;
    public bool Dia4 { get; set; } = false;
    public bool Dia5 { get; set; } = false;
    public bool Dia6 { get; set; } = false;
    public bool Dia7 { get; set; } = false;
    public int Diapag { get; set; } = 0;
    public int Diapag2 { get; set; } = 0;
    [MaxLength(30)] public string Dire { get; set; } = string.Empty;
    [MaxLength(40)] public string Direcc2erp { get; set; } = string.Empty;
    [MaxLength(80)] public string Direccion { get; set; } = string.Empty;
    public bool EfAdjfact { get; set; } = false;
    [MaxLength(3)] public string EfCuebanc { get; set; } = string.Empty;
    public bool EfEnvmail { get; set; } = false;
    public int EfFormat { get; set; } = 0;
    public string EfTexmail { get; set; } = string.Empty;
    public int EfTipodoc { get; set; } = 0;
    [MaxLength(150)] public string Email { get; set; } = string.Empty;
    [MaxLength(150)] public string EmailF { get; set; } = string.Empty;
    public int EnvCli { get; set; } = 0;
    public bool EsGrupo { get; set; } = false;
    [MaxLength(100)] public string Eticomu { get; set; } = string.Empty;
    public bool Excluir349 { get; set; } = false;
    public DateTime? Exportar { get; set; }
    public DateTime FAlta { get; set; } = DateTime.Now;
    [MaxLength(254)] public string Facebook { get; set; } = string.Empty;
    public DateTime? Fbloqnocar { get; set; }
    public DateTime? Fbloqnoema { get; set; }
    public DateTime? Fbloqnosms { get; set; }
    public DateTime? FecCam { get; set; }
    public DateTime? FechaBaj { get; set; }
    [MaxLength(2)] public string Fpag { get; set; } = string.Empty;
    public bool Fraesi { get; set; } = false;
    public bool Fraped { get; set; } = false;
    public bool Girmescomp { get; set; } = false;
    [MaxLength(50)] public string Guid { get; set; } = string.Empty;
    [MaxLength(50)] public string GuidExp { get; set; } = string.Empty;
    [MaxLength(50)] public string GuidId { get; set; } = string.Empty;
    [MaxLength(60)] public string Http { get; set; } = string.Empty;
    [MaxLength(3)] public string Idioma { get; set; } = "000";
    [MaxLength(3)] public string IdiomaImp { get; set; } = string.Empty;
    public DateTime? Importar { get; set; }
    public bool Isp { get; set; } = false;
    [MaxLength(2)] public string Letdefrect { get; set; } = string.Empty;
    [MaxLength(2)] public string Letdefven { get; set; } = string.Empty;
    [MaxLength(80)] public string Libre1 { get; set; } = string.Empty;
    public int LimMon { get; set; } = 0;
    [MaxLength(2)] public string LinDes { get; set; } = string.Empty;
    [MaxLength(50)] public string Mensaje { get; set; } = string.Empty;
    public DateTime Modified { get; set; } = DateTime.Now;
    public bool ModoRet { get; set; } = false;
    public int Modretnofi { get; set; } = 0;
    public bool Nocomucar { get; set; } = false;
    public bool Nocomuema { get; set; } = false;
    public string Nocomuobs { get; set; } = string.Empty;
    public bool Nocomusms { get; set; } = false;
    [MaxLength(120)] public string Nombre { get; set; } = string.Empty;
    [MaxLength(120)] public string Nombre2 { get; set; } = string.Empty;
    [MaxLength(40)] public string Nombre3erp { get; set; } = string.Empty;
    public string? Observacio { get; set; }
    public bool Oferta { get; set; } = false;
    [MaxLength(15)] public string Operacio { get; set; } = string.Empty;
    [MaxLength(3)] public string Pais { get; set; } = string.Empty;
    [MaxLength(10)] public string Plefact { get; set; } = string.Empty;
    [MaxLength(10)] public string Poblacerp { get; set; } = string.Empty;
    [MaxLength(30)] public string Poblacion { get; set; } = string.Empty;
    public decimal Portcomp { get; set; } = 0.0m;
    [MaxLength(10)] public string Portes { get; set; } = string.Empty;
    public decimal Posicion { get; set; } = 0.0m;
    public bool Pregvac { get; set; } = false;
    public decimal Pronto { get; set; } = 0.0m;
    [MaxLength(30)] public string Provincia { get; set; } = string.Empty;
    [MaxLength(10)] public string Provinerp { get; set; } = string.Empty;
    public bool Pverde { get; set; } = false;
    public decimal Recarfin { get; set; } = 0.0m;
    public bool Recargo { get; set; } = false;
    public bool Recc { get; set; } = false;
    [MaxLength(25)] public string ReferCat { get; set; } = string.Empty;
    public bool Refundir { get; set; } = false;
    public bool Regcaja { get; set; } = false;
    public bool Retencion { get; set; } = false;
    public bool Retnofisc { get; set; } = false;
    [MaxLength(2)] public string Ruta { get; set; } = string.Empty;
    [MaxLength(100)] public string Skype { get; set; } = string.Empty;
    public bool SyncCtc { get; set; } = false;
    [MaxLength(2)] public string Tarifa { get; set; } = string.Empty;
    public int Territerp { get; set; } = 0;
    [MaxLength(3)] public string Tipcredit { get; set; } = string.Empty;
    public int TipoCli { get; set; } = 0;
    [MaxLength(2)] public string TipoIva { get; set; } = string.Empty;
    [MaxLength(2)] public string TipoRet { get; set; } = string.Empty;
    [MaxLength(2)] public string Tipofac { get; set; } = string.Empty;
    public decimal Tpcretnofi { get; set; } = 0.0m;
    [MaxLength(254)] public string Twitter { get; set; } = string.Empty;
    public decimal ValPunt { get; set; } = 0.0m;
    public string Telefono { get; set; } = string.Empty;
    [MaxLength(64)] public string Validcheck { get; set; } = string.Empty;
    public bool ValorAlb { get; set; } = false;
    public decimal Valportes { get; set; } = 0.0m;
    [MaxLength(5)] public string Vendedor { get; set; } = string.Empty;
    public bool? Vista { get; set; }
    [MaxLength(4)] public string Zona { get; set; } = string.Empty;
    public string Key => $"{Codigo?.Trim()}_{Clienteerp?.Trim()}";
}
