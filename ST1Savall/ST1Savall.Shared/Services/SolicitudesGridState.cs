using DevExpress.Blazor;

namespace ST1Savall.Shared.Services;

public sealed class SolicitudesGridState
{
    public GridPersistentLayout? Layout { get; set; }
    public int SelectedConductorId { get; set; } = -1;
    public int SelectedTareaId { get; set; } = -1;
    public int SelectedEstadoId { get; set; } = -1;
    public int SelectedPrioridadId { get; set; } = -1;

    public void Clear()
    {
        Layout = null;
        SelectedConductorId = -1;
        SelectedTareaId = -1;
        SelectedEstadoId = -1;
        SelectedPrioridadId = -1;
    }
}
