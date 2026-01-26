using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;

namespace ThinkGeo.Core
{
    public class OpenStreetMapLayer : OpenStreetMapAsyncLayer
    {
        public OpenStreetMapLayer()
            : base()
        {
        }

        public OpenStreetMapLayer(IWebProxy webProxy)
            : base(webProxy)
        {
        }

        public void Draw(GeoCanvas geoCanvas, Collection<SimpleCandidate> candidates)
        {
            LayerDrawHelper.TryDraw(this, geoCanvas, candidates);
        }
    }

    public class BingMapsLayer : BingMapsAsyncLayer
    {
        public BingMapsLayer()
            : base()
        {
        }

        public BingMapsLayer(string applicationId)
            : base(applicationId)
        {
        }

        public BingMapsLayer(string applicationId, BingMapsMapType mapType)
            : base(applicationId, mapType)
        {
        }

        public void Draw(GeoCanvas geoCanvas, Collection<SimpleCandidate> candidates)
        {
            LayerDrawHelper.TryDraw(this, geoCanvas, candidates);
        }

        // Legacy compatibility: Map Suite used Proxy and ProjectionFromSphericalMercator.
        public IWebProxy Proxy
        {
            get => WebProxy;
            set => WebProxy = value;
        }

        public ProjectionConverter ProjectionFromSphericalMercator { get; set; }
    }

    public static class LineStyles
    {
        public static LineStyle SimpleBlueLine => LineStyle.CreateSimpleLineStyle(GeoColors.Blue, 2, true);
        public static LineStyle SimpleBlackLine => LineStyle.CreateSimpleLineStyle(GeoColors.Black, 1, true);

        public static LineStyle CreateSimpleLineStyle(GeoColor lineColor, float width, bool isVisible)
        {
            return LineStyle.CreateSimpleLineStyle(lineColor, width, isVisible);
        }

        public static LineStyle CreateSimpleLineStyle(GeoColor lineColor, float width, GeoColor outerColor, float outerWidth, bool isVisible)
        {
            return LineStyle.CreateSimpleLineStyle(lineColor, width, outerColor, outerWidth, isVisible);
        }
    }

    public static class PointStyles
    {
        public static PointStyle City1 => PointStyle.CreateSimpleCircleStyle(GeoColor.FromArgb(255, 255, 153, 51), 6, GeoColors.Black, 1);
        public static PointStyle City4 => PointStyle.CreateSimpleCircleStyle(GeoColor.FromArgb(255, 102, 153, 255), 8, GeoColors.Black, 1);

        public static PointStyle CreateSimpleCircleStyle(GeoColor fillColor, float size)
        {
            return PointStyle.CreateSimpleCircleStyle(fillColor, size);
        }

        public static PointStyle CreateSimpleCircleStyle(GeoColor fillColor, float size, GeoColor outlineColor)
        {
            return PointStyle.CreateSimpleCircleStyle(fillColor, size, outlineColor);
        }

        public static PointStyle CreateSimpleCircleStyle(GeoColor fillColor, float size, GeoColor outlineColor, float outlineThickness)
        {
            return PointStyle.CreateSimpleCircleStyle(fillColor, size, outlineColor, outlineThickness);
        }
    }

    public static class AreaStyles
    {
        public static AreaStyle Antarctica1 => AreaStyle.CreateSimpleAreaStyle(GeoColor.FromArgb(255, 210, 225, 235), GeoColor.FromArgb(255, 120, 150, 170), 1);

        public static AreaStyle CreateSimpleAreaStyle(GeoColor fillColor, GeoColor outlineColor)
        {
            return AreaStyle.CreateSimpleAreaStyle(fillColor, outlineColor);
        }

        public static AreaStyle CreateSimpleAreaStyle(GeoColor fillColor, GeoColor outlineColor, int outlineWidth)
        {
            return AreaStyle.CreateSimpleAreaStyle(fillColor, outlineColor, outlineWidth);
        }
    }

    public static class TextStyles
    {
        public static TextStyle Antarctical(string text)
        {
            return TextStyle.CreateSimpleTextStyle(text, "Arial", 10, DrawingFontStyles.Bold, GeoColors.Black);
        }
    }

    public class KmlFeatureLayer
    {
        public static void BuildIndexFile(string pathFileName, BuildIndexMode buildIndexMode)
        {
            // No-op shim: KML indexing is not available in ThinkGeo.Core.
        }
    }

    internal static class LayerDrawHelper
    {
        internal static void TryDraw(object layer, GeoCanvas geoCanvas, Collection<SimpleCandidate> candidates)
        {
            if (layer == null || geoCanvas == null) return;

            var method = layer.GetType().GetMethod(
                "Draw",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(GeoCanvas), typeof(Collection<SimpleCandidate>) },
                null);

            if (method == null) return;
            if (method.DeclaringType == layer.GetType()) return;

            method.Invoke(layer, new object[] { geoCanvas, candidates });
        }
    }

    /// <summary>
    /// Adapter to allow a WmsAsyncLayer to be used where a Layer is required
    /// (LayerOverlay, MapEngine, etc.).
    /// </summary>
    public sealed class WmsAsyncLayerAdapter : Layer
    {
        public WmsAsyncLayerAdapter(WmsAsyncLayer innerLayer)
        {
            InnerLayer = innerLayer;
            if (innerLayer != null)
            {
                Name = innerLayer.Name;
                DrawingExceptionMode = innerLayer.DrawingExceptionMode;
            }
        }

        public WmsAsyncLayer InnerLayer { get; }

        protected override void DrawCore(GeoCanvas canvas, Collection<SimpleCandidate> labelsInAllLayers)
        {
            if (InnerLayer == null || canvas == null) return;

            if (!InnerLayer.IsOpen)
            {
                InnerLayer.Open();
            }

            InnerLayer.DrawAsync(canvas, labelsInAllLayers).GetAwaiter().GetResult();
        }

        protected override void OpenCore()
        {
            if (InnerLayer != null && !InnerLayer.IsOpen)
            {
                InnerLayer.Open();
            }
        }

        protected override void CloseCore()
        {
            if (InnerLayer != null && InnerLayer.IsOpen)
            {
                InnerLayer.Close();
            }
        }

        protected override RectangleShape GetBoundingBoxCore()
        {
            if (InnerLayer == null) return null;
            return InnerLayer.GetBoundingBox();
        }

        protected override LayerBase CloneDeepCore()
        {
            if (InnerLayer == null) return new WmsAsyncLayerAdapter(null);
            var clonedInner = InnerLayer.CloneDeep() as WmsAsyncLayer;
            return new WmsAsyncLayerAdapter(clonedInner ?? InnerLayer);
        }
    }

    /// <summary>
    /// Legacy compatibility helpers for WmsAsyncLayer.
    /// </summary>
    public static class WmsAsyncLayerLegacyExtensions
    {
        public static void Open(this WmsAsyncLayer layer)
        {
            if (layer == null || layer.IsOpen) return;
            layer.OpenAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public static void Close(this WmsAsyncLayer layer)
        {
            if (layer == null || !layer.IsOpen) return;
            layer.CloseAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public static Collection<string> GetServerLayerNames(this WmsAsyncLayer layer)
        {
            if (layer == null) return new Collection<string>();
            return new Collection<string>(layer.GetServerLayers().Select(l => l.Name).ToList());
        }

        public static Collection<string> GetServerCrss(this WmsAsyncLayer layer)
        {
            if (layer == null) return new Collection<string>();
            return new Collection<string>(layer.GetServerCrsCollection().ToList());
        }

        public static void InitializeProj4Projection(this WmsAsyncLayer layer, string internalProj4ProjectionParameters)
        {
            if (layer == null) return;
            if (string.IsNullOrWhiteSpace(internalProj4ProjectionParameters)) return;

            layer.Projection = new Projection(internalProj4ProjectionParameters);
        }
    }
}
