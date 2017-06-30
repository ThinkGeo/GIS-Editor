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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class FilterHelper
    {
        private static Dictionary<FilterConditionType, Tuple<string, string>> normalFilterConditionTemplates;
        private static Dictionary<FilterConditionType, Tuple<string, string>> calculatedFilterConditionTemplates;
        private static Dictionary<FilterConditionType, string> filterConditionNames;

        static FilterHelper()
        {
        }

        public static Dictionary<FilterConditionType, string> FilterConditionNames
        {
            get
            {
                if (filterConditionNames == null)
                {
                    filterConditionNames = new Dictionary<FilterConditionType, string>();
                    filterConditionNames.Add(FilterConditionType.Equal, "equals to");
                    filterConditionNames.Add(FilterConditionType.Contains, "contains");
                    filterConditionNames.Add(FilterConditionType.StartsWith, "starts with");
                    filterConditionNames.Add(FilterConditionType.EndsWith, "ends with");
                    filterConditionNames.Add(FilterConditionType.DoesNotEqual, "does not equal to");
                    filterConditionNames.Add(FilterConditionType.DoesNotContain, "does not contain");
                    filterConditionNames.Add(FilterConditionType.GreaterThan, "greater than");
                    filterConditionNames.Add(FilterConditionType.GreaterThanOrEqualTo, "greater than or equal to");
                    filterConditionNames.Add(FilterConditionType.LessThan, "less than");
                    filterConditionNames.Add(FilterConditionType.LessThanOrEqualTo, "less than or equal to");
                    filterConditionNames.Add(FilterConditionType.DateRange, "from date");
                    filterConditionNames.Add(FilterConditionType.NumericRange, "from");
                    filterConditionNames.Add(FilterConditionType.Custom, "regular matches to");
                    filterConditionNames.Add(FilterConditionType.IsEmpty, "is empty");
                    filterConditionNames.Add(FilterConditionType.IsNotEmpty, "is not empty");
                    filterConditionNames.Add(FilterConditionType.ValidFeature, "valid features");
                }
                return FilterHelper.filterConditionNames;
            }
        }

        public static Dictionary<FilterConditionType, Tuple<string, string>> GetFilterConditionTemplates(FilterMode filterMode)
        {
            if (filterMode == FilterMode.Attributes)
            {
                if (normalFilterConditionTemplates == null)
                {
                    normalFilterConditionTemplates = new Dictionary<FilterConditionType, Tuple<string, string>>();

                    normalFilterConditionTemplates.Add(FilterConditionType.DoesNotContain, new Tuple<string, string>("^(?!.*?", ").*$"));
                    normalFilterConditionTemplates.Add(FilterConditionType.DoesNotEqual, new Tuple<string, string>("^(?!", ").*?$"));
                    normalFilterConditionTemplates.Add(FilterConditionType.Contains, new Tuple<string, string>(".*", ".*"));
                    normalFilterConditionTemplates.Add(FilterConditionType.Equal, new Tuple<string, string>("^", "$"));
                    normalFilterConditionTemplates.Add(FilterConditionType.StartsWith, new Tuple<string, string>("^", string.Empty));
                    normalFilterConditionTemplates.Add(FilterConditionType.EndsWith, new Tuple<string, string>(string.Empty, "$"));
                    normalFilterConditionTemplates.Add(FilterConditionType.GreaterThanOrEqualTo, new Tuple<string, string>(">=", string.Empty));
                    normalFilterConditionTemplates.Add(FilterConditionType.GreaterThan, new Tuple<string, string>(">", string.Empty));
                    normalFilterConditionTemplates.Add(FilterConditionType.LessThanOrEqualTo, new Tuple<string, string>("<=", string.Empty));
                    normalFilterConditionTemplates.Add(FilterConditionType.LessThan, new Tuple<string, string>("<", string.Empty));
                    normalFilterConditionTemplates.Add(FilterConditionType.DateRange, new Tuple<string, string>("Date(", ")"));
                    normalFilterConditionTemplates.Add(FilterConditionType.NumericRange, new Tuple<string, string>("Number", string.Empty));
                    normalFilterConditionTemplates.Add(FilterConditionType.IsEmpty, new Tuple<string, string>("^", "$"));
                    normalFilterConditionTemplates.Add(FilterConditionType.IsNotEmpty, new Tuple<string, string>("^(?!", ").*?$"));
                    normalFilterConditionTemplates.Add(FilterConditionType.ValidFeature, new Tuple<string, string>("ValidFeature(", ")"));
                    normalFilterConditionTemplates.Add(FilterConditionType.Custom, new Tuple<string, string>(string.Empty, string.Empty));
                }

                return normalFilterConditionTemplates;
            }
            else
            {
                if (calculatedFilterConditionTemplates == null)
                {
                    calculatedFilterConditionTemplates = new Dictionary<FilterConditionType, Tuple<string, string>>();
                    foreach (var item in GetFilterConditionTemplates(FilterMode.Attributes))
                    {
                        calculatedFilterConditionTemplates.Add(item.Key, item.Value);
                    }
                    calculatedFilterConditionTemplates.Remove(FilterConditionType.Contains);
                    calculatedFilterConditionTemplates.Remove(FilterConditionType.DateRange);
                    calculatedFilterConditionTemplates.Remove(FilterConditionType.DoesNotContain);
                    calculatedFilterConditionTemplates.Remove(FilterConditionType.DoesNotEqual);
                    calculatedFilterConditionTemplates.Remove(FilterConditionType.DynamicLanguage);
                    calculatedFilterConditionTemplates.Remove(FilterConditionType.EndsWith);
                    calculatedFilterConditionTemplates.Remove(FilterConditionType.IsEmpty);
                    calculatedFilterConditionTemplates.Remove(FilterConditionType.IsNotEmpty);
                    calculatedFilterConditionTemplates.Remove(FilterConditionType.StartsWith);
                    calculatedFilterConditionTemplates.Remove(FilterConditionType.ValidFeature);
                }

                return calculatedFilterConditionTemplates;

            }
        }
    }
}