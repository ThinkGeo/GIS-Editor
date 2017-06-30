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
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class CSVMappingTypeToStringConverter : ValueConverter
    {
        private const string longitudeAndLatitudeMappingType = "Longitude and Latitude columns";
        private const string wellKnownTextMappingType = "Well-Known Text (WKT) columns";

        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DelimitedSpatialColumnsType)
            {
                return Convert((DelimitedSpatialColumnsType)value);
            }
            else
            {
                return ConvertBack(value.ToString());
            }
        }

        private static string Convert(DelimitedSpatialColumnsType mappingType)
        {
            if (mappingType == DelimitedSpatialColumnsType.XAndY)
                return longitudeAndLatitudeMappingType;
            else if (mappingType == DelimitedSpatialColumnsType.WellKnownText)
                return wellKnownTextMappingType;
            else
                return mappingType.ToString();
        }

        private static DelimitedSpatialColumnsType ConvertBack(string mappingTypeName)
        {
            if (mappingTypeName == longitudeAndLatitudeMappingType)
                return DelimitedSpatialColumnsType.XAndY;
            else if (mappingTypeName == wellKnownTextMappingType)
                return DelimitedSpatialColumnsType.WellKnownText;
            else
            {
                DelimitedSpatialColumnsType mappingType = DelimitedSpatialColumnsType.XAndY;
                Enum.TryParse<DelimitedSpatialColumnsType>(mappingTypeName, out mappingType);
                return mappingType;
            }
        }
    }
}