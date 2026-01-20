using System.Collections.Generic;

// GeoDataTable existed in MapSuite v10 and was used by some helper APIs to return a lightweight
// tabular representation of feature attributes.
//
// ThinkGeo.Core v14 no longer exposes this type, but parts of the GIS Editor extension still
// expect it. This minimal version provides the handful of members used by MapEngine.cs.
namespace ThinkGeo.Core
{
    public class GeoDataTable
    {
        public GeoDataTable()
        {
            Columns = new List<string>();
            Rows = new List<Dictionary<string, object>>();
        }

        public IList<string> Columns { get; }

        public IList<Dictionary<string, object>> Rows { get; }
    }
}
