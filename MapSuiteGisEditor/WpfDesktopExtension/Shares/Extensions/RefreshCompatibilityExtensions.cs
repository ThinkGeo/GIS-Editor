using System;
using ThinkGeo.UI.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    /// <summary>
    /// Compatibility shims for legacy GIS Editor extension code.
    ///
    /// Older versions exposed Refresh overloads that accepted a buffer time and a
    /// "request drawing" strategy. ThinkGeo v14+ streamlined a number of drawing APIs.
    ///
    /// These helpers keep the extension buildable and map the older calls to a
    /// simple <see cref="MapView.Refresh()"/> / <see cref="Overlay.Refresh()"/>.
    /// </summary>
    public static class RefreshCompatibilityExtensions
    {
        //public static void Refresh(this MapView mapView, TimeSpan bufferTime, RequestDrawingBufferTimeType bufferTimeType)
        //{
        //    if (mapView == null) return;
        //    mapView.Refresh();
        //}

        //public static void Refresh(this MapView mapView, Overlay overlay, TimeSpan bufferTime, RequestDrawingBufferTimeType bufferTimeType)
        //{
        //    if (mapView == null) return;

        //    // Best-effort: many versions can refresh a single overlay, but the signature varies.
        //    // Falling back to refreshing the entire map keeps the behavior predictable.
        //    mapView.Refresh();
        //}

        //public static void Refresh(this Overlay overlay, TimeSpan bufferTime, RequestDrawingBufferTimeType bufferTimeType)
        //{
        //    if (overlay == null) return;
        //    overlay.Refresh();
        //}

        public static void RefreshWithBufferSettings(this TileOverlay overlay)
        {
            if (overlay == null) return;
            _ = overlay.RefreshAsync();
        }

        public static void RefreshWithBufferSettings(this LayerOverlay overlay)
        {
            if (overlay == null) return;
            _ = overlay.RefreshAsync();
        }
    }

    /// <summary>
    /// Legacy enum used by older GIS Editor extension code to control how delayed refresh
    /// requests should behave.
    /// </summary>
    public enum RequestDrawingBufferTimeType
    {
        ResetDelay,
        KeepDelay
    }
}
