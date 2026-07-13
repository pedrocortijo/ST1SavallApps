using DevExpress.Blazor;

namespace ST1Savall.Shared.Services;

public sealed class ObrasMntoGridState
{
    public GridPersistentLayout? Layout { get; set; }
    public bool IsMapNavigationFromObras { get; set; }

    public void Clear()
    {
        Layout = null;
        IsMapNavigationFromObras = false;
    }
}
