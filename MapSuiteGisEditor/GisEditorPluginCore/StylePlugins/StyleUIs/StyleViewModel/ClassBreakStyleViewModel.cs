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
using System.Windows;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ClassBreakStyleViewModel : StyleViewModel
    {
        private static readonly string classBreakStyleItemNameFormat = "{0} starting value is \"{1}\"";

        private StyleBuilderArguments requiredValues;
        private ObservableCollection<ClassBreakItem> classBreakItems;
        private ClassBreakStyle actualClassBreakStyle;
        private ClassBreakItem selectedClassBreakItem;
        private Dictionary<string, string> columns;

        [NonSerialized]
        private RelayCommand autoGenerateCommand;

        [NonSerialized]
        private RelayCommand addNewCommand;

        [NonSerialized]
        private ObservedCommand clearAllCommand;

        [NonSerialized]
        private RelayCommand<string> removeCommand;

        [NonSerialized]
        private RelayCommand<string> editCommand;


        public ClassBreakStyleViewModel(ClassBreakStyle style, StyleBuilderArguments requiredValues)
            : base(style)
        {
            this.HelpKey = "ClassBreakStyleHelp";
            this.classBreakItems = new ObservableCollection<ClassBreakItem>();
            foreach (var item in style.ClassBreaks)
            {
                classBreakItems.Add(GetClassBreakItem(item));
            }
            this.ActualObject = style;
            this.actualClassBreakStyle = style;
            this.requiredValues = requiredValues;

            this.columns = new Dictionary<string, string>();
            foreach (var columnName in requiredValues.ColumnNames)
            {
                string alias = requiredValues.FeatureLayer.FeatureSource.GetColumnAlias(columnName);
                this.columns[columnName] = alias;
            }

            this.SelectedColumnName = this.columns.FirstOrDefault(c => c.Key == style.ColumnName);
        }

        public StyleBuilderArguments RequiredValues
        {
            get { return requiredValues; }
        }

        public string ColumnName
        {
            get
            {
                return SelectedColumnName.Key;
            }
        }

        public KeyValuePair<string, string> SelectedColumnName
        {
            get
            {
                return columns.FirstOrDefault(c => c.Key == actualClassBreakStyle.ColumnName);
            }
            set
            {
                if (actualClassBreakStyle.ColumnName != value.Key)
                {
                    var originalColumnName = actualClassBreakStyle.ColumnName;
                    actualClassBreakStyle.ColumnName = value.Key;
                    var isValid = IsValidColumn(value.Key);
                    if (!isValid) RollbackPreviousColumnName(originalColumnName);
                    RaisePropertyChanged("SelectedColumnName");

                    if (isValid && ClassBreakItems.Count > 0)
                    {
                        var messageBoxResult = System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ClassBreakStyleViewModelNotValidMessage")
                            , "Warning", System.Windows.Forms.MessageBoxButtons.YesNoCancel, System.Windows.Forms.MessageBoxIcon.Warning);

                        if (messageBoxResult == System.Windows.Forms.DialogResult.Yes)
                        {
                            ClassBreakItems.Clear();
                            actualClassBreakStyle.ClassBreaks.Clear();
                            RaisePropertyChanged("ClassBreakItems");
                        }
                        else if (messageBoxResult == System.Windows.Forms.DialogResult.No)
                        {
                            RaisePropertyChanged("ClassBreakItems");
                        }
                        else
                        {
                            RollbackPreviousColumnName(originalColumnName);
                        }
                    }
                }
            }
        }

        public ClassBreakItem SelectedClassBreakItem
        {
            get { return selectedClassBreakItem; }
            set
            {
                selectedClassBreakItem = value;
                RaisePropertyChanged("SelectedClassBreakItem");
            }
        }

        public ObservableCollection<ClassBreakItem> ClassBreakItems
        {
            get
            {
                return classBreakItems;
            }
        }

        public Dictionary<string, string> Columns
        {
            get
            {
                return columns;
                //string[] requiredColumns = new string[RequiredValues.ColumnNames.Count];
                //for (int i = 0; i < requiredColumns.Length; i++)
                //{
                //    requiredColumns[i] = RequiredValues.ColumnNames[i];
                //}
                //return requiredColumns;
            }
        }

        public RelayCommand AutoGenerateCommand
        {
            get
            {
                if (autoGenerateCommand == null)
                {
                    autoGenerateCommand = new RelayCommand(() =>
                    {
                        if (string.IsNullOrEmpty(ColumnName))
                        {
                            var message = new DialogMessage(GisEditor.LanguageManager.GetStringResource("ClassBreakStyleViewModelSelectColumnMessage"), null)
                            {
                                Caption = "Info",
                                Button = MessageBoxButton.OK,
                                Icon = MessageBoxImage.Information
                            };
                            ShowMessageBox(message);
                        }
                        else
                        {
                            var message = new NotificationMessageAction<ClassBreakViewModel>("Auto Generate Class Break", tmpViewModel =>
                            {
                                double minValue = tmpViewModel.LowValue;
                                double maxValue = tmpViewModel.HighValue;

                                string itemStyleName = "";
                                Collection<GeoSolidBrush> brushes = tmpViewModel.CollectBrushes();
                                double stepValue = (maxValue - minValue) / (brushes.Count - 1);
                                //classBreakItems.Clear();
                                //actualClassBreakStyle.ClassBreaks.Clear();

                                for (int i = brushes.Count - 1; i >= 0; i--)
                                {
                                    GeoSolidBrush brush = brushes[i];
                                    double sourceValue = (maxValue - (brushes.Count - i - 1) * stepValue);
                                    double startingValue = Math.Round(sourceValue, 4);
                                    if (i == brushes.Count - 1 && startingValue < sourceValue)
                                    {
                                        startingValue += 0.0001;
                                    }
                                    else if (i == 0 && startingValue > sourceValue)
                                    {
                                        startingValue -= 0.0001;
                                    }
                                    itemStyleName = GetClassBreakItemStyleName(ColumnName, startingValue);
                                    var defaultStylePlugin = GetDefaultAvailableStylePlugin();
                                    bool tempUseRandomColor = defaultStylePlugin.UseRandomColor;
                                    defaultStylePlugin.UseRandomColor = true;
                                    var newStyle = defaultStylePlugin.GetDefaultStyle();
                                    defaultStylePlugin.UseRandomColor = tempUseRandomColor;

                                    var areaStyle = newStyle as AreaStyle;
                                    var pointStyle = newStyle as PointStyle;
                                    var lineStyle = newStyle as LineStyle;

                                    if (areaStyle != null)
                                        areaStyle.FillSolidBrush = brushes[i];
                                    else if (pointStyle != null)
                                        pointStyle.SymbolSolidBrush = brushes[i];
                                    else if (lineStyle != null)
                                        lineStyle.OuterPen.Brush = brushes[i];

                                    newStyle.Name = itemStyleName;

                                    Collection<Styles.Style> customStyles = new Collection<Styles.Style>();
                                    customStyles.Add(newStyle);
                                    newStyle.Name = GisEditor.StyleManager.GetStylePluginByStyle(newStyle).Name;
                                    var classBreak = new ClassBreak(startingValue, customStyles);
                                    classBreakItems.Add(GetClassBreakItem(classBreak));
                                    actualClassBreakStyle.ClassBreaks.Add(classBreak);
                                }
                            });

                            ShowAutoGenerateWindow(message);
                        }
                    });
                }
                return autoGenerateCommand;
            }
        }

        public RelayCommand AddNewCommand
        {
            get
            {
                if (addNewCommand == null)
                {
                    addNewCommand = new RelayCommand(() =>
                    {
                        if (string.IsNullOrEmpty(ColumnName))
                        {
                            var dialogMessage = new DialogMessage(GisEditor.LanguageManager.GetStringResource("ClassBreakStyleViewModelSelectColumnAddNewMessage"), null) { Caption = "Info", Icon = MessageBoxImage.Information, Button = MessageBoxButton.OK };
                            ShowMessageBox(dialogMessage);
                        }
                        else
                        {
                            var stylePlugin = GisEditor.StyleManager.GetDefaultStylePlugin(requiredValues.AvailableStyleCategories);
                            if (stylePlugin != null)
                            {
                                var resultClassBreak = new ClassBreak();
                                resultClassBreak.Value = 0;
                                resultClassBreak.CustomStyles.Add(stylePlugin.GetDefaultStyle());
                                ClassBreakItem classBreak = classBreakItems
                                    .FirstOrDefault(entity => entity.StartingValue.Equals(resultClassBreak.Value.ToString(), StringComparison.Ordinal));
                                classBreakItems.Add(GetClassBreakItem(resultClassBreak));
                                actualClassBreakStyle.ClassBreaks.Add(resultClassBreak);
                                RaisePropertyChanged("ClassBreakItems");
                            }
                        }
                    });
                }
                return addNewCommand;
            }
        }

        public ObservedCommand ClearAllCommand
        {
            get
            {
                if (clearAllCommand == null)
                {
                    clearAllCommand = new ObservedCommand(() =>
                    {
                        if (MessageBox.Show("Are you sure you want to clear these classbreaks?", "Clear Classbreaks", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            ClassBreakItems.Clear();
                            actualClassBreakStyle.ClassBreaks.Clear();
                            RaisePropertyChanged("ClassBreakItems");
                        }
                    }, () => ClassBreakItems.Count() > 0);
                }

                return clearAllCommand;
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
                        var message = new DialogMessage(GisEditor.LanguageManager.GetStringResource("ClassBreakStyleViewModelAreYouSureMessage"), null) { Caption = "Info", Button = MessageBoxButton.YesNo, Icon = MessageBoxImage.Asterisk };
                        if (ShowMessageBox(message) == MessageBoxResult.Yes)
                        {
                            var item = ClassBreakItems.FirstOrDefault(i => { return i.Id == id; });
                            ClassBreakItems.Remove(item);
                            actualClassBreakStyle.ClassBreaks.Remove(item.ClassBreak);
                            RaisePropertyChanged("ClassBreakItems");
                        }
                    });
                }
                return removeCommand;
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
                        //EditClassBreakItem(ClassBreakItems, ClassBreakItems.FirstOrDefault(item => item.Id == id));
                        ClassBreakItems.FirstOrDefault(item => item.Id == id).IsEditing = true;
                    });
                }
                return editCommand;
            }
        }

        private void ShowAutoGenerateWindow(NotificationMessageAction<ClassBreakViewModel> message)
        {
            ClassBreakStyleAutoGenerateWindow window = new ClassBreakStyleAutoGenerateWindow();
            window.Title = message.Notification;
            double maxValue = double.MinValue;
            double minValue = double.MaxValue;

            if (requiredValues.FeatureLayer != null)
            {
                ShapeFileFeatureLayer shapeFileFeatureLayer = requiredValues.FeatureLayer as ShapeFileFeatureLayer;
                //if (shapeFileFeatureLayer != null)
                //{
                //    double tmpMinValue = double.MaxValue;
                //    double tmpMaxValue = double.MinValue;
                //    shapeFileFeatureLayer.SafeProcess(() =>
                //    {
                //        string dbfFileName = Path.ChangeExtension(shapeFileFeatureLayer.ShapePathFileName, ".dbf");

                //        if (File.Exists(dbfFileName))
                //        {
                //            using (GeoDbf geoDbf = new GeoDbf(dbfFileName, DbfReadWriteMode.ReadOnly))
                //            {
                //                geoDbf.Open();
                //                for (int i = 1; i <= geoDbf.RecordCount; i++)
                //                {
                //                    string fieldValue = geoDbf.ReadFieldAsString(i, ColumnName);
                //                    double number = double.NaN;
                //                    Double.TryParse(fieldValue, out number);
                //                    if (!Double.IsNaN(number))
                //                    {
                //                        tmpMinValue = number < tmpMinValue ? number : tmpMinValue;
                //                        tmpMaxValue = number > tmpMaxValue ? number : tmpMaxValue;
                //                    }
                //                }
                //            }
                //        }
                //    });

                //    maxValue = tmpMaxValue;
                //    minValue = tmpMinValue;
                //}
                //else
                {
                    Collection<Feature> features = new Collection<Feature>();
                    bool needReturn = false;
                    requiredValues.FeatureLayer.SafeProcess(() =>
                        {
                            if (requiredValues.FeatureLayer.FeatureSource.CanGetCountQuickly())
                            {
                                int count = requiredValues.FeatureLayer.FeatureSource.GetCount();
                                if (count > 50000)
                                {
                                    //MessageBoxResult result = MessageBox.Show(string.Format("{0} contains a large amount of records, it might spend too much time to process. Do you want to continue?", requiredValues.FeatureLayer.Name), "Info", MessageBoxButton.YesNo, MessageBoxImage.Information);
                                    //if (result == MessageBoxResult.No)
                                    //{
                                    //    needReturn = true;
                                    //}
                                }
                            }

                            if (!needReturn)
                                features = requiredValues.FeatureLayer.QueryTools.GetAllFeatures(requiredValues.FeatureLayer.GetDistinctColumnNames());
                        });

                    if (needReturn) return;

                    foreach (Feature feature in features)
                    {
                        double columnValue = 0;
                        try
                        {
                            if (feature.ColumnValues.ContainsKey(ColumnName))
                            {
                                columnValue = Convert.ToDouble(feature.ColumnValues[ColumnName]);
                            }
                            //else if (feature.LinkColumnValues.ContainsKey(ColumnName))
                            //{
                            //    //TODO: we need to figure out how to choose the columnValue when there are multiple link column values.
                            //    Collection<LinkColumnValue> values = feature.LinkColumnValues[ColumnName];
                            //    LinkColumnValue firstValue = values.FirstOrDefault();
                            //    if (firstValue != null)
                            //    {
                            //        columnValue = Convert.ToDouble(firstValue.Value);
                            //    }
                            //}
                        }
                        catch
                        { }

                        minValue = columnValue < minValue ? columnValue : minValue;
                        maxValue = columnValue > maxValue ? columnValue : maxValue;
                    }
                }

                window.LowValue = minValue;
                window.HighValue = maxValue;
            }

            if (minValue == maxValue)
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ClassBreakStyleViewModelCanntbrokeMessage"), "Alert", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
            else if (window.ShowDialog().GetValueOrDefault())
            {
                message.Execute(window.ViewModel);
                RaisePropertyChanged("ClassBreakItems");
            }
        }

        private void RollbackPreviousColumnName(string originalColumnName)
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.BeginInvoke(tmpObject =>
                {
                    actualClassBreakStyle.ColumnName = (string)tmpObject;
                    RaisePropertyChanged("ColumnName");
                }, originalColumnName, DispatcherPriority.Normal);
            }
        }

        private bool IsValidColumn(string value)
        {
            bool isInvalid = false;
            if (!string.IsNullOrEmpty(value) && !Validate())
            {
                isInvalid = true;
                var message = new DialogMessage(GisEditor.LanguageManager.GetStringResource("ClassBreakStyleViewModelNotContainMessage"), null)
                {
                    Caption = "Warning",
                    Button = MessageBoxButton.OK,
                    Icon = MessageBoxImage.Warning
                };
                ShowMessageBox(message);
                actualClassBreakStyle.ColumnName = null;
            }

            return !isInvalid;
        }

        private bool Validate()
        {
            bool isColumnValid = false;
            RequiredValues.FeatureLayer.SafeProcess(() =>
            {
                var plugin = GisEditor.LayerManager.GetLayerPlugins(RequiredValues.FeatureLayer.GetType()).OfType<FeatureLayerPlugin>().FirstOrDefault();

                if (plugin != null)
                {
                    var selectedColumn = plugin.GetIntermediateColumns(RequiredValues.FeatureLayer.FeatureSource).FirstOrDefault(tmpColumn
                        => tmpColumn.ColumnName.Equals(ColumnName, StringComparison.OrdinalIgnoreCase));

                    if (selectedColumn != null)
                    {
                        if (selectedColumn.IntermediateColumnType == IntermediateColumnType.String
                            && GetValidatedTypes().Contains(RequiredValues.FeatureLayer.GetType()))
                        {
                            isColumnValid = true;
                        }
                        else if (selectedColumn.IntermediateColumnType == IntermediateColumnType.Double ||
                            selectedColumn.IntermediateColumnType == IntermediateColumnType.Integer)
                        {
                            isColumnValid = true;
                        }
                    }
                }

                if (!isColumnValid)
                {
                    var selectedColumn = RequiredValues.FeatureLayer.QueryTools.GetColumns().FirstOrDefault(tmpColumn
                        => tmpColumn.ColumnName.Equals(ColumnName, StringComparison.OrdinalIgnoreCase));

                    if (selectedColumn != null)
                    {
                        string[] numericColumnTypes = new string[] { DbfColumnType.Float.ToString(), 
                                                                     DbfColumnType.Numeric.ToString(),
                                                                     "Int"};

                        if (selectedColumn.TypeName.Equals(DbfColumnType.Character.ToString(), StringComparison.OrdinalIgnoreCase)
                            && GetValidatedTypes().Contains(RequiredValues.FeatureLayer.GetType()))
                        {
                            isColumnValid = true;
                        }
                        else if (numericColumnTypes.Any(n => selectedColumn.TypeName.Equals(n, StringComparison.OrdinalIgnoreCase)))
                        {
                            isColumnValid = true;
                        }
                    }
                }
            });
            return isColumnValid;
        }

        private IEnumerable<Type> GetValidatedTypes()
        {
            yield return typeof(GridFeatureLayer);
            yield return typeof(CsvFeatureLayer);
        }

        private ClassBreakItem GetClassBreakItem(ClassBreak classBreak)
        {
            var classBreakItem = new ClassBreakItem
            {
                Image = StyleHelper.GetImageFromStyle(classBreak.CustomStyles.LastOrDefault()),
                StartingValue = classBreak.Value.ToString(),
                ClassBreak = classBreak
            };

            classBreakItem.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals("StartingValue"))
                {
                    RaisePropertyChanged(e.PropertyName);
                }
            };

            return classBreakItem;
        }

        private StylePlugin GetDefaultAvailableStylePlugin()
        {
            var defaultStyleCategories = RequiredValues.AvailableStyleCategories ^ StyleCategories.Label ^ StyleCategories.Composite;
            var defaultStylePlugin = GisEditor.StyleManager.GetDefaultStylePlugin(RequiredValues.AvailableStyleCategories);
            if (defaultStylePlugin == null)
            {
                defaultStyleCategories = RequiredValues.AvailableStyleCategories ^ StyleCategories.Composite;
                defaultStylePlugin = GisEditor.StyleManager.GetDefaultStylePlugin(RequiredValues.AvailableStyleCategories);
            }

            if (defaultStylePlugin == null)
            {
                defaultStyleCategories = RequiredValues.AvailableStyleCategories;
                defaultStylePlugin = GisEditor.StyleManager.GetDefaultStylePlugin(RequiredValues.AvailableStyleCategories);
            }

            if (defaultStylePlugin == null)
            {
                defaultStylePlugin = new AdvancedAreaStylePlugin();
            }
            return defaultStylePlugin;
        }

        private MessageBoxResult ShowMessageBox(DialogMessage message)
        {
            return MessageBox.Show(message.Content, message.Caption, message.Button, message.Icon);
        }

        private static string GetClassBreakItemStyleName(string columnName, double matchingValue)
        {
            return string.Format(CultureInfo.InvariantCulture, classBreakStyleItemNameFormat, columnName, matchingValue);
        }
    }
}