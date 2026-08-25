namespace ST1Savall.Shared.Data;

/// <summary>Dirección alternativa de un cliente de Sage 50 (tabla ENV_CLI).</summary>
public class DireccionEnvioClienteSage50
{
    public string CLIENTE { get; set; } = string.Empty;
    public int LINEA { get; set; }
    public string DIRECCION { get; set; } = string.Empty;
    public string CODPOS { get; set; } = string.Empty;
    public string POBLACION { get; set; } = string.Empty;
    public string PROVINCIA { get; set; } = string.Empty;
}
