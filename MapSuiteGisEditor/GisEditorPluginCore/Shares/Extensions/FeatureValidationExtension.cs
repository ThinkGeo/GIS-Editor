using System;
using ThinkGeo.Core;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class FeatureValidationExtension
    {
        public static bool CanMakeValid(this Feature feature)
        {
            return feature != null;
        }

        public static bool IsValid(this Feature feature)
        {
            return feature != null && feature.IsGeometryValid();
        }

        public static Feature MakeValid(this Feature feature)
        {
            if (feature == null) return null;
            if (feature.IsGeometryValid()) return feature;

            try
            {
                return SqlTypesGeometryHelper.MakeValid(feature) ?? feature;
            }
            catch
            {
                return feature;
            }
        }
    }
}
