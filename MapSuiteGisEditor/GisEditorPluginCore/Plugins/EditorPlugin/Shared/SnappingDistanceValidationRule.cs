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
using System.Windows.Controls;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class SnappingDistanceValidationRule : ValidationRule
    {
        private static Dictionary<SnappingDistanceUnit, double> distanceMaxValues;

        static SnappingDistanceValidationRule()
        {
            distanceMaxValues = new Dictionary<SnappingDistanceUnit, double>
            {
                {SnappingDistanceUnit.Feet,300000},
                {SnappingDistanceUnit.Kilometer,100},
                {SnappingDistanceUnit.Meter,100000},
                {SnappingDistanceUnit.Mile,70},
                {SnappingDistanceUnit.UsSurveyFeet,300000},
                {SnappingDistanceUnit.Yard,100000},
            };
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            ValidationResult result = null;

            double inputValue = 0;
            bool isDouble = double.TryParse(value.ToString(), out inputValue);

            if (isDouble && GisEditor.ActiveMap != null)
            {
                GisEditorEditInteractiveOverlay editOverlay = GisEditor.ActiveMap.FeatureLayerEditOverlay;
                if (editOverlay != null)
                {
                    SnappingDistanceUnit unit = editOverlay.SnappingDistanceUnit;
                    if (distanceMaxValues.ContainsKey(unit))
                    {
                        double maxValue = distanceMaxValues[unit];
                        if (inputValue > maxValue)
                        {
                            result = new ValidationResult(false, null);
                        }
                        else
                        {
                            result = new ValidationResult(true, null);
                        }
                    }
                    else result = new ValidationResult(true, null);
                }
            }
            else
            {
                result = new ValidationResult(false, null);
            }

            return result;
        }
    }
}