using System;
using System.Collections.Generic;
using ThinkGeo.Core;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    /// <summary>
    /// Raised before an individual AdornmentLayer draws.
    /// </summary>
    public class AdornmentLayerDrawingEventArgs : EventArgs
    {
        public AdornmentLayerDrawingEventArgs(AdornmentLayer adornmentLayer)
        {
            AdornmentLayer = adornmentLayer;
        }

        public AdornmentLayer AdornmentLayer { get; }
    }

    /// <summary>
    /// Raised after an individual AdornmentLayer has drawn.
    /// </summary>
    public class AdornmentLayerDrawnEventArgs : EventArgs
    {
        public AdornmentLayerDrawnEventArgs(AdornmentLayer adornmentLayer)
        {
            AdornmentLayer = adornmentLayer;
        }

        public AdornmentLayer AdornmentLayer { get; }
    }

    /// <summary>
    /// Raised before all adornment layers draw.
    /// </summary>
    public class AdornmentLayersDrawingEventArgs : EventArgs
    {
        public AdornmentLayersDrawingEventArgs(IEnumerable<AdornmentLayer> adornmentLayers)
        {
            AdornmentLayers = adornmentLayers;
        }

        public IEnumerable<AdornmentLayer> AdornmentLayers { get; }
    }

    /// <summary>
    /// Raised after all adornment layers have drawn.
    /// </summary>
    public class AdornmentLayersDrawnEventArgs : EventArgs
    {
        public AdornmentLayersDrawnEventArgs(IEnumerable<AdornmentLayer> adornmentLayers)
        {
            AdornmentLayers = adornmentLayers;
        }

        public IEnumerable<AdornmentLayer> AdornmentLayers { get; }
    }
}
