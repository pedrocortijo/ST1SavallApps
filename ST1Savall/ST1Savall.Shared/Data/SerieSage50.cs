namespace ST1Savall.Shared.Data;

/// <summary>Contador de documentos de Sage 50 (tabla series).</summary>
public class SerieSage50
{
    public string EMPRESA { get; set; } = string.Empty;
    public int TIPODOC { get; set; }
    public string SERIE { get; set; } = string.Empty;
    public decimal CONTADOR { get; set; }

    public string Clave => $"{EMPRESA.Trim()}|{TIPODOC}|{SERIE.Trim()}";
}
