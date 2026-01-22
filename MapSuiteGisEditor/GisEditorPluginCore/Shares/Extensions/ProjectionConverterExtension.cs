using System.Reflection;
using ThinkGeo.Core;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class ProjectionConverterExtension
    {
        public static ProjectionConverter CloneDeep(this ProjectionConverter converter)
        {
            if (converter == null) return null;

            if (converter is Proj4Projection proj4)
            {
                return new Proj4Projection(proj4.InternalProjectionParametersString, proj4.ExternalProjectionParametersString);
            }

            var cloneMethod = converter.GetType().GetMethod("CloneDeep", BindingFlags.Instance | BindingFlags.Public);
            if (cloneMethod != null && typeof(ProjectionConverter).IsAssignableFrom(cloneMethod.ReturnType))
            {
                try
                {
                    return (ProjectionConverter)cloneMethod.Invoke(converter, null);
                }
                catch
                {
                    // Fall through to return original.
                }
            }

            return converter;
        }
    }
}
