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
using System.Collections.ObjectModel;
using System.Reflection;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class CalculatedDbfColumn : DbfColumn
    {
        private const string specificGoogleProjection1 = "+proj=merc  +lon_0=0  +lat_ts=0  +x_0=0  +y_0=0  +a=6378137  +b=6378137  +units=m  +no_defs ";
        private const string specificGoogleProjection2 = "+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +no_defs";

        public static Dictionary<string, Collection<CalculatedDbfColumn>> CalculatedColumns = new Dictionary<string, Collection<CalculatedDbfColumn>>();

        [Obfuscation(Exclude = true)]
        private CalculatedDbfColumnType calculationType;

        [Obfuscation(Exclude = true)]
        private AreaUnit areaUnit;

        [Obfuscation(Exclude = true)]
        private DistanceUnit lengthUnit;

        public CalculatedDbfColumn()
            : this("", DbfColumnType.Character, 10, 0, CalculatedDbfColumnType.Area, AreaUnit.Acres)
        {
        }

        public CalculatedDbfColumn(string columnName,
            DbfColumnType columnType,
            int length,
            int decimalLength,
            CalculatedDbfColumnType calculationType,
            AreaUnit measurementUnit)
            : base(columnName, columnType, length, decimalLength)
        {
            this.calculationType = calculationType;
            this.areaUnit = measurementUnit;
        }

        public CalculatedDbfColumn(string columnName,
           DbfColumnType columnType,
           int length,
           int decimalLength,
           CalculatedDbfColumnType calculationType,
           DistanceUnit lengthUnit)
            : base(columnName, columnType, length, decimalLength)
        {
            this.calculationType = calculationType;
            this.lengthUnit = lengthUnit;
        }

        public CalculatedDbfColumnType CalculationType
        {
            get { return calculationType; }
            set { calculationType = value; }
        }

        public AreaUnit AreaUnit
        {
            get { return areaUnit; }
            set { areaUnit = value; }
        }

        public DistanceUnit LengthUnit
        {
            get { return lengthUnit; }
            set { lengthUnit = value; }
        }

        public static void UpdateCalculatedRecords(IEnumerable<CalculatedDbfColumn> updateColumns, IEnumerable<Feature> features, string featuresProj4ProjectionParameters)
        {
            foreach (var calculateColumn in updateColumns)
            {
                foreach (var feature in features)
                {
                    Feature featureInDecimalDegree = ConvertToWgs84Projection(feature, featuresProj4ProjectionParameters);
                    var calculatedValue = string.Empty;

                    switch (calculateColumn.CalculationType)
                    {
                        case CalculatedDbfColumnType.Perimeter:
                            var perimeterShape = featureInDecimalDegree.GetShape() as AreaBaseShape;
                            if (perimeterShape != null)
                            {
                                calculatedValue = perimeterShape.GetPerimeter(GeographyUnit.DecimalDegree, calculateColumn.LengthUnit).ToString();
                            }
                            break;

                        case CalculatedDbfColumnType.Area:
                            var areaShape = featureInDecimalDegree.GetShape() as AreaBaseShape;
                            if (areaShape != null)
                            {
                                calculatedValue = areaShape.GetArea(GeographyUnit.DecimalDegree, calculateColumn.AreaUnit).ToString();
                            }
                            break;

                        case CalculatedDbfColumnType.Length:
                            var lineShape = featureInDecimalDegree.GetShape() as LineBaseShape;
                            if (lineShape != null)
                            {
                                calculatedValue = lineShape.GetLength(GeographyUnit.DecimalDegree, calculateColumn.LengthUnit).ToString();
                            }
                            break;

                        default:
                            break;
                    }

                    if (calculatedValue.Length > calculateColumn.Length)
                    {
                        calculatedValue = calculatedValue.Substring(0, calculateColumn.Length);
                    }

                    feature.ColumnValues[calculateColumn.ColumnName] = calculatedValue;
                }
            }
        }

        private static Feature ConvertToWgs84Projection(Feature feature, string displayProjectionParameters)
        {
            Feature featureInDecimalDegree = feature;
            Proj4Projection projection = new Proj4Projection();
            projection.InternalProjectionParametersString = Proj4Projection.GetEpsgParametersString(4326);
            projection.ExternalProjectionParametersString = displayProjectionParameters;
            SyncProjectionParametersString(projection);
            if (CanProject(projection))
            {
                projection.Open();
                featureInDecimalDegree = projection.ConvertToInternalProjection(featureInDecimalDegree);
                projection.Close();
            }
            return featureInDecimalDegree;
        }

        private static void SyncProjectionParametersString(Proj4Projection projection)
        {
            string internalParameter = projection.InternalProjectionParametersString;
            string externalParameter = projection.ExternalProjectionParametersString;
            if (!CanProject(internalParameter, externalParameter) && !String.IsNullOrEmpty(internalParameter) && !String.IsNullOrEmpty(externalParameter))
            {
                projection.InternalProjectionParametersString = projection.ExternalProjectionParametersString;
            }
        }

        private static bool CanProject(Proj4Projection proj4Projection)
        {
            return CanProject(proj4Projection.InternalProjectionParametersString, proj4Projection.ExternalProjectionParametersString);
        }

        private static bool CanProject(string internalProj4ProjectionParameters, string externalProj4ProjectionParameters)
        {
            if (String.IsNullOrEmpty(internalProj4ProjectionParameters) || String.IsNullOrEmpty(externalProj4ProjectionParameters))
            {
                return false;
            }
            else if (CheckSpecificGoogleProjectionIsEqual(internalProj4ProjectionParameters, externalProj4ProjectionParameters))
            {
                return false;
            }
            else
            {
                bool baseCanProject = !internalProj4ProjectionParameters.Replace(" ", "").Equals(externalProj4ProjectionParameters.Replace(" ", ""), StringComparison.Ordinal);

                if (baseCanProject)
                {
                    Dictionary<string, string> internalParameters = ParseParams(internalProj4ProjectionParameters);
                    Dictionary<string, string> externalParameters = ParseParams(externalProj4ProjectionParameters);

                    foreach (var internalKeyValue in internalParameters)
                    {
                        if (!externalParameters.ContainsKey(internalKeyValue.Key))
                        {
                            return true;
                        }
                    }

                    foreach (var internalKeyValue in internalParameters)
                    {
                        if (externalParameters[internalKeyValue.Key] != internalKeyValue.Value)
                        {
                            double internalValue = double.NaN;
                            double externalValue = double.NaN;
                            if (Double.TryParse(externalParameters[internalKeyValue.Key], out externalValue) && Double.TryParse(internalParameters[internalKeyValue.Key], out internalValue))
                            {
                                if (Math.Abs(externalValue - internalValue) >= 1) return true;
                            }
                            else
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
        }

        private static bool CheckSpecificGoogleProjectionIsEqual(string internalProj4, string externalProj4)
        {
            return (internalProj4.Equals(specificGoogleProjection1, StringComparison.OrdinalIgnoreCase) || internalProj4.Equals(specificGoogleProjection2, StringComparison.OrdinalIgnoreCase))
                && (externalProj4.Equals(specificGoogleProjection1, StringComparison.OrdinalIgnoreCase) || externalProj4.Equals(specificGoogleProjection2, StringComparison.OrdinalIgnoreCase));
        }

        private static Dictionary<string, string> ParseParams(string parameterString)
        {
            string upperCaseParameters = parameterString.ToUpperInvariant().Replace(" ", "");
            string[] parameters = upperCaseParameters.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> parameterDict = new Dictionary<string, string>();
            foreach (string parameter in parameters)
            {
                if (parameter.Contains("="))
                {
                    string[] keyValue = parameter.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    parameterDict.Add(keyValue[0], keyValue[1]);
                }
            }

            return parameterDict;
        }
    }
}