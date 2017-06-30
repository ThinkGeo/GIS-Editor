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


using System.Runtime.Serialization;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [DataContract]
    public class OpenStreetGeocodeMatch
    {
        [DataMember(Name = "place_id", EmitDefaultValue = false)]
        public string PlaceId { get; set; }

        [DataMember(Name = "licence", EmitDefaultValue = false)]
        public string License { get; set; }

        [DataMember(Name = "osm_type", EmitDefaultValue = false)]
        public string OsmType { get; set; }

        [DataMember(Name = "osm_id", EmitDefaultValue = false)]
        public string OsmId { get; set; }

        [DataMember(Name = "boundingbox", EmitDefaultValue = false)]
        public double[] BoundingBox { get; set; }

        [DataMember(Name = "lat", EmitDefaultValue = false)]
        public double Latitude { get; set; }

        [DataMember(Name = "lon", EmitDefaultValue = false)]
        public double Longitude { get; set; }

        [DataMember(Name = "display_name", EmitDefaultValue = false)]
        public string DisplayName { get; set; }

        [DataMember(Name = "class", EmitDefaultValue = false)]
        public string Class { get; set; }

        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "importance", EmitDefaultValue = false)]
        public double Importance { get; set; }

        [DataMember(Name = "address", EmitDefaultValue = false)]
        public OpenStreetGeocodeAddress Address { get; set; }
    }
}