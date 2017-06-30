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


using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for SelectColumns.xaml
    /// </summary>
    public partial class MergeWizardSelectColumnsUserControl : UserControl
    {
        private MergeWizardShareObject entity;
        private Collection<FeatureSourceColumnDefinition> selectedAvaliableColumns;
        private Collection<FeatureSourceColumnDefinition> selectedIncludedColumns;
        private static Dictionary<string, List<FeatureSourceColumnDefinition>> duplicateColumnsDictionary;

        public MergeWizardSelectColumnsUserControl(MergeWizardShareObject parameter)
        {
            InitializeComponent();
            DataContext = parameter;
            entity = parameter;
            selectedAvaliableColumns = new Collection<FeatureSourceColumnDefinition>();
            selectedIncludedColumns = new Collection<FeatureSourceColumnDefinition>();

            Loaded += new RoutedEventHandler(MergeWizardSelectColumnsUserControl_Loaded);
        }

        private void MergeWizardSelectColumnsUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (entity.LayerColumnPair.Keys.Count == 0)
            {
                foreach (FeatureLayer featureLayer in entity.SelectedLayers.AsParallel())
                {
                    lock (featureLayer)
                    {
                        featureLayer.SafeProcess(() =>
                        {
                            var tempColums = new ObservableCollection<FeatureSourceColumnDefinition>(featureLayer.FeatureSource.GetColumns().Select(c => new FeatureSourceColumnDefinition(c, featureLayer)));
                            tempColums.CollectionChanged += new NotifyCollectionChangedEventHandler(TempColums_CollectionChanged);
                            foreach (var tempColumn in tempColums)
                            {
                                tempColumn.AliasName = featureLayer.FeatureSource.GetColumnAlias(tempColumn.ColumnName);
                            }
                            entity.LayerColumnPair.Add(featureLayer, tempColums);
                        });
                    }
                }

                var allColumnDefinitions = new ObservableCollection<FeatureSourceColumnDefinition>(entity.LayerColumnPair.SelectMany(pair => pair.Value).ToArray());
                if (entity.LayerColumnPair.Keys.Count(l => l.Name == GisEditor.LanguageManager.GetStringResource("MergeWizardSelectColumnsStepAllLayersName")) == 0)
                {
                    allColumnDefinitions.CollectionChanged += new NotifyCollectionChangedEventHandler(AllColumnDefinitions_CollectionChanged);
                    entity.LayerColumnPair.Add(new ShapeFileFeatureLayer() { Name = GisEditor.LanguageManager.GetStringResource("MergeWizardSelectColumnsStepAllLayersName") }, allColumnDefinitions);
                }
                HandleCommonColumns(entity, allColumnDefinitions);
                entity.SelectedLayerForColumns = entity.LayerColumnPair.Keys.ElementAt(entity.LayerColumnPair.Keys.Count - 1);
            }
        }

        private void AllColumnDefinitions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems.OfType<FeatureSourceColumnDefinition>())
                {
                    var allColumns = entity.LayerColumnPair[item.FeatureLayer];
                    if (!allColumns.Contains(item))
                    {
                        allColumns.Add(item);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems.OfType<FeatureSourceColumnDefinition>())
                {
                    var allColumns = entity.LayerColumnPair[item.FeatureLayer];
                    if (allColumns.Contains(item))
                    {
                        allColumns.Remove(item);
                    }
                }
            }
        }

        private void TempColums_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var featureLayer = entity.LayerColumnPair.Keys.FirstOrDefault(l => l.Name == GisEditor.LanguageManager.GetStringResource("MergeWizardSelectColumnsStepAllLayersName"));
            if (featureLayer != null && entity.LayerColumnPair.ContainsKey(featureLayer))
            {
                var allColumns = entity.LayerColumnPair[featureLayer];
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (var item in e.NewItems.OfType<FeatureSourceColumnDefinition>())
                    {
                        if (!allColumns.Contains(item))
                        {
                            allColumns.Add(item);
                        }
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (var item in e.OldItems.OfType<FeatureSourceColumnDefinition>())
                    {
                        if (allColumns.Contains(item))
                        {
                            allColumns.Remove(item);
                        }
                    }
                }
            }
        }

        private static void HandleCommonColumns(MergeWizardShareObject parameter, IEnumerable<FeatureSourceColumnDefinition> allColumns)
        {
            duplicateColumnsDictionary = new Dictionary<string, List<FeatureSourceColumnDefinition>>();

            foreach (var group in allColumns.GroupBy(c => c.OriginalName))
            {
                var groupColumns = group.ToList();
                if (groupColumns.Count > 1)
                {
                    List<FeatureSourceColumnDefinition> commonColumns = new List<FeatureSourceColumnDefinition>();
                    duplicateColumnsDictionary.Add(group.Key, commonColumns);
                    foreach (var item in group)
                    {
                        item.IsDuplicate = true;
                        commonColumns.Add(item);
                    }
                }
            }

            foreach (var pair in duplicateColumnsDictionary)
            {
                foreach (var item in pair.Value)
                {
                    if (parameter.LayerColumnPair.ContainsKey(item.FeatureLayer))
                    {
                        parameter.LayerColumnPair[item.FeatureLayer].Remove(item);
                    }
                }

                FeatureSourceColumnDefinition firstColumn = pair.Value.First();

                if (parameter.IncludedColumns.Count(c => c.OriginalName.Equals(firstColumn.OriginalName)) == 0)
                {
                    parameter.IncludedColumns.Add(firstColumn);
                }
            }
        }

        [Obfuscation]
        private void AvaliableColumns_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                foreach (object obj in e.AddedItems)
                {
                    selectedAvaliableColumns.Add((FeatureSourceColumnDefinition)obj);
                }
            }
            if (e.RemovedItems.Count > 0)
            {
                foreach (object obj in e.RemovedItems)
                {
                    selectedAvaliableColumns.Remove((FeatureSourceColumnDefinition)obj);
                }
            }
        }

        [Obfuscation]
        private void IncludedColumns_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                foreach (object obj in e.AddedItems)
                {
                    selectedIncludedColumns.Add((FeatureSourceColumnDefinition)obj);
                }
            }
            if (e.RemovedItems.Count > 0)
            {
                foreach (object obj in e.RemovedItems)
                {
                    selectedIncludedColumns.Remove((FeatureSourceColumnDefinition)obj);
                }
            }
        }

        [Obfuscation]
        private void LeftToRight_Click(object sender, RoutedEventArgs e)
        {
            if (selectedAvaliableColumns.Count > 0)
            {
                foreach (FeatureSourceColumnDefinition column in selectedAvaliableColumns.ToArray())
                {
                    if (!entity.IncludedColumns.Any(c => c.OriginalName.Equals(column.OriginalName)))
                    {
                        entity.IncludedColumns.Add(column);
                        entity.LayerColumnPair[entity.SelectedLayerForColumns].Remove(column);
                        if (column.IsDuplicate)
                        {
                            foreach (var keyValue in entity.LayerColumnPair.Where(l => l.Key.Name != "All Layers"))
                            {
                                FeatureSourceColumnDefinition match = keyValue.Value.FirstOrDefault(c => c.OriginalName.Equals(column.OriginalName));
                                if (match != null)
                                {
                                    keyValue.Value.Remove(match);
                                }
                            }
                        }
                    }
                }
            }
        }

        [Obfuscation]
        private void RightToLeft_Click(object sender, RoutedEventArgs e)
        {
            if (selectedIncludedColumns.Count > 0)
            {
                foreach (FeatureSourceColumnDefinition column in selectedIncludedColumns.ToArray())
                {
                    entity.IncludedColumns.Remove(column);

                    if (!column.IsDuplicate)
                    {
                        var allColumns = entity.LayerColumnPair[column.FeatureLayer];
                        if (!allColumns.Contains(column))
                        {
                            allColumns.Add(column);
                        }
                    }
                    else
                    {
                        foreach (var item in duplicateColumnsDictionary[column.OriginalName])
                        {
                            var allColumns = entity.LayerColumnPair[item.FeatureLayer];
                            if (!allColumns.Contains(item))
                            {
                                allColumns.Add(item);
                            }
                        }
                    }
                }
            }
        }

        [Obfuscation]
        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (includedListBox.SelectedItems.Count > 0)
            {
                Collection<FeatureSourceColumnDefinition> tmpColumns = new Collection<FeatureSourceColumnDefinition>();
                foreach (object obj in includedListBox.SelectedItems)
                {
                    tmpColumns.Add((FeatureSourceColumnDefinition)obj);
                }
                foreach (FeatureSourceColumnDefinition column in tmpColumns)
                {
                    int index = entity.IncludedColumns.IndexOf(column);
                    if (index < entity.IncludedColumns.Count - 1)
                    {
                        entity.IncludedColumns.Move(index, index + 1);
                    }
                }
            }
        }

        [Obfuscation]
        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (includedListBox.SelectedItems.Count > 0)
            {
                Collection<FeatureSourceColumnDefinition> tmpColumns = new Collection<FeatureSourceColumnDefinition>();
                foreach (object obj in includedListBox.SelectedItems)
                {
                    tmpColumns.Add((FeatureSourceColumnDefinition)obj);
                }
                foreach (FeatureSourceColumnDefinition column in tmpColumns)
                {
                    int index = entity.IncludedColumns.IndexOf(column);
                    if (index >= 1)
                    {
                        entity.IncludedColumns.Move(index, index - 1);
                    }
                }
            }
        }

        [Obfuscation]
        private void AvaliableAll_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                if (checkBox.IsChecked.Value)
                    avaliableListBox.SelectAll();
                else
                    avaliableListBox.UnselectAll();
            }
        }

        [Obfuscation]
        private void Include_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                if (checkBox.IsChecked.Value)
                    includedListBox.SelectAll();
                else
                    includedListBox.UnselectAll();
            }
        }
    }
}