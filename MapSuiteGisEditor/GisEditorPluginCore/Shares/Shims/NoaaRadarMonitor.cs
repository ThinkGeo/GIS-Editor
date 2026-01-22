using System;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class NoaaRadarMonitor
    {
        public static TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(5);

        public static void StartMonitoring()
        {
            // No-op shim for legacy NOAA monitoring.
        }

        public static void StopMonitoring()
        {
            // No-op shim for legacy NOAA monitoring.
        }
    }
}
