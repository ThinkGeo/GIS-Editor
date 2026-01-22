using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using ThinkGeo.Core;
using ThinkGeo.UI.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public static class GisEditorWpfMapLegacyExtension
    {
        private const int DefaultZoomPercentage = 20;

        public static void Refresh(this GisEditorWpfMap map)
        {
            if (map == null) return;
            _ = map.RefreshAsync();
        }

        public static void Refresh(this GisEditorWpfMap map, Overlay overlay)
        {
            if (map == null) return;
            _ = map.RefreshAsync();
        }

        public static void Refresh(this GisEditorWpfMap map, IEnumerable<Overlay> overlays)
        {
            if (map == null) return;
            _ = map.RefreshAsync();
        }

        public static void ZoomIn(this GisEditorWpfMap map)
        {
            if (map == null || map.CurrentExtent == null) return;
            map.CurrentExtent = MapUtil.ZoomIn(map.CurrentExtent, DefaultZoomPercentage);
            _ = map.RefreshAsync();
        }

        public static void ZoomOut(this GisEditorWpfMap map)
        {
            if (map == null || map.CurrentExtent == null) return;
            map.CurrentExtent = MapUtil.ZoomOut(map.CurrentExtent, DefaultZoomPercentage);
            _ = map.RefreshAsync();
        }

        public static void ZoomTo(this GisEditorWpfMap map, PointShape center, double scale)
        {
            if (map == null || center == null) return;
            var extent = MapUtils.CalculateExtent(center, scale, map.MapUnit, map.ActualWidth, map.ActualHeight);
            map.CurrentExtent = extent;
            _ = map.RefreshAsync();
        }

        public static void CenterAt(this GisEditorWpfMap map, PointShape center)
        {
            if (map == null || center == null) return;
            var extent = MapUtils.CalculateExtent(center, map.CurrentScale, map.MapUnit, map.ActualWidth, map.ActualHeight);
            map.CurrentExtent = extent;
            _ = map.RefreshAsync();
        }

        public static void ZoomToPreviousExtent(this GisEditorWpfMap map)
        {
            if (map == null) return;

            var zoomPrev = typeof(MapView).GetMethod("ZoomToPreviousExtent", BindingFlags.Instance | BindingFlags.Public);
            if (zoomPrev != null)
            {
                zoomPrev.Invoke(map, null);
                return;
            }

            var historyProp = typeof(MapView).GetProperty("HistoryExtents", BindingFlags.Instance | BindingFlags.Public);
            var indexProp = typeof(MapView).GetProperty("HistoryExtentsCurrentIndex", BindingFlags.Instance | BindingFlags.Public);
            if (historyProp != null && indexProp != null && indexProp.CanRead)
            {
                var historyObj = historyProp.GetValue(map);
                if (historyObj is System.Collections.IList history && history.Count > 0)
                {
                    int index;
                    try
                    {
                        index = Convert.ToInt32(indexProp.GetValue(map));
                    }
                    catch
                    {
                        index = 0;
                    }

                    if (index > 0)
                    {
                        if (indexProp.CanWrite)
                        {
                            indexProp.SetValue(map, index - 1);
                            return;
                        }

                        if (history[index - 1] is RectangleShape prevExtent)
                        {
                            map.CurrentExtent = prevExtent;
                            _ = map.RefreshAsync();
                            return;
                        }
                    }
                }
            }
        }

        public static IMapArguments GetMapArguments(this GisEditorWpfMap map)
        {
            if (map == null) return null;

            if (map.ExtentOverlay != null && map.ExtentOverlay.MapArguments != null)
            {
                return map.ExtentOverlay.MapArguments;
            }

            var overlay = map.InteractiveOverlays.FirstOrDefault();
            return overlay?.MapArguments;
        }

        public static RectangleShape GetMaxExtent(this GisEditorWpfMap map)
        {
            if (map == null) return null;

            var prop = typeof(MapView).GetProperty("MaxExtent", BindingFlags.Instance | BindingFlags.Public)
                ?? typeof(MapView).GetProperty("MaximumExtent", BindingFlags.Instance | BindingFlags.Public);
            if (prop != null)
            {
                try
                {
                    return prop.GetValue(map, null) as RectangleShape;
                }
                catch
                {
                    // fall through
                }
            }

            return map.CurrentExtent;
        }
    }
}
