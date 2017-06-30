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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class FilterStyleMatchTypeToStringConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is FilterConditionViewModel)
            {
                FilterConditionViewModel matchCondition = (FilterConditionViewModel)value;
                FilterConditionType matchType = matchCondition.MatchType.Key;

                //string logical = "OR";
                //if (matchCondition.Logical)
                //{
                //    logical = "AND";
                //}

                if (matchType == FilterConditionType.DynamicLanguage)
                {
                    return matchCondition.MatchExpression;
                }
                else if (matchType == FilterConditionType.DateRange)
                {
                    string fromDateString = "\"" + matchCondition.ColumnName + "\"" + ">=" + "\"" + matchCondition.FromDate.ToString() + "\"";
                    string toDateString = "\"" + matchCondition.ColumnName + "\"" + "<=" + "\"" + matchCondition.ToDate.ToString() + "\"";

                    string contractor = " and ";

                    if (DateTime.Parse(matchCondition.FromDate).ToShortDateString().Equals(new DateTime(1900, 1, 1).ToShortDateString()))
                    {
                        fromDateString = string.Empty;
                        contractor = string.Empty;
                    }

                    if (DateTime.Parse(matchCondition.ToDate).ToShortDateString().Equals(DateTime.MaxValue.ToShortDateString()))
                    {
                        toDateString = string.Empty;
                        contractor = string.Empty;
                    }

                    return string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", fromDateString, contractor, toDateString);
                }
                else if (matchType == FilterConditionType.ValidFeature)
                {
                    return "Features are " + (matchCondition.ValidStatus ? "valid" : "invalid");
                }
                else
                {
                    string matches = string.Empty;

                    if (FilterHelper.FilterConditionNames.ContainsKey(matchType))
                    {
                        matches = FilterHelper.FilterConditionNames[matchType];
                    }
                    else
                    {
                        matches = "unknown match";
                    }

                    if (matchType == FilterConditionType.IsNotEmpty || matchType == FilterConditionType.IsEmpty)
                    {
                        return string.Format(CultureInfo.InvariantCulture, "\"{0}\" {1}"
                            , matchCondition.ColumnName
                            , matches);
                    }
                    else if (matchCondition.FilterMode == FilterMode.Attributes)
                    {
                        return string.Format(CultureInfo.InvariantCulture, "\"{0}\" {1} \"{2}\""
                            , matchCondition.ColumnName
                            , matches
                            , matchCondition.MatchExpression);
                    }
                    else
                    {
                        return string.Format(CultureInfo.InvariantCulture, "\"{0}\" {1} \"{2}\""
                            , "Caculated Area"
                            , matches
                            , matchCondition.MatchExpression);
                    }
                }
            }
            else return Binding.DoNothing;
        }
    }
}