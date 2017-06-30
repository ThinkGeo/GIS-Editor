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
using System.Reflection;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    internal static class QueryOperatorHelper
    {
        private static Dictionary<QueryOperaterType, Tuple<string, string>> filterConditionTemplates;
        private static Dictionary<QueryOperaterType, string> operatorNames;

        public static Dictionary<QueryOperaterType, string> OperatorNames
        {
            get
            {
                if (operatorNames == null)
                {
                    operatorNames = new Dictionary<QueryOperaterType, string>();
                    operatorNames.Add(QueryOperaterType.Equal, "equals to");
                    operatorNames.Add(QueryOperaterType.Contains, "contains");
                    operatorNames.Add(QueryOperaterType.StartsWith, "starts with");
                    operatorNames.Add(QueryOperaterType.EndsWith, "ends with");
                    operatorNames.Add(QueryOperaterType.DoesNotEqual, "does not equal to");
                    operatorNames.Add(QueryOperaterType.DoesNotContain, "does not contain");
                    operatorNames.Add(QueryOperaterType.GreaterThan, "greater than");
                    operatorNames.Add(QueryOperaterType.GreaterThanOrEqualTo, "greater than or equal to");
                    operatorNames.Add(QueryOperaterType.LessThan, "less than");
                    operatorNames.Add(QueryOperaterType.LessThanOrEqualTo, "less than or equal to");
                    operatorNames.Add(QueryOperaterType.Custom, "regular matches to");
                    operatorNames.Add(QueryOperaterType.DynamicLanguage, "");
                }
                return QueryOperatorHelper.operatorNames;
            }
        }

        public static Dictionary<QueryOperaterType, Tuple<string, string>> FilterConditionTemplates
        {
            get
            {
                if (filterConditionTemplates == null)
                {
                    filterConditionTemplates = new Dictionary<QueryOperaterType, Tuple<string, string>>();
                    filterConditionTemplates.Add(QueryOperaterType.DoesNotContain, new Tuple<string, string>("^(?!.*?", ").*$"));
                    filterConditionTemplates.Add(QueryOperaterType.DoesNotEqual, new Tuple<string, string>("^(?!", ").*?$"));
                    filterConditionTemplates.Add(QueryOperaterType.Contains, new Tuple<string, string>(".*", ".*"));
                    filterConditionTemplates.Add(QueryOperaterType.Equal, new Tuple<string, string>("^", "$"));
                    filterConditionTemplates.Add(QueryOperaterType.StartsWith, new Tuple<string, string>("^", string.Empty));
                    filterConditionTemplates.Add(QueryOperaterType.EndsWith, new Tuple<string, string>(string.Empty, "$"));
                    filterConditionTemplates.Add(QueryOperaterType.GreaterThanOrEqualTo, new Tuple<string, string>(">=", string.Empty));
                    filterConditionTemplates.Add(QueryOperaterType.GreaterThan, new Tuple<string, string>(">", string.Empty));
                    filterConditionTemplates.Add(QueryOperaterType.LessThanOrEqualTo, new Tuple<string, string>("<=", string.Empty));
                    filterConditionTemplates.Add(QueryOperaterType.LessThan, new Tuple<string, string>("<", string.Empty));
                    filterConditionTemplates.Add(QueryOperaterType.Custom, new Tuple<string, string>(string.Empty, string.Empty));
                    filterConditionTemplates.Add(QueryOperaterType.DynamicLanguage, new Tuple<string, string>(string.Empty, string.Empty));
                }

                return filterConditionTemplates;
            }
        }
    }
}