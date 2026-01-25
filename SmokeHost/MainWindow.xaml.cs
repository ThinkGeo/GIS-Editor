using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ThinkGeo.Core;
using ThinkGeo.MapSuite.WpfDesktop.Extension;
using ThinkGeo.UI.Wpf;

namespace ThinkGeo.SmokeHost
{
    public partial class MainWindow : Window
    {
        private LayerOverlay dataOverlay;
        private InMemoryFeatureLayer dataLayer;

        private int pointMoveStep;
        private long refreshSequence;

        // Keep the initial demo extent stable so you can compare v10/v14 behavior easily.
        private static readonly RectangleShape DemoExtent = new RectangleShape(
            -125, 50,   // upper-left  (lon, lat)
            -66, 24);   // lower-right (lon, lat)

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Auto-init on startup for convenience.
            await InitAsync("Window_Loaded");
        }

        private async void Init_Click(object sender, RoutedEventArgs e)
        {
            await InitAsync("Init_Click");
        }

        private async Task InitAsync(string source)
        {
            Log($">> Init ({source})");

            // Reset state
            pointMoveStep = 0;
            refreshSequence = 0;

            // Map basics
            Map.MapUnit = GeographyUnit.DecimalDegree;
            Map.CurrentExtent = DemoExtent;

            // Clear custom overlays (AdornmentOverlay lives on a separate property in GisEditorWpfMap).
            Map.Overlays.Clear();

            // Build layer + overlay
            dataLayer = BuildDemoLayer(pointMoveStep);
            dataOverlay = new LayerOverlay { Name = "SmokeHost.DataOverlay" };
            dataOverlay.Layers.Add(dataLayer);

            Map.Overlays.Add(dataOverlay);
            Map.ActiveOverlay = dataOverlay;

            HookMapEventsOnce();

            await RequestRefreshAsync(source: "InitAsync", overlayOnly: false);
        }

        private void HookMapEventsOnce()
        {
            // Avoid duplicate subscriptions if Init is clicked multiple times.
            Map.OverlaysDrawn -= Map_OverlaysDrawn;
            Map.CurrentExtentChanged -= Map_CurrentExtentChanged;
            Map.CurrentScaleChanged -= Map_CurrentScaleChanged;

            Map.OverlaysDrawn += Map_OverlaysDrawn;
            Map.CurrentExtentChanged += Map_CurrentExtentChanged;
            Map.CurrentScaleChanged += Map_CurrentScaleChanged;
        }

        private void Map_CurrentExtentChanged(object sender, CurrentExtentChangedMapViewEventArgs e)
        {
            Log($"[Event] CurrentExtentChanged  extent={FormatExtent(Map.CurrentExtent)}");
            UpdateStatus();
        }

        private void Map_CurrentScaleChanged(object sender, CurrentScaleChangedMapViewEventArgs e)
        {
            Log($"[Event] CurrentScaleChanged  scale={Map.CurrentScale.ToString("N0", CultureInfo.InvariantCulture)}");
            UpdateStatus();
        }

        private void Map_OverlaysDrawn(object sender, OverlaysDrawnMapViewEventArgs e)
        {
            // OverlaysDrawn is your best "render completed" signal when chasing async refresh timing.
            Log($"[Event] OverlaysDrawn  overlays={Map.Overlays.Count}");
            UpdateStatus();
        }

        private async void ZoomToData_Click(object sender, RoutedEventArgs e)
        {
            Map.CurrentExtent = DemoExtent;
            await RequestRefreshAsync(source: "ZoomToData_Click", overlayOnly: false);
        }

        private async void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ZoomBy(factor: 0.5);
            await RequestRefreshAsync(source: "ZoomIn_Click", overlayOnly: false);
        }

        private async void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ZoomBy(factor: 2.0);
            await RequestRefreshAsync(source: "ZoomOut_Click", overlayOnly: false);
        }

        private async void MovePoint_Click(object sender, RoutedEventArgs e)
        {
            pointMoveStep++;

            // Rebuild demo layer contents deterministically.
            dataLayer = BuildDemoLayer(pointMoveStep);

            // Replace the layer instance in the overlay (keeps the overlay stable).
            dataOverlay.Layers.Clear();
            dataOverlay.Layers.Add(dataLayer);

            await RequestRefreshAsync(source: $"MovePoint_Click step={pointMoveStep}", overlayOnly: true);
        }

        private async void RefreshOverlay_Click(object sender, RoutedEventArgs e)
        {
            await RequestRefreshAsync(source: "RefreshOverlay_Click", overlayOnly: true);
        }

        private async void RefreshMap_Click(object sender, RoutedEventArgs e)
        {
            await RequestRefreshAsync(source: "RefreshMap_Click", overlayOnly: false);
        }


        private void RefreshLegacy_Click(object sender, RoutedEventArgs e)
        {
            // Uses the v10-style shim: non-awaited, fire-and-forget refresh.
            Log("-- Refresh (legacy shim) requested");

            if (dataOverlay != null)
            {
                // Extension method lives in ThinkGeo.MapSuite.WpfDesktop.Extension namespace.
                Map.Refresh(dataOverlay);
            }
            else
            {
                Map.Refresh();
            }
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
        }

        private async Task RequestRefreshAsync(string source, bool overlayOnly)
        {
            var seq = Interlocked.Increment(ref refreshSequence);
            var scope = overlayOnly ? "overlay" : "map";

            Log($"-- #{seq} RefreshAsync({scope}) requested  source={source}");

            try
            {
                if (overlayOnly && dataOverlay != null)
                {
                    await Map.RefreshAsync(dataOverlay);
                }
                else
                {
                    await Map.RefreshAsync();
                }

                Log($"-- #{seq} RefreshAsync({scope}) completed");
            }
            catch (Exception ex)
            {
                Log($"!! #{seq} RefreshAsync({scope}) FAILED: {ex}");
                throw;
            }
        }

        private void ZoomBy(double factor)
        {
            var extent = Map.CurrentExtent;
            if (extent == null || extent.Width <= 0 || extent.Height <= 0) return;

            var centerX = (extent.UpperLeftPoint.X + extent.LowerRightPoint.X) / 2.0;
            var centerY = (extent.UpperLeftPoint.Y + extent.LowerRightPoint.Y) / 2.0;

            var halfWidth = (extent.Width * factor) / 2.0;
            var halfHeight = (extent.Height * factor) / 2.0;

            Map.CurrentExtent = new RectangleShape(
                centerX - halfWidth, centerY + halfHeight,
                centerX + halfWidth, centerY - halfHeight);
        }

        private static InMemoryFeatureLayer BuildDemoLayer(int step)
        {
            var layer = new InMemoryFeatureLayer
            {
                Name = "SmokeHost.DemoLayer"
            };

            // Geometry: one area, one line, one moving point.
            layer.InternalFeatures.Add(new Feature(
                new PolygonShape("POLYGON((-110 45, -90 45, -90 35, -110 35, -110 45))"))
            {
                Id = "Area"
            });

            var lineVertices = new Collection<Vertex>
            {
                new Vertex(-120, 30),
                new Vertex(-100, 42),
                new Vertex(-80, 33)
            };
            layer.InternalFeatures.Add(new Feature(new LineShape(lineVertices))
            {
                Id = "Line"
            });

            var movingX = -96 + (step * 1.0);
            var movingY = 41 + (step * 0.5);
            layer.InternalFeatures.Add(new Feature(new PointShape(movingX, movingY))
            {
                Id = "Point"
            });

            // Styles: keep them simple; the purpose here is verifying draw/refresh/scale/extent timing.
            layer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle =
                AreaStyle.CreateSimpleAreaStyle(GeoColors.LightGreen, GeoColors.Gray);

            layer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle =
                LineStyle.CreateSimpleLineStyle(GeoColors.Blue, 2, true);

            layer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle =
                PointStyle.CreateSimpleCircleStyle(GeoColors.Red, 10, GeoColors.Black);

            layer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;

            return layer;
        }

        private void UpdateStatus()
        {
            StatusText.Text =
                $"Scale: {Map.CurrentScale.ToString("N0", CultureInfo.InvariantCulture)}\n" +
                $"Extent: {FormatExtent(Map.CurrentExtent)}\n" +
                $"ZoomScales: {(Map.ZoomScales == null ? 0 : Map.ZoomScales.Count)}\n" +
                $"Overlays: {Map.Overlays.Count}";
        }

        private void Log(string message)
        {
            var ts = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var tid = Thread.CurrentThread.ManagedThreadId;

            LogTextBox.AppendText($"{ts} [T{tid}] {message}{Environment.NewLine}");
            LogTextBox.ScrollToEnd();

            System.Diagnostics.Debug.WriteLine(message);
        }

        private static string FormatExtent(RectangleShape extent)
        {
            if (extent == null) return "<null>";
            return $"UL({extent.UpperLeftPoint.X:0.####},{extent.UpperLeftPoint.Y:0.####})  LR({extent.LowerRightPoint.X:0.####},{extent.LowerRightPoint.Y:0.####})";
        }
    }
}
