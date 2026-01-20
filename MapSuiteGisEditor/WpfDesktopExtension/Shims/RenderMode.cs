namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    /// <summary>
    /// Legacy render-mode switch used by the v10 GIS Editor extension.
    ///
    /// In v14 the default WPF rendering path is used for interactive overlays; the enum is
    /// kept primarily for source compatibility.
    /// </summary>
    public enum RenderMode
    {
        DrawingVisual = 0,
        GdiPlus = 1
    }
}
