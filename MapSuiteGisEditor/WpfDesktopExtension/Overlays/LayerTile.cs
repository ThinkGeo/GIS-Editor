using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ThinkGeo.Core;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    /// <summary>
    /// Lightweight tile element used by several legacy interactive overlays.
    ///
    /// ThinkGeo v10 exposed <c>LayerTile</c> in the WPF UI library. In ThinkGeo v14+
    /// the public tile UI classes have changed. The GIS Editor extension only
    /// needs a simple WPF element that can host a rendered image plus a few
    /// metadata fields (target extent, zoom index, etc.).
    ///
    /// This implementation keeps those overlays buildable and functional.
    /// </summary>
    public class LayerTile : ContentControl
    {
        private readonly Image image;

        public LayerTile()
        {
            image = new Image
            {
                Stretch = Stretch.Fill,
                SnapsToDevicePixels = true
            };

            SnapsToDevicePixels = true;
            ClipToBounds = true;
            Content = image;

            DrawingLayers = new Collection<Layer>();
        }

        /// <summary>
        /// Gets the layers rendered onto this tile.
        /// </summary>
        public Collection<Layer> DrawingLayers { get; }

        public RectangleShape TargetExtent { get; set; }

        public Point UpperLeftPointInPixel { get; set; }

        public int ZoomLevelIndex { get; set; }

        /// <summary>
        /// Kept for API compatibility. The legacy overlays set this to false and
        /// draw synchronously.
        /// </summary>
        public bool IsAsync { get; set; }

        /// <summary>
        /// Optional cache reference (not required by the legacy overlays).
        /// </summary>
        public RasterTileCache TileCache { get; set; }

        /// <summary>
        /// Draws all layers in <see cref="DrawingLayers"/> onto the provided canvas.
        /// </summary>
        public void Draw(GeoCanvas geoCanvas)
        {
            if (geoCanvas == null) return;

            // The legacy implementation draws layers in reverse order.
            var labels = new Collection<SimpleCandidate>();

            foreach (var layer in DrawingLayers.Where(l => l != null).Reverse())
            {
                // Layer.Draw is public in ThinkGeo.Core.
                layer.Draw(geoCanvas, labels);
            }
        }

        /// <summary>
        /// Commits a rendered image to the tile UI.
        /// </summary>
        public void CommitDrawing(GeoCanvas geoCanvas, ImageSource imageSource)
        {
            // geoCanvas is kept in the signature for compatibility.
            image.Source = imageSource;
        }
    }
}
