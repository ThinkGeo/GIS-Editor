using ThinkGeo.Core;
using ThinkGeo.UI.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public static class LayerOverlayExtension
    {
        public static void Close(this LayerOverlay overlay)
        {
            if (overlay == null) return;

            foreach (var layer in overlay.Layers)
            {
                if (layer.IsOpen)
                {
                    if (layer is AsyncLayer asyncLayer)
                    {
                        _ = asyncLayer.CloseAsync();
                    }
                    else if (layer is Layer syncLayer)
                    {
                        syncLayer.Close();
                    }
                }
            }
        }
    }
}
