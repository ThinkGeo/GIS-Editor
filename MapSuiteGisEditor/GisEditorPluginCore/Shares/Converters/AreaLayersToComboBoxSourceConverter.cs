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
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class AreaLayersToComboBoxSourceConverter : ValueConverter
    {
        private SelectColumnsEntity allLayersEntity;
        private List<SelectColumnsEntity> entities;

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Collection<FeatureLayer> areaLayers = value as Collection<FeatureLayer>;

            if (areaLayers == null)
            {
                throw new ArgumentException("The parameter \"value\" is not valid.");
            }
            else
            {
                entities = areaLayers.Select(layer => new SelectColumnsEntity(layer.Name, GetColumns(layer))).ToList();
                allLayersEntity = new SelectColumnsEntity("All Layers");
                var allColumns = new Collection<FeatureSourceColumnDefinition>();
                entities.ForEach(ent =>
                {
                    foreach (var column in ent.Columns)
                    {
                        allColumns.Add(column);
                    }
                    ent.Columns.CollectionChanged += Columns_CollectionChanged;
                });

                foreach (var item in FeatureSourceColumnDefinition.GetFixedColumnDefinitions(allColumns))
                {
                    allLayersEntity.Columns.Add(item);
                }
                allLayersEntity.Columns.CollectionChanged += AllColumns_CollectionChanged;
                entities.Insert(0, allLayersEntity);

                return entities;
            }
        }

        private void AllColumns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var group in e.NewItems.OfType<FeatureSourceColumnDefinition>().GroupBy(f => f.LayerName))
                {
                    var result = entities.FirstOrDefault(en => en.LayerName.Equals(group.Key));
                    if (result != null)
                    {
                        foreach (var item in group)
                        {
                            if (!result.Columns.Contains(item))
                            {
                                result.Columns.Add(item);
                            }
                        }
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var group in e.OldItems.OfType<FeatureSourceColumnDefinition>().GroupBy(f => f.LayerName))
                {
                    var result = entities.FirstOrDefault(en => en.LayerName.Equals(group.Key));
                    if (result != null)
                    {
                        foreach (var item in group)
                        {
                            if (result.Columns.Contains(item))
                            {
                                result.Columns.Remove(item);
                            }
                        }
                    }
                }
            }
        }

        private void Columns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems.OfType<FeatureSourceColumnDefinition>())
                {
                    if (!allLayersEntity.Columns.Contains(item))
                    {
                        allLayersEntity.Columns.Add(item);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems.OfType<FeatureSourceColumnDefinition>())
                {
                    if (allLayersEntity.Columns.Contains(item))
                    {
                        allLayersEntity.Columns.Remove(item);
                    }
                }
            }
        }

        private static ObservableCollection<FeatureSourceColumnDefinition> GetColumns(FeatureLayer layer)
        {
            ObservableCollection<FeatureSourceColumnDefinition> results = new ObservableCollection<FeatureSourceColumnDefinition>();
            layer.SafeProcess(() =>
            {
                foreach (var item in layer.FeatureSource.GetColumns())
                {
                    FeatureSourceColumnDefinition featureSourceColumnDefinition = new FeatureSourceColumnDefinition(item, layer);
                    string alias = layer.FeatureSource.GetColumnAlias(featureSourceColumnDefinition.ColumnName);
                    featureSourceColumnDefinition.AliasName = alias;

                    results.Add(featureSourceColumnDefinition);
                }
            });

            return results;
        }
    }
}