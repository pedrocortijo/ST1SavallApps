using System;

namespace ST1Savall.Shared.Data;

public class ClienteSage50
{
    public string Agencia { get; set; } = string.Empty;
    public bool Albafra { get; set; } = false;
    public string BancoPrev { get; set; } = string.Empty;
    public bool BloqCli { get; set; } = false;
    public bool BloqVen { get; set; } = false;
    public bool Bloqalbvta { get; set; } = false;
    public bool Bloqdepvta { get; set; } = false;
    public bool Bloqpedvta { get; set; } = false;
    public bool Bloqprevta { get; set; } = false;
    public string CEnt { get; set; } = string.Empty;
    public decimal Cambio { get; set; } = 0.0m;
    public string Canal { get; set; } = string.Empty;
    public string CiaCred { get; set; } = string.Empty;
    public string Cif { get; set; } = string.Empty;
    public string Clienteerp { get; set; } = string.Empty;
    public string Clifinal { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Codpost { get; set; } = string.Empty;
    public int Comunitari { get; set; } = 0;
    public bool Contado { get; set; } = false;
    public string Contrapar { get; set; } = string.Empty;
    public int CopiaFra { get; set; } = 0;
    public DateTime Created { get; set; } = DateTime.Now;
    public decimal Credito { get; set; } = 0.0m;
    public bool Csb { get; set; } = false;
    public string Ctaerp { get; set; } = string.Empty;
    public string Delegerp { get; set; } = string.Empty;
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
    public string Dire { get; set; } = string.Empty;
    public string Direcc2erp { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public bool EfAdjfact { get; set; } = false;
    public string EfCuebanc { get; set; } = string.Empty;
    public bool EfEnvmail { get; set; } = false;
    public int EfFormat { get; set; } = 0;
    public string EfTexmail { get; set; } = string.Empty;
    public int EfTipodoc { get; set; } = 0;
    public string Email { get; set; } = string.Empty;
    public string EmailF { get; set; } = string.Empty;
    public int EnvCli { get; set; } = 0;
    public bool EsGrupo { get; set; } = false;
    public string Eticomu { get; set; } = string.Empty;
    public bool Excluir349 { get; set; } = false;
    public DateTime? Exportar { get; set; }
    public DateTime FAlta { get; set; } = DateTime.Now;
    public string Facebook { get; set; } = string.Empty;
    public DateTime? Fbloqnocar { get; set; }
    public DateTime? Fbloqnoema { get; set; }
    public DateTime? Fbloqnosms { get; set; }
    public DateTime? FecCam { get; set; }
    public DateTime? FechaBaj { get; set; }
    public string Fpag { get; set; } = string.Empty;
    public bool Fraesi { get; set; } = false;
    public bool Fraped { get; set; } = false;
    public bool Girmescomp { get; set; } = false;
    public string Guid { get; set; } = string.Empty;
    public string GuidExp { get; set; } = string.Empty;
    public string GuidId { get; set; } = string.Empty;
    public string Http { get; set; } = string.Empty;
    public string Idioma { get; set; } = "000";
    public string IdiomaImp { get; set; } = string.Empty;
    public DateTime? Importar { get; set; }
    public bool Isp { get; set; } = false;
    public string Letdefrect { get; set; } = string.Empty;
    public string Letdefven { get; set; } = string.Empty;
    public string Libre1 { get; set; } = string.Empty;
    public int LimMon { get; set; } = 0;
    public string LinDes { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime Modified { get; set; } = DateTime.Now;
    public bool ModoRet { get; set; } = false;
    public int Modretnofi { get; set; } = 0;
    public bool Nocomucar { get; set; } = false;
    public bool Nocomuema { get; set; } = false;
    public string Nocomuobs { get; set; } = string.Empty;
    public bool Nocomusms { get; set; } = false;
    public string Nombre { get; set; } = string.Empty;
    public string Nombre2 { get; set; } = string.Empty;
    public string Nombre3erp { get; set; } = string.Empty;
    public string? Observacio { get; set; }
    public bool Oferta { get; set; } = false;
    public string Operacio { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public string Plefact { get; set; } = string.Empty;
    public string Poblacerp { get; set; } = string.Empty;
    public string Poblacion { get; set; } = string.Empty;
    public decimal Portcomp { get; set; } = 0.0m;
    public string Portes { get; set; } = string.Empty;
    public decimal Posicion { get; set; } = 0.0m;
    public bool Pregvac { get; set; } = false;
    public decimal Pronto { get; set; } = 0.0m;
    public string Provincia { get; set; } = string.Empty;
    public string Provinerp { get; set; } = string.Empty;
    public bool Pverde { get; set; } = false;
    public decimal Recarfin { get; set; } = 0.0m;
    public bool Recargo { get; set; } = false;
    public bool Recc { get; set; } = false;
    public string ReferCat { get; set; } = string.Empty;
    public bool Refundir { get; set; } = false;
    public bool Regcaja { get; set; } = false;
    public bool Retencion { get; set; } = false;
    public bool Retnofisc { get; set; } = false;
    public string Ruta { get; set; } = string.Empty;
    public string Skype { get; set; } = string.Empty;
    public bool SyncCtc { get; set; } = false;
    public string Tarifa { get; set; } = string.Empty;
    public int Territerp { get; set; } = 0;
    public string Tipcredit { get; set; } = string.Empty;
    public int TipoCli { get; set; } = 0;
    public string TipoIva { get; set; } = string.Empty;
    public string TipoRet { get; set; } = string.Empty;
    public string Tipofac { get; set; } = string.Empty;
    public decimal Tpcretnofi { get; set; } = 0.0m;
    public string Twitter { get; set; } = string.Empty;
    public decimal ValPunt { get; set; } = 0.0m;
    public string Telefono { get; set; } = string.Empty;
    public string Validcheck { get; set; } = string.Empty;
    public bool ValorAlb { get; set; } = false;
    public decimal Valportes { get; set; } = 0.0m;
    public string Vendedor { get; set; } = string.Empty;
    public bool? Vista { get; set; }
    public string Zona { get; set; } = string.Empty;
    public string Key => $"{Codigo?.Trim()}_{Clienteerp?.Trim()}";
}