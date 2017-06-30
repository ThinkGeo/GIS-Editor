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
using System.Globalization;
using System.Linq;
using System.Reflection;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class FeatureSourceColumnDefinition
    {
        private static Dictionary<string, Collection<Tuple<string, string>>> renameDictionary;

        public static Dictionary<string, Collection<Tuple<string, string>>> RenameDictionary
        {
            get { return renameDictionary; }
        }

        static FeatureSourceColumnDefinition()
        {
            renameDictionary = new Dictionary<string, Collection<Tuple<string, string>>>();
        }

        private static readonly string columnNameTemplate = "{0}_{1}";
        private static readonly string duplicateColumnNameDisplayTemplate = "{0} ({1})";

        [Obfuscation]
        private string columnName;

        [Obfuscation]
        private string layerName;

        [Obfuscation]
        private string originalName;

        [Obfuscation]
        private string aliasName;

        [Obfuscation]
        private bool isDuplicate;

        [Obfuscation]
        private FeatureSourceColumn featureSourceColumn;

        [Obfuscation]
        private FeatureLayer featureLayer;

        private FeatureSourceColumnDefinition()
            : this(null, null)
        { }

        public FeatureSourceColumnDefinition(FeatureSourceColumn featureSourceColumn, FeatureLayer featureLayer)
        {
            this.isDuplicate = false;
            this.featureSourceColumn = featureSourceColumn;
            this.originalName = featureSourceColumn.ColumnName;
            this.columnName = featureSourceColumn.ColumnName;
            this.featureLayer = featureLayer;
            this.layerName = featureLayer.Name;
        }

        public string LayerName
        {
            get { return layerName; }
        }

        public string ColumnName
        {
            get { return columnName; }
            set { columnName = value; }
        }

        public string OriginalName
        {
            get { return originalName; }
        }

        public bool IsDuplicate
        {
            get { return isDuplicate; }
            set { isDuplicate = value; }
        }

        public FeatureLayer FeatureLayer
        {
            get { return featureLayer; }
        }

        public string DisplayName
        {
            get { return IsDuplicate ? string.Format(duplicateColumnNameDisplayTemplate, originalName, layerName) : columnName; }
        }

        public string AliasName
        {
            get { return IsDuplicate ? string.Format(duplicateColumnNameDisplayTemplate, aliasName, layerName) : aliasName; }
            set { aliasName = value; }
        }

        public FeatureSourceColumn ToFeatureSourceColumn()
        {
            return new FeatureSourceColumn(ColumnName, featureSourceColumn.TypeName, featureSourceColumn.MaxLength);
        }

        public static List<FeatureSourceColumnDefinition> GetFixedColumnDefinitions(IEnumerable<FeatureSourceColumnDefinition> columnDefinitions)
        {
            renameDictionary.Clear();
            foreach (var item in columnDefinitions)
            {
                if (!renameDictionary.ContainsKey(item.LayerName))
                {
                    renameDictionary.Add(item.LayerName, new Collection<Tuple<string, string>>());
                }
            }

            foreach (var group in columnDefinitions.GroupBy(columnDefinition => columnDefinition.ColumnName))
            {
                if (group.Count() > 1)
                {
                    int index = 1;
                    Dictionary<string, int> layerNames = new Dictionary<string, int>();
                    foreach (var item in group)
                    {
                        item.IsDuplicate = true;
                        string shortLayerName = item.LayerName;
                        if (shortLayerName.Length > 3)
                        {
                            shortLayerName = shortLayerName.Substring(0, 3);
                        }
                        string shortLayerNameUpper = shortLayerName.ToUpperInvariant();
                        if (layerNames.ContainsKey(shortLayerNameUpper))
                        {
                            int number = layerNames[shortLayerNameUpper] + 1;
                            layerNames[shortLayerNameUpper] = number;
                            int totalLayerNameLength = (shortLayerName + number.ToString()).Length;
                            int subLength = shortLayerName.Length - (totalLayerNameLength - shortLayerName.Length);
                            if (subLength > 0)
                            {
                                shortLayerName = shortLayerName.Substring(0, subLength) + number.ToString();
                            }
                            else
                            {
                                shortLayerName = number.ToString();
                            }
                        }
                        else
                        {
                            layerNames.Add(shortLayerNameUpper, 0);
                        }
                        item.ColumnName = string.Format(CultureInfo.InvariantCulture, columnNameTemplate, shortLayerName, item.ColumnName);
                        if (item.ColumnName.Length > 10)
                        {
                            int totalLength = item.ColumnName.Length + index.ToString().Length;
                            int remainLength = item.ColumnName.Length - (totalLength - 10);
                            item.ColumnName = item.ColumnName.Substring(0, remainLength) + index;
                        }
                        renameDictionary[item.LayerName].Add(new Tuple<string, string>(item.OriginalName, item.ColumnName));
                        index++;
                    }
                }
            }

            Collection<FeatureSourceColumnDefinition> finalResultColumnDefinitions = new Collection<FeatureSourceColumnDefinition>();

            foreach (var group in columnDefinitions.GroupBy(c => c.ColumnName))
            {
                int count = group.Count();
                if (count > 1)
                {
                    int index = 1;
                    foreach (var item in group)
                    {
                        string textIndex = index.ToString();
                        int totalLength = textIndex.Length + item.ColumnName.Length;
                        if (totalLength > 10)
                        {
                            int remainLength = item.ColumnName.Length - (totalLength - 10);
                            item.ColumnName = item.ColumnName.Substring(0, remainLength) + textIndex;
                        }
                        else
                        {
                            item.ColumnName = item.ColumnName + textIndex;
                        }
                        index++;
                        finalResultColumnDefinitions.Add(item);
                        if (renameDictionary.ContainsKey(item.LayerName))
                        {
                            Tuple<string, string> resultTuple = renameDictionary[item.LayerName].FirstOrDefault(r => r.Item1.Equals(item.OriginalName));
                            if (resultTuple != null)
                            {
                                renameDictionary[item.LayerName].Remove(resultTuple);
                            }
                        }
                        else
                        {
                            renameDictionary.Add(item.LayerName, new Collection<Tuple<string, string>>());
                        }
                        renameDictionary[item.LayerName].Add(new Tuple<string, string>(item.OriginalName, item.ColumnName));
                    }
                }
            }

            return columnDefinitions.ToList();
        }
    }
}