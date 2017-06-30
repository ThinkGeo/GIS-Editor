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


using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class Proj4ToNameConverter : ValueConverter
    {
        private static readonly Dictionary<string, string> dictionary;

        static Proj4ToNameConverter()
        {
            if (dictionary == null)
            {
                dictionary = new Dictionary<string, string>
                {
                    {"longlat", "Geographic (Latitude/Longitude)"},
                    {"tmerc", "Transverse Mercator"},
                    {"utm", "UTM" },
                    {"sterea", "Oblique Stereographic" },
                    {"somerc", "Swiss Oblique Mercator" },
                    {"omerc", "Hotine Oblique Mercator" },
                    {"lcc", "Lambert Conic Conformal" },
                    {"krovak", "Krovak Oblique Conic Conformal"},
                    {"cass", "Cassini-Soldner"} ,
                    {"laea", "Lambert Azimuthal Equal Area"} ,
                    {"merc", "Mercator" } ,
                    {"aea", "Albers Equal-Area Conic" } ,
                    {"stere", "Stereographic" } ,
                    {"nzmg", "New Zealand Map Grid" } ,
                    {"poly", "Polyconic" } ,
                    {"eqc", "Equidistant Conic (EQC)" } ,
                    {"mill", "Miller Cylindrical" } ,
                    {"sinu", "Sinusoidal" } ,
                    {"moll", "Mollweide" } ,
                    {"eck6", "Eckert VI" } ,
                    {"eck4", "Eckert IV" } ,
                    {"gall", "Gall Stereographic" } ,
                    {"eqdc", "Equidistant Conic (EQDC)" } ,
                    {"vandg", "VanDerGrinten" } ,
                    {"robin", "Robinson" } ,
                    {"aeqd", "Azimuthal Equidistant" } ,
                    {"tmert+lcc", "State Plane"}
                };
            }
        }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string proj4 = value as string;
            if (!String.IsNullOrEmpty(proj4)) return GetProjName(proj4);
            else return Binding.DoNothing;
        }

        private string GetProjName(string proj4String)
        {
            string projectionFullName = "Unknown";
            if (!string.IsNullOrEmpty(proj4String))
            {
                string projectionShortName = proj4String.Split(' ')[0].Replace("+proj=", string.Empty);

                if (!dictionary.TryGetValue(projectionShortName, out projectionFullName))
                {
                    projectionFullName = "Unknown";
                }
            }

            return projectionFullName;
        }
    }
}