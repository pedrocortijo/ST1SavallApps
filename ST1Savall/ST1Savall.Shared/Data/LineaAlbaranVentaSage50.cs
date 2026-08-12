namespace ST1Savall.Shared.Data;

/// <summary>Línea del albarán de venta de Sage 50 (tabla d_albven).</summary>
public class LineaAlbaranVentaSage50
{
    public string USUARIO { get; set; } = string.Empty;
    public string EMPRESA { get; set; } = string.Empty;
    public string NUMERO { get; set; } = string.Empty;
    public string ARTICULO { get; set; } = string.Empty;
    public string DEFINICION { get; set; } = string.Empty;
    public decimal UNIDADES { get; set; }
    public decimal DTO1 { get; set; }
    public decimal DTO2 { get; set; }
    public string TIPO_IVA { get; set; } = string.Empty;
    public decimal COSTE { get; set; }
    public string CUENTA { get; set; } = string.Empty;
    public string PEDIDO { get; set; } = string.Empty;
    public DateTime? FECHA { get; set; }
    public int LINIA { get; set; }
    public string CLIENTE { get; set; } = string.Empty;
    public decimal PRECIO { get; set; }
    public decimal IMPORTE { get; set; }
    public decimal PRECIOIVA { get; set; }
    public decimal IMPORTEIVA { get; set; }
    public decimal CAJAS { get; set; }
    public string FAMILIA { get; set; } = string.Empty;
    public decimal PRECIODIV { get; set; }
    public decimal IMPORTEDIV { get; set; }
    public string SERIE { get; set; } = string.Empty;
    public int TIPO { get; set; }
    public decimal COMISION { get; set; }
    public decimal IMP_COM { get; set; }
    public decimal PESO { get; set; }
    public int DOC { get; set; }
    public string DOC_NUM { get; set; } = string.Empty;
    public int DOC_LIN { get; set; }
    public decimal DOC_UNID { get; set; }
    public bool? VISTA { get; set; }
    public decimal PVERDE { get; set; }
    public bool RECARG { get; set; }
    public string COLOR { get; set; } = string.Empty;
    public string LETRA { get; set; } = string.Empty;
    public string TALLA { get; set; } = string.Empty;
    public decimal IMPDIVIVA { get; set; }
    public decimal PREDIVIVA { get; set; }
    public bool LOTE { get; set; }
    public decimal PUNTOS { get; set; }
    public decimal DTO3_IMP { get; set; }
    public decimal DTO3_IMPDI { get; set; }
    public decimal DTO3_IVA { get; set; }
    public decimal DTO3_IVADI { get; set; }
    public string LIBRE_1 { get; set; } = string.Empty;
    public string LIBRE_2 { get; set; } = string.Empty;
    public string LIBRE_3 { get; set; } = string.Empty;
    public string LIBRE_4 { get; set; } = string.Empty;
    public string LIBRE_5 { get; set; } = string.Empty;
    public bool VENTASER { get; set; }
    public string ASI { get; set; } = string.Empty;
    public string PROVEEDOR { get; set; } = string.Empty;
    public decimal ESCANEADO { get; set; }
    public bool STOCKNO { get; set; }
    public string VENDEDOR { get; set; } = string.Empty;
    public decimal DIAS { get; set; }
    public decimal ACTUAL { get; set; }
    public decimal ANTERIOR { get; set; }
    public int CONTADOR { get; set; }
    public DateTime? FLACTUAL { get; set; }
    public DateTime? FLANTERIOR { get; set; }
    public decimal UNID_DIAS { get; set; }
    public decimal DOC_CAJA { get; set; }
    public string ESCANDAL { get; set; } = string.Empty;
    public string ALMACEN { get; set; } = string.Empty;
    public decimal TIPOPREC { get; set; }
    public string TIPO_IVAV { get; set; } = string.Empty;
    public string TIPO_ART { get; set; } = string.Empty;
    public decimal UNIMEDIDA { get; set; }
    public decimal PREMEDIDA { get; set; }
    public string NUMALBORI { get; set; } = string.Empty;
    public string LETALBORI { get; set; } = string.Empty;
    public int EJEALBORI { get; set; }
    public string CLIENTEERP { get; set; } = string.Empty;
    public string CODAGRUP { get; set; } = string.Empty;
    public decimal UNIAGRUP { get; set; }
    public bool? FACTURABLE { get; set; }
    public bool SUPLIDO { get; set; }
    public string GUID_ID { get; set; } = string.Empty;
    public DateTime CREATED { get; set; }
    public DateTime MODIFIED { get; set; }
    public string VALIDCHECK { get; set; } = string.Empty;
}
