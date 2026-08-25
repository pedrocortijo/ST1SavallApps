namespace ST1Savall.Shared.Data;

public sealed class ProcesoAdjudicarKgPlantasRequest
{
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
}

public sealed class ResultadoProcesoAdjudicarKgPlantas
{
    public int Revisados { get; set; }
    public int Actualizados { get; set; }
    public int SinDatos { get; set; }
    public List<DetalleProcesoAdjudicarKgPlantas> Detalles { get; set; } = [];
}

public sealed class DetalleProcesoAdjudicarKgPlantas
{
    public int IdSolicitud { get; set; }
    public DateTime? FechaServicio { get; set; }
    public string AlbaranPlanta { get; set; } = string.Empty;
    public string AlbaranSage { get; set; } = string.Empty;
    public int Kg { get; set; }
    public bool Actualizado { get; set; }
}
