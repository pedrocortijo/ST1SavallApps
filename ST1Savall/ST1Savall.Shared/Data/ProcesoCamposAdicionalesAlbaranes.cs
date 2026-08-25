namespace ST1Savall.Shared.Data;

public sealed class ProcesoCamposAdicionalesAlbaranesRequest
{
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
}

public sealed class ResultadoProcesoCamposAdicionalesAlbaranes
{
    public int Total { get; set; }
    public int Actualizados { get; set; }
    public int ConAviso { get; set; }
    public List<DetalleProcesoCamposAdicionalesAlbaran> Detalles { get; set; } = [];
}

public sealed class DetalleProcesoCamposAdicionalesAlbaran
{
    public DateTime Fecha { get; set; }
    public string Serie { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string NetoKg { get; set; } = string.Empty;
    public string AlbaranPlanta { get; set; } = string.Empty;
    public string FechaPlanta { get; set; } = string.Empty;
    public bool Actualizado { get; set; }
    public string? Aviso { get; set; }
}
