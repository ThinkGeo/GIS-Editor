using ThinkGeo.UI.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    /// <summary>
    /// Compatibility wrapper for older code that was built against the v10 WPF drawing canvas
    /// type name <c>DrawingVisualGeoCanvas</c>.
    ///
    /// ThinkGeo v14+ uses <see cref="WpfDrawingGeoCanvas"/>; this thin wrapper lets the legacy
    /// extension source compile while keeping behavior consistent.
    /// </summary>
    public class DrawingVisualGeoCanvas : WpfDrawingGeoCanvas
    {
    }
}
