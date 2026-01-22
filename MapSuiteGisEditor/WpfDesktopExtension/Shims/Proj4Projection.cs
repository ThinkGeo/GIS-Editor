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
        private const string WktWgs84 =
            "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]]";
        private const string WktWebMercator =
            "PROJCS[\"WGS 84 / Pseudo-Mercator\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"Mercator_1SP\"],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH]]";

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

        public static string GetWgs84ParametersString()
        {
            return GetEpsgParametersString(4326);
        }

        public static string GetDecimalDegreesParametersString()
        {
            return GetWgs84ParametersString();
        }

        public static string GetGoogleMapParametersString()
        {
            return GetEpsgParametersString(3857);
        }

        public static string ConvertProj4ToPrj(string proj4Parameters)
        {
            if (string.IsNullOrWhiteSpace(proj4Parameters)) return string.Empty;

            var wkt = TryConvertProj4ToPrjWithOsr(proj4Parameters);
            if (!string.IsNullOrWhiteSpace(wkt)) return wkt;

            if (LooksLikeWkt(proj4Parameters)) return proj4Parameters;

            var normalized = proj4Parameters.ToLowerInvariant();
            if (normalized.Contains("+proj=longlat") || normalized.Contains("+proj=latlong"))
            {
                return WktWgs84;
            }

            if (normalized.Contains("+proj=merc"))
            {
                return WktWebMercator;
            }

            return string.Empty;
        }

        public static string ConvertPrjToProj4(string prjWkt)
        {
            if (string.IsNullOrWhiteSpace(prjWkt)) return string.Empty;

            var proj4 = TryConvertPrjToProj4WithOsr(prjWkt);
            if (!string.IsNullOrWhiteSpace(proj4)) return proj4;

            if (LooksLikeProj4(prjWkt)) return prjWkt;

            var normalized = prjWkt.ToLowerInvariant();
            if (normalized.Contains("wgs_1984") || normalized.Contains("wgs 84"))
            {
                if (normalized.Contains("pseudo-mercator") || normalized.Contains("web mercator") || normalized.Contains("mercator"))
                {
                    return GetGoogleMapParametersString();
                }

                return GetWgs84ParametersString();
            }

            return string.Empty;
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

        private static bool LooksLikeWkt(string text)
        {
            return text.IndexOf("GEOGCS", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("PROJCS", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("LOCAL_CS", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool LooksLikeProj4(string text)
        {
            return text.IndexOf("+proj", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("+datum", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("+a=", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string TryConvertProj4ToPrjWithOsr(string proj4Parameters)
        {
            try
            {
                var srType = Type.GetType("OSGeo.OSR.SpatialReference, osr_csharp");
                if (srType == null) return null;

                var sr = Activator.CreateInstance(srType);
                var import = srType.GetMethod("ImportFromProj4", new[] { typeof(string) });
                var export = srType.GetMethod("ExportToWkt", new[] { typeof(string).MakeByRefType(), typeof(string[]) });
                if (import == null || export == null) return null;

                var importResult = (int)import.Invoke(sr, new object[] { proj4Parameters });
                if (importResult != 0) return null;

                object[] exportArgs = { null, null };
                var exportResult = (int)export.Invoke(sr, exportArgs);
                if (exportResult != 0) return null;

                return exportArgs[0] as string;
            }
            catch
            {
                return null;
            }
        }

        private static string TryConvertPrjToProj4WithOsr(string prjWkt)
        {
            try
            {
                var srType = Type.GetType("OSGeo.OSR.SpatialReference, osr_csharp");
                if (srType == null) return null;

                var sr = Activator.CreateInstance(srType);
                var import = srType.GetMethod("ImportFromWkt", new[] { typeof(string).MakeByRefType() });
                var export = srType.GetMethod("ExportToProj4", new[] { typeof(string).MakeByRefType() });
                if (import == null || export == null) return null;

                object[] importArgs = { prjWkt };
                var importResult = (int)import.Invoke(sr, importArgs);
                if (importResult != 0) return null;

                object[] exportArgs = { null };
                var exportResult = (int)export.Invoke(sr, exportArgs);
                if (exportResult != 0) return null;

                return exportArgs[0] as string;
            }
            catch
            {
                return null;
            }
        }
    }
}
