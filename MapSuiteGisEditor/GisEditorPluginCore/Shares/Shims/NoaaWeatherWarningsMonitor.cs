using System;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class NoaaWeatherWarningsMonitor
    {
        public static TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(5);

        public static void StartMonitoring()
        {
            // No-op shim for legacy NOAA warnings monitoring.
        }

        public static void StopMonitoring()
        {
            // No-op shim for legacy NOAA warnings monitoring.
        }
    }
}
