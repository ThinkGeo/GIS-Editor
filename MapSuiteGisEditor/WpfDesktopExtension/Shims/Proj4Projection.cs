using System;
using System.Reflection;

namespace ThinkGeo.Core
{
    /// <summary>
    /// Compatibility shim for Map Suite v10's Proj4Projection.
    ///
    /// ThinkGeo.Core v12+ replaced <c>Proj4Projection</c> with <c>ProjectionConverter</c>. The
    /// legacy GIS Editor extension still references Proj4Projection and the proj4-string based
    /// API. To keep the editor compiling on v14, we provide a light wrapper that:
    ///
    /// - Inherits from <c>ProjectionConverter</c> so it can be assigned to <c>FeatureSource.Projection</c>.
    /// - Exposes the legacy <c>InternalProjectionParametersString</c> / <c>ExternalProjectionParametersString</c> properties.
    /// - Provides <c>GetEpsgParametersString</c> for common EPSG codes (and delegates to ThinkGeo if available).
    ///
    /// Note: The underlying ThinkGeo projection API may evolve; this shim uses reflection to
    /// set/get projection strings when the exact property names differ.
    /// </summary>
    public class Proj4Projection : ProjectionConverter
    {
        private string internalParametersString;
        private string externalParametersString;

        public Proj4Projection()
        {
        }

        public Proj4Projection(string internalProjectionParametersString, string externalProjectionParametersString)
        {
            InternalProjectionParametersString = internalProjectionParametersString;
            ExternalProjectionParametersString = externalProjectionParametersString;
        }

        /// <summary>
        /// Gets or sets the internal (source) projection parameters in proj4 string format.
        /// </summary>
        public string InternalProjectionParametersString
        {
            get => GetProjectionString("InternalProjectionParametersString") ?? internalParametersString;
            set
            {
                internalParametersString = value;
                SetProjectionString("InternalProjectionParametersString", value);
            }
        }

        /// <summary>
        /// Gets or sets the external (target) projection parameters in proj4 string format.
        /// </summary>
        public string ExternalProjectionParametersString
        {
            get => GetProjectionString("ExternalProjectionParametersString") ?? externalParametersString;
            set
            {
                externalParametersString = value;
                SetProjectionString("ExternalProjectionParametersString", value);
            }
        }

        /// <summary>
        /// Returns a proj4 parameters string for an EPSG code.
        ///
        /// If ThinkGeo.Core exposes a matching helper method, this delegates to it. Otherwise a
        /// small built-in mapping is used for common codes.
        /// </summary>
        public static string GetEpsgParametersString(int epsgCode)
        {
            // Try ThinkGeo.Core's helper if it exists in this version.
            try
            {
                var mi = typeof(ProjectionConverter).GetMethod(
                    "GetEpsgParametersString",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(int) },
                    null);

                if (mi != null)
                {
                    var result = mi.Invoke(null, new object[] { epsgCode }) as string;
                    if (!string.IsNullOrWhiteSpace(result)) return result;
                }
            }
            catch
            {
                // Ignore and fall back.
            }

            // Minimal fallback mapping (enough for the editor defaults).
            switch (epsgCode)
            {
                case 4326:
                    return "+proj=longlat +datum=WGS84 +no_defs";

                // Web Mercator
                case 3857:
                case 900913:
                    return "+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext +no_defs";

                default:
                    return string.Empty;
            }
        }

        private string GetProjectionString(string preferredName)
        {
            // Try common property names across ThinkGeo versions.
            var candidates = preferredName == "InternalProjectionParametersString"
                ? new[] { "InternalProjectionParametersString", "InternalProjectionString", "InternalParametersString" }
                : new[] { "ExternalProjectionParametersString", "ExternalProjectionString", "ExternalParametersString" };

            foreach (var name in candidates)
            {
                try
                {
                    var pi = typeof(ProjectionConverter).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (pi != null && pi.PropertyType == typeof(string) && pi.CanRead)
                    {
                        return pi.GetValue(this, null) as string;
                    }
                }
                catch
                {
                    // ignore
                }
            }

            return null;
        }

        private void SetProjectionString(string preferredName, string value)
        {
            var candidates = preferredName == "InternalProjectionParametersString"
                ? new[] { "InternalProjectionParametersString", "InternalProjectionString", "InternalParametersString" }
                : new[] { "ExternalProjectionParametersString", "ExternalProjectionString", "ExternalParametersString" };

            foreach (var name in candidates)
            {
                try
                {
                    var pi = typeof(ProjectionConverter).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (pi != null && pi.PropertyType == typeof(string) && pi.CanWrite)
                    {
                        pi.SetValue(this, value, null);
                        return;
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}
