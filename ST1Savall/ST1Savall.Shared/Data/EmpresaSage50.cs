namespace ST1Savall.Shared.Data;

public class EmpresaSage50
{
    public int IdEmpresa { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? NombreCliente { get; set; }
    public string? DireccionCliente { get; set; }
    public string? PoblacionCliente { get; set; }
    public string? TelefonoCliente { get; set; }
    public string? CodigoPostalCliente { get; set; }

    public string SearchDisplayText => $"{IdEmpresa} - {Nombre}";
}
