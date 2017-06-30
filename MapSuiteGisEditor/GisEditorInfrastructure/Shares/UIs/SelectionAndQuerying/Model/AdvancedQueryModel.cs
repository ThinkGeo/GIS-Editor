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
using System.Linq;
using System.Reflection;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    [Obfuscation]
    internal class AdvancedQueryModel
    {
        public IEnumerable<Feature> FindFeatures(ObservableCollection<QueryConditionViewModel> conditions, QueryMatchMode queryMatchMode)
        {
            IEnumerable<Feature> filteredFeatures = new Collection<Feature>();
            switch (queryMatchMode)
            {
                case QueryMatchMode.All:
                    var featuresGroup = conditions.Select(condition => AdvancedQueryViewModel.FilterFeatures(condition)).ToArray();

                    for (int i = 0; i < featuresGroup.Length; i++)
                    {
                        if (i == 0) filteredFeatures = featuresGroup[i];
                        else return Intersect(featuresGroup[i - 1], featuresGroup[i]);
                    }
                    break;

                case QueryMatchMode.Any:
                    filteredFeatures = conditions.SelectMany(condition => AdvancedQueryViewModel.FilterFeatures(condition));
                    Dictionary<string, Feature> result = new Dictionary<string, Feature>();
                    foreach (var item in filteredFeatures)
                    {
                        Feature newFeature = GisEditor.SelectionManager.GetSelectionOverlay().CreateHighlightFeature(item, (FeatureLayer)item.Tag);
                        if (!result.ContainsKey(newFeature.Id))
                        {
                            result.Add(newFeature.Id, newFeature);
                        }
                    }
                    return result.Values;
            }

            return filteredFeatures;
        }

        private IEnumerable<Feature> Intersect(IEnumerable<Feature> SourceFeatures, IEnumerable<Feature> TargetFeatures)
        {
            Collection<Feature> result = new Collection<Feature>();
            foreach (var item1 in SourceFeatures)
            {
                foreach (var item2 in TargetFeatures)
                {
                    string key1 = item1.Id + "|" + item1.Tag.GetHashCode().ToString();
                    string key2 = item2.Id + "|" + item2.Tag.GetHashCode().ToString();
                    if (key1 == key2)
                    {
                        result.Add(item1);
                    }
                }
            }
            return result;
        }

        private static bool CompareStringAsNumber(string string1, string string2, QueryOperaterType op)
        {
            double number1 = 0;
            double number2 = 0;

            if (double.TryParse(string1, out number1) && double.TryParse(string2, out number2))
            {
                if (op == QueryOperaterType.GreaterThan)
                {
                    return number1 > number2;
                }
                else if (op == QueryOperaterType.GreaterThanOrEqualTo)
                {
                    return number1 >= number2;
                }
                else if (op == QueryOperaterType.LessThan)
                {
                    return number1 < number2;
                }
                else if (op == QueryOperaterType.LessThanOrEqualTo)
                {
                    return number1 <= number2;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}