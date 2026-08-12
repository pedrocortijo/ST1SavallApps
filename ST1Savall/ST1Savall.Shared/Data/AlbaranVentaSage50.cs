namespace ST1Savall.Shared.Data;

/// <summary>Cabecera del albarán de venta de Sage 50 (tabla c_albven).</summary>
public class AlbaranVentaSage50
{
    public string USUARIO { get; set; } = string.Empty;
    public string EMPRESA { get; set; } = string.Empty;
    public string NUMERO { get; set; } = string.Empty;
    public DateTime FECHA { get; set; }
    public string CLIENTE { get; set; } = string.Empty;
    public int ENV_CLI { get; set; }
    public string PRESUP { get; set; } = string.Empty;
    public decimal PRONTO { get; set; }
    public string VENDEDOR { get; set; } = string.Empty;
    public string RUTA { get; set; } = string.Empty;
    public string ALMACEN { get; set; } = string.Empty;
    public bool IVA_INC { get; set; }
    public string FACTURA { get; set; } = string.Empty;
    public DateTime? FECHA_FAC { get; set; }
    public string ASI { get; set; } = string.Empty;
    public string FPAG { get; set; } = string.Empty;
    public decimal IMPORTE { get; set; }
    public string OBSERVACIO { get; set; } = string.Empty;
    public int BANC_CLI { get; set; }
    public string DIVISA { get; set; } = string.Empty;
    public decimal CAMBIO { get; set; }
    public decimal IMPDIVISA { get; set; }
    public decimal FINAN { get; set; }
    public bool? VISTA { get; set; }
    public decimal COSTE { get; set; }
    public decimal PESO { get; set; }
    public decimal LITROS { get; set; }
    public string OBRA { get; set; } = string.Empty;
    public bool TRASPASADO { get; set; }
    public bool RECEQUIV { get; set; }
    public bool TAG { get; set; }
    public string OPERARIO { get; set; } = string.Empty;
    public string LETRA { get; set; } = string.Empty;
    public bool FACTURABLE { get; set; }
    public string PEDIDO { get; set; } = string.Empty;
    public decimal COT_PUNT { get; set; }
    public decimal PUNTOS { get; set; }
    public bool COMMS { get; set; }
    public bool SEND_FRA { get; set; }
    public string CLIFINAL { get; set; } = string.Empty;
    public string KEYCOPY { get; set; } = string.Empty;
    public bool IMPRESO { get; set; }
    public string LIBRE_1 { get; set; } = string.Empty;
    public string LIBRE_2 { get; set; } = string.Empty;
    public string LIBRE_3 { get; set; } = string.Empty;
    public int CERTIFIC { get; set; }
    public decimal STOCK_COEF { get; set; }
    public decimal TPCRETNOFI { get; set; }
    public bool EDI { get; set; }
    public bool GASTOS { get; set; }
    public int ENVIADO { get; set; }
    public int LIBRE_4 { get; set; }
    public DateTime FECHASTOCK { get; set; }
    public DateTime? EXPORTAR { get; set; }
    public string MANDATO { get; set; } = string.Empty;
    public string CLIENTEERP { get; set; } = string.Empty;
    public bool RECC { get; set; }
    public string GUID_EXP { get; set; } = string.Empty;
    public decimal TOTALDOC { get; set; }
    public string CODPOST { get; set; } = string.Empty;
    public string CANAL { get; set; } = string.Empty;
    public bool TRASPERP { get; set; }
    public string GUID_ID { get; set; } = string.Empty;
    public DateTime CREATED { get; set; }
    public DateTime MODIFIED { get; set; }
    public decimal TOTALDIV { get; set; }
    public decimal PORCEN_RET { get; set; }
    public int CALCULO { get; set; }
    public string DESCFAC { get; set; } = string.Empty;
    public string REFERCLI { get; set; } = string.Empty;
    public bool FRADIRECTA { get; set; }
    public int MODO_RET { get; set; }
    public bool GIRMESCOMP { get; set; }
    public string VALIDCHECK { get; set; } = string.Empty;
}
