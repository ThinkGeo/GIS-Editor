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
using System.Globalization;
using System.Windows.Data;
using System.Reflection;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    [Obfuscation]
    internal class ProjectionTypeToStringConverter : IValueConverter
    {
        private static string noSelect = GisEditor.LanguageManager.GetStringResource("ProjectionConfigurationCommonProjectionsPleaseSelect");
        private const string geographic = "Geographic (Longitude/Latitude)";
        private const string googleMaps = "Google Maps / Bing Maps / OpenStreet Maps";
        private const string statePlane = "State Plane";
        private const string utm = "UTM";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProjectionType)
            {
                return Convert((ProjectionType)value);
            }
            else return ConvertBack(value.ToString());
        }

        public static string Convert(ProjectionType projectionType)
        {
            switch (projectionType)
            {
                case ProjectionType.Geographic:
                    return geographic;
                case ProjectionType.GoogleMaps:
                    return googleMaps;
                case ProjectionType.StatePlane:
                    return statePlane;
                case ProjectionType.UTM:
                    return utm;
                case ProjectionType.None:
                default:
                    return noSelect;
            }
        }

        public static ProjectionType ConvertBack(string projectionTypeName)
        {
            switch (projectionTypeName)
            {
                case geographic:
                    return ProjectionType.Geographic;
                case googleMaps:
                    return ProjectionType.GoogleMaps;
                case statePlane:
                    return ProjectionType.StatePlane;
                case utm:
                    return ProjectionType.UTM;
                default:
                    return ProjectionType.None;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }
}