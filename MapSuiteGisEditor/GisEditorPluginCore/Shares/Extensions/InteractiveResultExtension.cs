using System;
using System.Reflection;
using ThinkGeo.UI.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class InteractiveResultExtension
    {
        public static void SetDrawThisOverlay(this InteractiveResult result, InteractiveOverlayDrawType value)
        {
            if (result == null) return;

            var prop = result.GetType().GetProperty("DrawThisOverlay", BindingFlags.Instance | BindingFlags.Public);
            if (prop == null || !prop.CanWrite) return;

            object converted = value;
            if (prop.PropertyType.IsEnum && prop.PropertyType != typeof(InteractiveOverlayDrawType))
            {
                try
                {
                    converted = Enum.Parse(prop.PropertyType, value.ToString(), true);
                }
                catch
                {
                    return;
                }
            }
            else if (!prop.PropertyType.IsEnum && prop.PropertyType != typeof(InteractiveOverlayDrawType))
            {
                return;
            }

            prop.SetValue(result, converted, null);
        }
    }
}
