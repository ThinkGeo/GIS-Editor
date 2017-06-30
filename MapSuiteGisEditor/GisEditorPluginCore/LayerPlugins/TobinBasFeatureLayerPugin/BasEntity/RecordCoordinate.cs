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
using System.IO;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    internal class RecordCoordinate
    {
        // Col 1 Record Type.
        // Always "3", indicating that this record is a Coordinate Record.
        // Col 2-16
        // A 15-character latitude/longitude coordinate field which specifies the start point for a line segment.
        // Col 17-36
        // A 20-character X/Y coordinate field which specifies the start point for a line segment.
        // Col 37-51
        // A 15-character latitude/longitude coordinate field which specifies the ending point for a line segment.
        // Col 52-71
        // A 20-character X/Y coordinate field which specifies the ending point for a line segment.
        // Col 72-73 Line Code.
        // The following table defines the values for the line codes.
        // Col 74-79 Line Segment Sequence Number.
        // The sequence number is used to identify those co-incident arcs, with the same state and county, in TDRBM II format. This allows the elimination of line segment over plotting.
        // Col 80 End of Polygon flag.
        private string startPointLonLat;

        private string endPointLonLat;

        private string endOfPolygonFlag;

        public bool EndOfPolygonFlag
        {
            get
            {
                return endOfPolygonFlag == "9";
            }
        }

        public string StartPointLonLat
        {
            get { return startPointLonLat; }
            set { startPointLonLat = value; }
        }

        public string EndPointLonLat
        {
            get { return endPointLonLat; }
            set { endPointLonLat = value; }
        }

        public void Read(BinaryReader reader)
        {
            startPointLonLat = new string(reader.ReadChars(15));

            endPointLonLat = new string(reader.ReadChars(15));
            endOfPolygonFlag = new string(reader.ReadChars(1));
        }
    }
}