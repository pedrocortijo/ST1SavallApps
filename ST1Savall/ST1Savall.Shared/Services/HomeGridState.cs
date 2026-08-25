using DevExpress.Blazor;

namespace ST1Savall.Shared.Services;

public sealed class HomeGridState
{
    public GridPersistentLayout? Layout { get; set; }
    public int SelectedConductorId { get; set; } = -1;
    public int SelectedTareaId { get; set; } = -1;
    public int SelectedEstadoId { get; set; } = -1;
    public int SelectedPrioridadId { get; set; } = -1;
    public int VistaDashboardIndex { get; set; } = 0;

    public void Clear()
    {
        Layout = null;
        SelectedConductorId = -1;
        SelectedTareaId = -1;
        SelectedEstadoId = -1;
        SelectedPrioridadId = -1;
        VistaDashboardIndex = 0;
    }
}
