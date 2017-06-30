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

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    internal class ProjectionAbbreviations
    {
        private readonly static Dictionary<string, string> abbreviations;

        static ProjectionAbbreviations()
        {
            if (abbreviations == null)
            {
                abbreviations = new Dictionary<string, string>();
                abbreviations.Add("longlat", "Geographic (Latitude/Longitude)");
                abbreviations.Add("tmerc", "Transverse Mercator");
                abbreviations.Add("utm", "UTM");
                abbreviations.Add("sterea", "Oblique Stereographic");
                abbreviations.Add("somerc", "Swiss Oblique Mercator");
                abbreviations.Add("omerc", "Hotine Oblique Mercator");
                abbreviations.Add("lcc", "Lambert Conic Conformal");
                abbreviations.Add("krovak", "Krovak Oblique Conic Conformal");
                abbreviations.Add("cass", "Cassini-Soldner");
                abbreviations.Add("laea", "Lambert Azimuthal Equal Area");
                abbreviations.Add("merc", "Mercator");
                abbreviations.Add("aea", "Albers Equal-Area Conic");
                abbreviations.Add("stere", "Stereographic");
                abbreviations.Add("nzmg", "New Zealand Map Grid");
                abbreviations.Add("poly", "Polyconic");
                abbreviations.Add("eqc", "Equidistant Conic (EQC)");
                abbreviations.Add("mill", "Miller Cylindrical");
                abbreviations.Add("sinu", "Sinusoidal");
                abbreviations.Add("moll", "Mollweide");
                abbreviations.Add("eck6", "Eckert VI");
                abbreviations.Add("eck4", "Eckert IV");
                abbreviations.Add("gall", "Gall Stereographic");
                abbreviations.Add("eqdc", "Equidistant Conic (EQDC)");
                abbreviations.Add("vandg", "VanDerGrinten");
                abbreviations.Add("robin", "Robinson");
                abbreviations.Add("aeqd", "Azimuthal Equidistant");
                abbreviations.Add("tmert+lcc", "State Plane");
            }
        }

        public string this[string abbr]
        {
            get
            {
                if (abbreviations.ContainsKey(abbr))
                {
                    return abbreviations[abbr];
                }
                else return "Unknown";
            }
        }
    }
}