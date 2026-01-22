using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkGeo.Core;
using ThinkGeo.UI.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public static class WmsOverlayExtension
    {
        public static IReadOnlyCollection<Uri> GetServerUris(this WmsOverlay overlay)
        {
            if (overlay == null) return Array.Empty<Uri>();

            var uris = GetUrisFromProperty(overlay, "ServerUris", "ServerUri", "RequestUris", "RequestUri", "Uri", "Url");
            return uris.ToList();
        }

        public static IEnumerable<Uri> GetRequestUrisCompat(this WmsOverlay overlay, RectangleShape extent)
        {
            if (overlay == null) return Enumerable.Empty<Uri>();

            var method = overlay.GetType().GetMethod("GetRequestUris", BindingFlags.Instance | BindingFlags.Public);
            if (method != null)
            {
                try
                {
                    var result = method.GetParameters().Length == 0
                        ? method.Invoke(overlay, null)
                        : method.Invoke(overlay, new object[] { extent });

                    if (result is IEnumerable<Uri> uriList) return uriList;
                    if (result is Uri singleUri) return new[] { singleUri };
                }
                catch
                {
                    // Best-effort only.
                }
            }

            var singleMethod = overlay.GetType().GetMethod("GetRequestUri", BindingFlags.Instance | BindingFlags.Public);
            if (singleMethod != null)
            {
                try
                {
                    var result = singleMethod.GetParameters().Length == 0
                        ? singleMethod.Invoke(overlay, null)
                        : singleMethod.Invoke(overlay, new object[] { extent });

                    if (result is Uri singleUri) return new[] { singleUri };
                }
                catch
                {
                    // Best-effort only.
                }
            }

            return GetServerUris(overlay);
        }

        private static IEnumerable<Uri> GetUrisFromProperty(object target, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                if (property == null) continue;

                object value;
                try
                {
                    value = property.GetValue(target, null);
                }
                catch
                {
                    continue;
                }

                if (value is IEnumerable<Uri> uris) return uris;
                if (value is Uri uri) return new[] { uri };
                if (value is string uriString && Uri.TryCreate(uriString, UriKind.Absolute, out var parsed)) return new[] { parsed };
            }

            return Enumerable.Empty<Uri>();
        }
    }
}
