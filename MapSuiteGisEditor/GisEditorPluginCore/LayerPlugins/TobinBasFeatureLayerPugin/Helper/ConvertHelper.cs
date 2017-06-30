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
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal class ConvertHelper
    {
        //Lat-Lon coordinate pairs representing locations on the surface of the earth will be 15-character numeric
        //fields of the form "YYyyyyyXXXxxxxx", where XXX.xxxxx is the longitude west, and YY.yyyyy the latitude
        //north. So, for example, the point 88.44411 west longitude and 33.55500 north latitude would be
        //represented "335550008844411".
        public static Vertex LatLonToVertex(string latlonString)
        {
            if (latlonString.Length != 15)
            {
                throw new ArgumentException();
            }
            double y = double.Parse(latlonString.Substring(0, 2)) + double.Parse(latlonString.Substring(2, 5)) * 0.00001;
            double x = double.Parse(latlonString.Substring(7, 3)) + double.Parse(latlonString.Substring(10, 5)) * 0.00001;

            return new Vertex(-x, y);
        }

        public static string LatLonToString(Vertex vertex)
        {
            throw new NotImplementedException();
        }

        //X-Y coordinate pairs representing locations on the surface of the earth
        //will be either 18-character numeric fields of the form "XXXXXXXXXYYYYYYYYY",
        //where XXXXXXXXX is the northing and YYYYYYYYY the easting for text positioning
        public static Vertex XY18ToVertex(string XY18String)
        {
            throw new NotImplementedException();
        }

        public static string XY18ToString(Vertex vertex)
        {
            throw new NotImplementedException();
        }

        //X-Y coordinate pairs representing locations on the surface of the earth
        //will be either 20-character numeric fields of the form "XXXXXXXXXXYYYYYYYYYY",
        //where XXXXXXXXXX is the northing and YYYYYYYYYY the easting for line segments
        public static Vertex XY20ToVertex(string XY20String)
        {
            throw new NotImplementedException();
        }

        public static string XY20ToString(Vertex vertex)
        {
            throw new NotImplementedException();
        }

        //Dates will be 6-character numeric fields of the form "YYMMDD", where YY is the year, MM is the month
        //("01" is January, "12" is December, etc.), and DD is the day of the month from "01" to "31".
        public static DateTime YYMMDDToVertex(string YYMMDDString)
        {
            throw new NotImplementedException();
        }

        public static string YYMMDDToString(DateTime datetime)
        {
            throw new NotImplementedException();
        }

        // A 5-character rotation angle in the form XXX.XX. The angle is specified in 100ths of a degree.
        public static float TextAngleToAngle(string textAngle)
        {
            if (textAngle.Length != 5)
            {
                throw new ArgumentException();
            }
            return float.Parse(textAngle) * 0.01f;
        }

        //public static BasEnum.DataType GetType(string data)
        //{
        //    throw new NotImplementedException();
        //}
    }
}