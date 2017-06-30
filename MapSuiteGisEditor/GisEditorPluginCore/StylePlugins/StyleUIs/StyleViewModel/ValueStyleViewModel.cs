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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;
using Style = ThinkGeo.MapSuite.Styles.Style;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ValueStyleViewModel : StyleViewModel
    {
        private static readonly string valueStyleItemNameFormat = "{0} = {1}";

        private ObservableCollection<ValueItemEntity> valueItems;
        private Dictionary<string, string> columnNames;
        private StyleBuilderArguments requiredValues;

        [NonSerialized]
        private RelayCommand addCommand;

        [NonSerialized]
        private RelayCommand autoGenerateCommand;

        [NonSerialized]
        private RelayCommand<string> removeCommand;

        [NonSerialized]
        private RelayCommand<string> editCommand;

        private ValueStyle actualValueStyle;
        private ValueItemEntity selectedValueItem;
        private Collection<DistinctColumnValue> distinctColumnValues;

        public ValueStyleViewModel(ValueStyle style, StyleBuilderArguments requiredValues)
            : base(style)
        {
            HelpKey = "ValueStyleHelp";
            this.valueItems = new ObservableCollection<ValueItemEntity>();
            this.ActualObject = style;
            this.actualValueStyle = style;
            this.requiredValues = requiredValues;
            this.columnNames = new Dictionary<string, string>();
            foreach (var columnName in requiredValues.ColumnNames)
            {
                string alias = requiredValues.FeatureLayer.FeatureSource.GetColumnAlias(columnName);
                this.columnNames[columnName] = alias;
            }

            if (string.IsNullOrEmpty(actualValueStyle.ColumnName))
            {
                SelectedColumnName = columnNames.FirstOrDefault();
            }

            InitializeValueItems();

            if (requiredValues.FeatureLayer != null) AnalyzeCount(requiredValues.FeatureLayer);
        }

        public StyleBuilderArguments RequiredValues
        {
            get { return requiredValues; }
        }

        public Dictionary<string, string> ColumnNames
        {
            get { return columnNames; }
        }

        public string ColumnName
        {
            get { return SelectedColumnName.Key; }
        }

        public KeyValuePair<string, string> SelectedColumnName
        {
            get
            {
                return columnNames.FirstOrDefault(c => c.Key == actualValueStyle.ColumnName);
                //return new KeyValuePair<string, string>(actualValueStyle.ColumnName, GisEditor.ActiveMap.GetFeatureSourceColumnAlias(requiredValues.FeatureLayer, actualValueStyle.ColumnName));
            }
            set
            {
                if (!actualValueStyle.ColumnName.Equals(value.Key, StringComparison.Ordinal))
                {
                    var originalColumnName = actualValueStyle.ColumnName;
                    actualValueStyle.ColumnName = value.Key;
                    RaisePropertyChanged("SelectedColumnName");

                    if (ValueItems.Count > 0)
                    {
                        var messageBoxResult = System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ValueStyleViewModelchangeColumnMessage")
                            , "Warning", System.Windows.Forms.MessageBoxButtons.YesNoCancel, System.Windows.Forms.MessageBoxIcon.Warning);
                        if (messageBoxResult == System.Windows.Forms.DialogResult.Yes)
                        {
                            ValueItems.Clear();
                            RaisePropertyChanged("ValueItems");
                        }
                        else if (messageBoxResult == System.Windows.Forms.DialogResult.No)
                        {
                            RaisePropertyChanged("ValueItems");
                        }
                        else
                        {
                            if (Application.Current != null)
                            {
                                Application.Current.Dispatcher.BeginInvoke(tmpObject =>
                                {
                                    actualValueStyle.ColumnName = (string)tmpObject;
                                    RaisePropertyChanged("SelectedColumnName");
                                }, originalColumnName, DispatcherPriority.Normal);
                            }
                        }
                    }
                }
            }
        }

        public ValueItemEntity SelectedValueItem
        {
            get { return selectedValueItem; }
            set
            {
                selectedValueItem = value;
                RaisePropertyChanged("SelectedValueItem");
            }
        }

        public ObservableCollection<ValueItemEntity> ValueItems
        {
            get { return valueItems; }
        }

        public RelayCommand AddCommand
        {
            get
            {
                if (addCommand == null)
                {
                    addCommand = new RelayCommand(Add);
                }
                return addCommand;
            }
        }

        public RelayCommand AutoGenerateCommand
        {
            get
            {
                if (autoGenerateCommand == null)
                {
                    autoGenerateCommand = new RelayCommand(AutoGenerate);
                }
                return autoGenerateCommand;
            }
        }

        public RelayCommand<string> RemoveCommand
        {
            get
            {
                if (removeCommand == null)
                {
                    removeCommand = new RelayCommand<string>((id) =>
                    {
                        var result = System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ValueStyleViewModelAreUSureMessage"), "Info", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Asterisk);
                        if (result == System.Windows.Forms.DialogResult.Yes)
                        {
                            var selectedItem = ValueItems
                                .FirstOrDefault(tmpItem => tmpItem.Id.Equals(id, StringComparison.Ordinal));
                            if (selectedItem != null)
                            {
                                ValueItems.Remove(selectedItem);
                                RaisePropertyChanged("ValueItems");
                            }
                        }
                    });
                } return removeCommand;
            }
        }

        public RelayCommand<string> EditCommand
        {
            get
            {
                if (editCommand == null)
                {
                    editCommand = new RelayCommand<string>((id) =>
                    {
                        var editingValueItem = ValueItems.FirstOrDefault(i => i.Id == id);
                        if (editingValueItem != null) editingValueItem.IsEditing = true;
                    });
                }
                return editCommand;
            }
        }

        private void Add()
        {
            if (string.IsNullOrEmpty(ColumnName))
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ValueStyleViewModelColumnCanntEmptyMessage"), "Column is Empty");
            else
            {
                var stylePlugin = GisEditor.StyleManager.GetDefaultStylePlugin(requiredValues.AvailableStyleCategories);
                if (stylePlugin != null)
                {
                    var newValueItem = new ValueItem();
                    newValueItem.Value = "Undefined";
                    newValueItem.CustomStyles.Add(stylePlugin.GetDefaultStyle());

                    var image = StyleHelper.GetImageFromStyle(newValueItem.CustomStyles[0]);
                    var valueItemEntity = GetValueItemEntity(image, newValueItem.Value, newValueItem);

                    //if (RequiredValues.FeatureLayer is ShapeFileFeatureLayer)
                    //{
                    //    valueItemEntity.Count = StyleHelper.CalculateSpeceficValuesCount(ColumnName
                    //        , newValueItem.Value, (ShapeFileFeatureLayer)RequiredValues.FeatureLayer);
                    //}
                    //else if (RequiredValues.FeatureLayer != null)
                    //{
                    //    if (distinctColumnValues == null)
                    //    {
                    //        RequiredValues.FeatureLayer.SafeProcess(() =>
                    //        {
                    //            distinctColumnValues = RequiredValues.FeatureLayer.FeatureSource.GetDistinctColumnValues(ColumnName);
                    //        });
                    //    }

                    //    if (distinctColumnValues != null && distinctColumnValues.Count > 0)
                    //    {
                    //        var distinctColumnValue = distinctColumnValues.FirstOrDefault(tmpFeature
                    //            => tmpFeature.ColumnValue
                    //            .Equals(newValueItem.Value, StringComparison.Ordinal));
                    //        if (distinctColumnValue != null)
                    //        {
                    //            valueItemEntity.Count = distinctColumnValue.ColumnValueCount;
                    //        }
                    //    }
                    //}
                    ValueItems.Add(valueItemEntity);
                    RaisePropertyChanged("ValueItems");
                }
            }
        }

        private void AutoGenerate()
        {
            if (string.IsNullOrEmpty(ColumnName))
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ValueStyleViewModelColumnCanntEmptyMessage"), "Column is Empty");
            else
            {
                FeatureLayer featureLayer = RequiredValues.FeatureLayer;
                FeatureLayerPlugin featureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
                if (featureLayerPlugin == null) return;

                string selectedColumnName = ColumnName;

                featureLayer.Open();
                if (featureLayer.FeatureSource.CanGetCountQuickly())
                {
                    int count = featureLayer.FeatureSource.GetCount();
                    if (count > 500000)
                    {
                        //MessageBoxResult result = MessageBox.Show(string.Format("{0} contains a large amount of records, it might spend too much time to process. Do you want to continue?", featureLayer.Name), "Info", MessageBoxButton.YesNo, MessageBoxImage.Information);
                        //if (result == MessageBoxResult.No)
                        //{
                        //    return;
                        //}
                    }
                }

                //Collection<DistinctColumnValue> distinctColumnValues = null;
                if (distinctColumnValues == null)
                {
                    featureLayer.SafeProcess(() =>
                    {
                        distinctColumnValues = featureLayer.FeatureSource.GetDistinctColumnValues(selectedColumnName);
                    });
                }
                if (distinctColumnValues != null && distinctColumnValues.Count() > 0)
                {
                    SimpleShapeType shpType = SimpleShapeType.Unknown;
                    featureLayer.SafeProcess(() =>
                    {
                        shpType = featureLayerPlugin.GetFeatureSimpleShapeType(featureLayer);
                    });

                    foreach (var columnValue in distinctColumnValues)
                    {
                        switch (shpType)
                        {
                            case SimpleShapeType.Point:
                                InitValueItem<PointStyle>(StyleCategories.Point, columnValue, selectedColumnName);
                                break;

                            case SimpleShapeType.Line:
                                InitValueItem<LineStyle>(StyleCategories.Line, columnValue, selectedColumnName);
                                break;

                            case SimpleShapeType.Area:
                                InitValueItem<AreaStyle>(StyleCategories.Area, columnValue, selectedColumnName);
                                break;

                            case SimpleShapeType.Unknown:
                                InitValueItem<Style>(StyleCategories.Composite, columnValue, selectedColumnName);
                                break;
                        }
                    }
                    var itemSourceList = ValueItems.OrderBy(itemValue => itemValue.ValueItem.Value).ToList();
                    ValueItems.Clear();
                    foreach (var item in itemSourceList.Where(i => !String.IsNullOrEmpty(i.MatchedValue)))
                    {
                        ValueItems.Add(item);
                    }
                    foreach (var item in itemSourceList.Where(i => String.IsNullOrEmpty(i.MatchedValue)))
                    {
                        ValueItems.Add(item);
                    }

                    SyncActualValueItems();
                    RaisePropertyChanged("ValueItems");
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ValueStyleViewModelColumnNoValueMatchedMessage"));
                }
            }
        }

        private void InitValueItem<T>(StyleCategories styleProviderType, DistinctColumnValue columnValueGroup, string columnName) where T : Style
        {
            Collection<Style> styles = new Collection<Style>();

            if (styleProviderType == StyleCategories.Composite)
            {
                Collection<StylePlugin> plugins = new Collection<StylePlugin>();
                var areaPlugin = GisEditor.StyleManager.GetDefaultStylePlugin(StyleCategories.Area);
                if (areaPlugin != null)
                {
                    plugins.Add(areaPlugin);
                }
                var linePlugin = GisEditor.StyleManager.GetDefaultStylePlugin(StyleCategories.Line);
                if (linePlugin != null)
                {
                    plugins.Add(linePlugin);
                }
                var pointPlugin = GisEditor.StyleManager.GetDefaultStylePlugin(StyleCategories.Point);
                if (pointPlugin != null)
                {
                    plugins.Add(pointPlugin);
                }
                foreach (var item in plugins)
                {
                    Style style = GetStyleByPlugin(item);
                    if (style != null)
                    {
                        styles.Add(style);
                    }
                }
            }
            else
            {
                var styleProvider = GisEditor.StyleManager.GetDefaultStylePlugin(styleProviderType);
                if (styleProvider != null)
                {
                    Style style = GetStyleByPlugin(styleProvider);
                    if (style != null)
                    {
                        styles.Add(style);
                    }
                }
            }

            if (styles.Count > 0)
            {
                ValueItem valueItem = new ValueItem();
                valueItem.Value = columnValueGroup.ColumnValue;
                styles.ForEach(s => valueItem.CustomStyles.Add(s));

                ValueItemEntity valueItemEntity = ValueItems.FirstOrDefault(tmpItem
                    => tmpItem.MatchedValue.Equals(columnValueGroup.ColumnValue, StringComparison.Ordinal));

                if (valueItemEntity == null)
                {
                    valueItem = new ValueItem();
                    valueItem.Value = columnValueGroup.ColumnValue;
                    styles.ForEach(s => valueItem.CustomStyles.Add(s));

                    var image = StyleHelper.GetImageFromStyle(styles);
                    var newValueItemEntity = GetValueItemEntity(image, columnValueGroup.ColumnValue, valueItem);
                    newValueItemEntity.Count = columnValueGroup.ColumnValueCount;
                    ValueItems.Add(newValueItemEntity);
                }
                else
                {
                    //valueItemEntity.Update(valueItem);
                    valueItemEntity.Count = columnValueGroup.ColumnValueCount;
                }
            }
        }

        private static Style GetStyleByPlugin(StylePlugin styleProvider)
        {
            Style style = null;
            bool needRestore = false;
            if (!styleProvider.UseRandomColor)
            {
                styleProvider.UseRandomColor = true;
                needRestore = true;
            }

            // Apply more colors to avoid duplicated colors.
            GeoColorHelper.RandomColorType = RandomColorType.All;
            Style tmpStyle = styleProvider.GetDefaultStyle();
            GeoColorHelper.RandomColorType = RandomColorType.Pastel;

            if (needRestore) styleProvider.UseRandomColor = false;
            if (tmpStyle != null)
            {
                style = tmpStyle.CloneDeep();
                style.Name = GisEditor.StyleManager.GetStylePluginByStyle(style).Name;
            }
            return style;
        }

        public void SyncActualValueItems()
        {
            actualValueStyle.ValueItems.Clear();
            foreach (var item in ValueItems)
            {
                actualValueStyle.ValueItems.Add(item.ValueItem);
            }
        }

        private void InitializeValueItems()
        {
            valueItems = new ObservableCollection<ValueItemEntity>(actualValueStyle.ValueItems.Select(tmpItem =>
            {
                var image = StyleHelper.GetImageFromStyle(tmpItem.CustomStyles.LastOrDefault());
                return GetValueItemEntity(image, tmpItem.Value, tmpItem);
            }).ToList());

            valueItems.CollectionChanged += (s, e) => { SyncActualValueItems(); };
        }

        private ValueItemEntity GetValueItemEntity(Image image, string matchedValue, ValueItem valueItem)
        {
            ValueItemEntity valueItemEntity = new ValueItemEntity();
            valueItemEntity.Image = image;
            valueItemEntity.MatchedValue = matchedValue;
            valueItemEntity.ValueItem = valueItem;
            valueItemEntity.PropertyChanged += new PropertyChangedEventHandler(ValueItemEntity_PropertyChanged);
            return valueItemEntity;
        }

        private void ValueItemEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("MatchedValue"))
            {
                RaisePropertyChanged(e.PropertyName);
            }
        }

        internal void AnalyzeCount(FeatureLayer featureLayer)
        {
            if (ValueItems.Count > 0)
            {
                featureLayer.SafeProcess(() =>
                {
                    //ShapeFileFeatureLayer shpFeatureLayer = featureLayer as ShapeFileFeatureLayer;
                    //IEnumerable<IGrouping<string, string>> fieldGroups = null;

                    if (distinctColumnValues == null)
                    {
                        distinctColumnValues = featureLayer.FeatureSource.GetDistinctColumnValues(ColumnName);
                    }

                    //if (shpFeatureLayer != null)
                    //{
                    //    fieldGroups = StyleHelper.GroupColumnValues(ColumnName, shpFeatureLayer);
                    //}
                    //else if (featureLayer != null)
                    //{
                    //    fieldGroups = featureLayer.QueryTools.GetAllFeatures(featureLayer.GetDistinctColumnNames())
                    //        .Select(tmpFeature => tmpFeature.ColumnValues[ColumnName]).GroupBy(fieldValue => fieldValue);
                    //}

                    if (distinctColumnValues != null && distinctColumnValues.Count > 0)
                    {
                        foreach (var itemEntity in ValueItems)
                        {
                            var tmpGroup = distinctColumnValues.FirstOrDefault(g => g.ColumnValue.Equals(itemEntity.MatchedValue, StringComparison.Ordinal));
                            if (tmpGroup != null) itemEntity.Count = tmpGroup.ColumnValueCount;
                            else itemEntity.Count = 0;
                        }
                    }
                });
            }
        }

        public static string GetValueItemStyleName(string columnName, string matchingValue)
        {
            return string.Format(CultureInfo.InvariantCulture, valueStyleItemNameFormat, columnName, matchingValue);
        }
    }
}