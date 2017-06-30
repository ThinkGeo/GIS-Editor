/*
* Licensed to the Apache Software Foundation (ASF) under one
* or more contributor license agreements.  See the NOTICE file
* distributed with this work for additional information
* regarding copyright ownership.  The ASF licenses this file
* to you under the Apache License, Version 2.0 (the
* "License"); you may not use this file except in compliance
* with the License.  You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/


using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThinkGeo.MapSuite.GeocodeServerSdk;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class SearchPlaceHelper
    {
        public static Collection<GeocodeMatch> GetGeocodeMatches(string searchAddress)
        {
            var matches = OpenStreetGeocodeHelper.GeocodeAsync(searchAddress).Result;
            return GetGeocodeMatches(matches);
        }

        public static Collection<GeocodeMatch> GetGeocodeMatches(double longitude, double latitude)
        {
            var match = OpenStreetGeocodeHelper.ReverseGeocodeAsync(longitude, latitude).Result;
            var result = new Collection<GeocodeMatch>();
            if (match != null) result.Add(GetGeocodeMatch(match));

            return result;
        }

        private static Collection<GeocodeMatch> GetGeocodeMatches(IEnumerable<OpenStreetGeocodeMatch> matches)
        {
            Collection<GeocodeMatch> resultMatches = new Collection<GeocodeMatch>();
            foreach (var item in matches)
            {
                GeocodeMatch match = GetGeocodeMatch(item);
                resultMatches.Add(match);
            }

            return resultMatches;
        }

        private static GeocodeMatch GetGeocodeMatch(OpenStreetGeocodeMatch item)
        {
            GeocodeMatch match = new GeocodeMatch();
            match.MatchResults["CentroidPoint"] = "POINT(" + string.Join(" ", new[] { item.Longitude, item.Latitude }) + ")";
            match.MatchResults["BoundingBox"] = new RectangleShape(item.BoundingBox[2], item.BoundingBox[1], item.BoundingBox[3], item.BoundingBox[0]).GetWellKnownText();
            match.MatchResults["City"] = item.Address?.City;
            match.MatchResults["State"] = item.Address?.State;
            match.MatchResults["Country"] = item.Address?.Country;
            return match;
        }
    }
}
