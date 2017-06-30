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
using System.Windows;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;
using Style = ThinkGeo.MapSuite.Styles.Style;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class FilterStyleViewModel : StyleViewModel
    {
        private static readonly string defaultName = "Filter Style";
        private static readonly string format = "{0} ({1})";

        private bool canChangeNameAutomatically;
        private StylePlugin stylePlugin;
        private FilterStyle actualFilterStyle;
        private RelayCommand viewDataCommand;
        private RelayCommand testScriptCommand;
        private RelayCommand addConditionCommand;
        private RelayCommand<FilterConditionViewModel> editConditionCommand;
        private RelayCommand<FilterConditionViewModel> removeConditionCommand;
        private ObservedCommand showResultsCommand;
        private StyleBuilderArguments requiredValues;
        private ScriptFilterCondition scriptFilterCondition;
        private Dictionary<string, IntermediateColumnType> columnNameTypes;
        private FilterConditionViewModel selectedFilterCondtion;
        private ObservableCollection<FilterConditionViewModel> filterConditions;
        private FilterStyleMatchTypeToStringConverter filterStyleMatchTypeToStringConverter;
        private Dictionary<StylePlugin, Style> availableStylePlugins;
        private string filterScriptDescription;

        private RelayCommand toTopCommand;
        private RelayCommand toBottomCommand;
        private RelayCommand moveUpCommand;
        private RelayCommand moveDownCommand;

        public FilterStyleViewModel(FilterStyle filterStyle, StyleBuilderArguments requiredValues)
            : base(filterStyle)
        {
            filterStyleMatchTypeToStringConverter = new FilterStyleMatchTypeToStringConverter();
            HelpKey = "FilterStyleHelp";
            this.actualFilterStyle = filterStyle;
            this.requiredValues = requiredValues;
            this.filterConditions = new ObservableCollection<FilterConditionViewModel>();
            this.filterConditions.CollectionChanged += FilterConditions_CollectionChanged;
            this.availableStylePlugins = new Dictionary<StylePlugin, Style>();
            this.InitializeConditions(filterStyle.Conditions);
            this.InitializeAvailablePlugins(requiredValues.FeatureLayer);
            this.InitializeCommands();
            FilterConditions_CollectionChanged(null, null);
            InitializeColumnNameTypes(requiredValues);
        }

        private void InitializeColumnNameTypes(StyleBuilderArguments requiredValues)
        {
            columnNameTypes = new Dictionary<string, IntermediateColumnType>();
            if (RequiredValues.FeatureLayer != null)
            {
                FeatureLayerPlugin resultLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(RequiredValues.FeatureLayer.GetType()).LastOrDefault() as FeatureLayerPlugin;

                if (resultLayerPlugin != null)
                {
                    Collection<IntermediateColumn> columns = resultLayerPlugin.GetIntermediateColumns(requiredValues.FeatureLayer.FeatureSource);
                    foreach (var item in columns)
                    {
                        columnNameTypes[item.ColumnName] = item.IntermediateColumnType;
                    }
                }
            }
        }

        private void FilterConditions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            string newName = string.Empty;
            if (filterConditions.Count == 0)
            {
                newName = defaultName;
            }
            else
            {
                newName = string.Format(CultureInfo.InvariantCulture, format, defaultName, filterStyleMatchTypeToStringConverter.Convert(filterConditions[0], null, null, null).ToString());
            }

            if (canChangeNameAutomatically)
            {
                Name = newName;
            }

            if (Name.Equals(newName) || Name.Equals(defaultName))
            {
                canChangeNameAutomatically = true;
            }
            else
            {
                canChangeNameAutomatically = false;
            }

            RefreshFilterScriptDescription();
        }

        private void RefreshFilterScriptDescription()
        {
            FilterScriptDescription = string.Empty;
            foreach (var condition in filterConditions)
            {
                if (condition.FilterCondition.IsLeftBracket) FilterScriptDescription += "(";
                FilterScriptDescription += condition.FilterCondition.Name;
                if (condition.FilterCondition.IsRightBracket) FilterScriptDescription += ")";

                if (condition.FilterCondition.Logical)
                    FilterScriptDescription += " AND ";
                else FilterScriptDescription += " OR ";
            }

            FilterScriptDescription = FilterScriptDescription.TrimEnd(" OR".ToCharArray());
            FilterScriptDescription = FilterScriptDescription.TrimEnd(" AND".ToCharArray());
        }

        public StyleBuilderArguments RequiredValues
        {
            get { return requiredValues; }
        }

        public StylePlugin StylePlugin
        {
            get { return stylePlugin; }
            set
            {
                if (stylePlugin != value)
                {
                    stylePlugin = value;
                    RaisePropertyChanged("StylePlugin");
                }
            }
        }

        public string FilterScriptDescription
        {
            get { return filterScriptDescription; }
            set
            {
                filterScriptDescription = value;
                RaisePropertyChanged("FilterScriptDescription");
            }
        }

        public Dictionary<StylePlugin, Style> AvailableStylePlugins
        {
            get { return availableStylePlugins; }
        }

        public ObservableCollection<FilterConditionViewModel> FilterConditions
        {
            get { return filterConditions; }
        }

        public RelayCommand AddConditionCommand
        {
            get { return addConditionCommand; }
        }

        public ObservedCommand ShowResultsCommand
        {
            get { return showResultsCommand; }
        }

        public RelayCommand<FilterConditionViewModel> EditConditionCommand
        {
            get { return editConditionCommand; }
        }

        public RelayCommand<FilterConditionViewModel> RemoveConditionCommand
        {
            get { return removeConditionCommand; }
        }

        public RelayCommand TestScriptCommand
        {
            get { return testScriptCommand; }
        }

        public RelayCommand ViewDataCommand
        {
            get { return viewDataCommand; }
        }

        public FilterConditionViewModel SelectedFilterCondtion
        {
            get { return selectedFilterCondtion; }
            set
            {
                selectedFilterCondtion = value;
                RaisePropertyChanged(() => CanMoveUp);
                RaisePropertyChanged(() => CanMoveDown);
                RaisePropertyChanged(() => SelectedFilterCondtion);
            }
        }

        public bool CanMoveUp
        {
            get
            {
                return SelectedFilterCondtion != null && FilterConditions.Count > 0 && FilterConditions.IndexOf(SelectedFilterCondtion) != 0;
            }
        }

        public bool CanMoveDown
        {
            get
            {
                return SelectedFilterCondtion != null && FilterConditions.Count > 0 && FilterConditions.IndexOf(SelectedFilterCondtion) != (FilterConditions.Count - 1);
            }
        }

        public RelayCommand ToTopCommand
        {
            get
            {
                if (toTopCommand == null)
                {
                    toTopCommand = new RelayCommand(() =>
                    {
                        if (SelectedFilterCondtion != null && FilterConditions.Contains(SelectedFilterCondtion))
                        {
                            var tmpSelectedItem = SelectedFilterCondtion;
                            FilterConditions.Remove(tmpSelectedItem);
                            FilterConditions.Insert(0, tmpSelectedItem);
                            SelectedFilterCondtion = tmpSelectedItem;
                            SyncConditions();
                            RaisePropertyChanged("FilterConditions");
                        }
                    });
                }
                return toTopCommand;
            }
        }

        public RelayCommand ToBottomCommand
        {
            get
            {
                if (toBottomCommand == null)
                {
                    toBottomCommand = new RelayCommand(() =>
                    {
                        if (SelectedFilterCondtion != null && FilterConditions.Contains(SelectedFilterCondtion))
                        {
                            var tmpSelectedItem = SelectedFilterCondtion;
                            FilterConditions.Remove(tmpSelectedItem);
                            FilterConditions.Add(tmpSelectedItem);
                            SelectedFilterCondtion = tmpSelectedItem;
                            SyncConditions();
                            RaisePropertyChanged("FilterConditions");
                        }
                    });
                }
                return toBottomCommand;
            }
        }

        public RelayCommand MoveUpCommand
        {
            get
            {
                if (moveUpCommand == null)
                {
                    moveUpCommand = new RelayCommand(() =>
                    {
                        if (SelectedFilterCondtion != null && FilterConditions.Contains(SelectedFilterCondtion))
                        {
                            int index = FilterConditions.IndexOf(SelectedFilterCondtion);
                            var tmpSelectedItem = SelectedFilterCondtion;
                            FilterConditions.Remove(tmpSelectedItem);
                            FilterConditions.Insert(index - 1, tmpSelectedItem);
                            SelectedFilterCondtion = tmpSelectedItem;
                            SyncConditions();
                            RaisePropertyChanged("FilterConditions");
                        }
                    });
                }
                return moveUpCommand;
            }
        }

        public RelayCommand MoveDownCommand
        {
            get
            {
                if (moveDownCommand == null)
                {
                    moveDownCommand = new RelayCommand(() =>
                    {
                        if (SelectedFilterCondtion != null && FilterConditions.Contains(SelectedFilterCondtion))
                        {
                            int index = FilterConditions.IndexOf(SelectedFilterCondtion);
                            var tmpSelectedItem = SelectedFilterCondtion;
                            FilterConditions.Remove(tmpSelectedItem);
                            FilterConditions.Insert(index + 1, tmpSelectedItem);
                            SelectedFilterCondtion = tmpSelectedItem;
                            SyncConditions();
                            RaisePropertyChanged("FilterConditions");
                        }
                    });
                }
                return moveDownCommand;
            }
        }

        public string FilterScript
        {
            get { return scriptFilterCondition.Expression; }
            set
            {
                scriptFilterCondition.Expression = value;
                RaisePropertyChanged("FilterScript");
            }
        }

        public FilterStyleScriptType FilterStyleScriptType
        {
            get { return scriptFilterCondition.ScriptType; }
            set
            {
                scriptFilterCondition.ScriptType = value;
                FilterScript = string.Empty;
                RaisePropertyChanged("FilterStyleScriptType");
            }
        }

        private void InitializeConditions(Collection<FilterCondition> conditions)
        {
            foreach (var condition in conditions.Where(c => !(c is ScriptFilterCondition)))
            {
                FilterConditionViewModel itemViewModel = new FilterConditionViewModel(requiredValues, condition);
                itemViewModel.PropertyChanged += ItemViewModel_PropertyChanged;
                FilterConditions.Add(itemViewModel);
            }

            scriptFilterCondition = conditions.OfType<ScriptFilterCondition>().FirstOrDefault();
            if (scriptFilterCondition == null)
            {
                scriptFilterCondition = new ScriptFilterCondition();
                scriptFilterCondition.ScriptType = FilterStyleScriptType;
                conditions.Add(scriptFilterCondition);
            }
        }

        private void InitializeAvailablePlugins(FeatureLayer featureLayer)
        {
            var shapeType = StylePluginHelper.GetWellKnownType(featureLayer);
            switch (shapeType)
            {
                case SimpleShapeType.Point:
                    InitializeAvailableStyleProviders(StyleCategories.Point);
                    break;

                case SimpleShapeType.Line:
                    InitializeAvailableStyleProviders(StyleCategories.Line);
                    break;

                case SimpleShapeType.Area:
                    InitializeAvailableStyleProviders(StyleCategories.Area);
                    break;

                default:
                    break;
            }

            bool isAddingText = ActualObject is TextFilterStyle;
            if (isAddingText) InitializeAvailableStyleProviders(StyleCategories.Label, isAddingText);
            RemoveIgnoredStylePlugins();
        }

        private void InitializeAvailableStyleProviders(StyleCategories supportStyleType, bool reset = true)
        {
            if (reset) availableStylePlugins.Clear();
            GisEditor.StyleManager.GetStylePlugins(supportStyleType)
                .Where(tmpProvider => !tmpProvider.StyleCategories.ToString().Contains(','))
                .ForEach(tmpProvider =>
                {
                    var defaultStyle = tmpProvider.GetDefaultStyle();
                    availableStylePlugins.Add(tmpProvider, defaultStyle.CloneDeep());
                });
        }

        private void InitializeCommands()
        {
            addConditionCommand = new RelayCommand(() =>
            {
                FilterConditionWindow newFilterConditionWindow = new FilterConditionWindow();
                FilterConditionViewModel itemViewModel = new FilterConditionViewModel(RequiredValues);
                itemViewModel.PropertyChanged += ItemViewModel_PropertyChanged;
                newFilterConditionWindow.DataContext = itemViewModel;
                foreach (var item in columnNameTypes)
                {
                    newFilterConditionWindow.ViewModel.ColumnNameTypes[item.Key] = item.Value;
                }

                newFilterConditionWindow.ViewModel.SelectedColumnName = newFilterConditionWindow.ViewModel.ColumnNames.FirstOrDefault();
                if (newFilterConditionWindow.ShowDialog().GetValueOrDefault())
                {
                    var stylePlugin = GisEditor.StyleManager.GetDefaultStylePlugin(requiredValues.AvailableStyleCategories);

                    filterConditions.Add(newFilterConditionWindow.ViewModel);
                    SyncConditions();
                    RaisePropertyChanged("FilterConditions");
                }
            });

            editConditionCommand = new RelayCommand<FilterConditionViewModel>(v =>
            {
                var clonedViewModel = v.CloneDeep();
                clonedViewModel.ColumnNameTypes.Clear();

                foreach (var item in columnNameTypes)
                {
                    clonedViewModel.ColumnNameTypes[item.Key] = item.Value;
                }

                FilterConditionWindow newFilterConditionWindow = new FilterConditionWindow(clonedViewModel);

                FeatureLayerPlugin resultLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(RequiredValues.FeatureLayer.GetType()).LastOrDefault() as FeatureLayerPlugin;

                if (resultLayerPlugin != null)
                {
                    Collection<IntermediateColumn> columns = resultLayerPlugin.GetIntermediateColumns(requiredValues.FeatureLayer.FeatureSource);
                    foreach (var item in columns)
                    {
                        newFilterConditionWindow.ViewModel.ColumnNameTypes[item.ColumnName] = item.IntermediateColumnType;
                    }
                }

                if (newFilterConditionWindow.ShowDialog().Value)
                {
                    var index = FilterConditions.IndexOf(v);
                    if (index != -1)
                    {
                        v = clonedViewModel;
                        FilterConditions.RemoveAt(index);
                        FilterConditions.Insert(index, v);
                    }
                }

                SyncConditions();
            });

            removeConditionCommand = new RelayCommand<FilterConditionViewModel>(v =>
            {
                if (FilterConditions.Contains(v))
                {
                    var messageBoxResult = System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("FilterStyleViewModelConditionRemovedMessage")
                        , "Alert"
                        , System.Windows.Forms.MessageBoxButtons.YesNo);

                    if (messageBoxResult == System.Windows.Forms.DialogResult.Yes)
                    {
                        FilterConditions.Remove(v);
                        SyncConditions();
                        RaisePropertyChanged("FilterConditions");
                    }
                }
            });

            testScriptCommand = new RelayCommand(() =>
            {
                try
                {
                    var result = actualFilterStyle.GetRequiredColumnNames().All(c => requiredValues.ColumnNames.Contains(c));
                    if (!result) MessageBox.Show(GisEditor.LanguageManager.GetStringResource("FilterStyleUserControlColumnMessage"));
                    else
                    {
                        Collection<Feature> features = new Collection<Feature>();
                        ScriptFilterCondition condition = actualFilterStyle.Conditions.OfType<ScriptFilterCondition>().FirstOrDefault();
                        if (condition != null)
                        {
                            condition.GetMatchingFeatures(features);
                        }
                        MessageBox.Show(GisEditor.LanguageManager.GetStringResource("TestPassMessage"));
                    }
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    MessageBox.Show(ex.Message);
                }
            });

            viewDataCommand = new RelayCommand(() =>
            {
                DataViewerUserControl content = new DataViewerUserControl();
                content.ShowDialog();
            });

            showResultsCommand = new ObservedCommand(() =>
            {
                ShowFilteredData(requiredValues.FeatureLayer, FilterConditions.Select(f => f.FilterCondition), "");
            }, () => filterConditions.Count > 0);
        }

        private void ItemViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RefreshFilterScriptDescription();
        }

        public static void ShowFilteredData(FeatureLayer featureLayer, IEnumerable<FilterCondition> conditions, string title)
        {
            Collection<FeatureLayer> layers = new Collection<FeatureLayer>();
            InMemoryFeatureLayer wrapperFeatureLayer = new InMemoryFeatureLayer();
            wrapperFeatureLayer.Name = featureLayer.Name;
            wrapperFeatureLayer.Open();

            //foreach (var item in featureLayer.FeatureSource.LinkExpressions)
            //{
            //    wrapperFeatureLayer.FeatureSource.LinkExpressions.Add(item);
            //}
            //foreach (var item in featureLayer.FeatureSource.LinkSources)
            //{
            //    wrapperFeatureLayer.FeatureSource.LinkSources.Add(item);
            //}
            wrapperFeatureLayer.Columns.Clear();

            featureLayer.SafeProcess(() =>
            {
                Collection<FeatureSourceColumn> columns = featureLayer.FeatureSource.GetColumns();

                foreach (var column in columns)
                {
                    wrapperFeatureLayer.Columns.Add(column);
                }

                Collection<Feature> resultFeatures = featureLayer.QueryTools.GetAllFeatures(ReturningColumnsType.AllColumns);
                resultFeatures = GetFeaturesByConditions(conditions, resultFeatures);

                foreach (Feature feature in resultFeatures)
                {
                    wrapperFeatureLayer.InternalFeatures.Add(feature);
                }
            });

            wrapperFeatureLayer.ZoomLevelSet = featureLayer.ZoomLevelSet;
            layers.Add(wrapperFeatureLayer);

            DataViewerUserControl content = new DataViewerUserControl(wrapperFeatureLayer, layers);
            content.IsHighlightFeatureEnabled = false;
            content.Title = title;
            content.ShowDock();
        }

        private static Collection<Feature> GetFeaturesByConditions(IEnumerable<FilterCondition> conditions, Collection<Feature> features)
        {
            Collection<Feature> matchedFeatures = new Collection<Feature>();
            Collection<Feature> currentMatchedFeatures = new Collection<Feature>();
            bool currentConnectLogical = true;

            bool connectLogical = true;
            bool isInBracket = false;

            var firstCondition = conditions.FirstOrDefault();
            if (firstCondition != null)
            {
                if (!string.IsNullOrEmpty(firstCondition.Expression))
                {
                    isInBracket = firstCondition.IsLeftBracket;

                    currentMatchedFeatures = firstCondition.GetMatchingFeatures(features);
                    matchedFeatures = new Collection<Feature>(currentMatchedFeatures.ToList());

                    isInBracket = isInBracket && !firstCondition.IsRightBracket;
                    connectLogical = firstCondition.Logical;
                    currentConnectLogical = connectLogical;
                }
            }

            foreach (var condition in conditions.Skip(1))
            {
                isInBracket = isInBracket || condition.IsLeftBracket;

                if (!string.IsNullOrEmpty(condition.Expression))
                {
                    if (!connectLogical)
                    {
                        if (isInBracket)
                        {
                            foreach (var feature in condition.GetMatchingFeatures(features))
                            {
                                currentMatchedFeatures.Add(feature);
                            }

                        }
                        else
                        {
                            foreach (var feature in condition.GetMatchingFeatures(features))
                            {
                                matchedFeatures.Add(feature);
                            }
                        }
                    }
                    else
                    {
                        if (isInBracket)
                        {
                            var containFeautures = condition.GetMatchingFeatures(features);

                            for (int i = 0; i < currentMatchedFeatures.Count; i++)
                            {
                                if (!containFeautures.Contains(currentMatchedFeatures[i]))
                                {
                                    currentMatchedFeatures.Remove(currentMatchedFeatures[i]);
                                    i--;
                                }
                            }
                        }
                        else
                        {
                            var containFeautures = condition.GetMatchingFeatures(features);

                            for (int i = 0; i < matchedFeatures.Count; i++)
                            {
                                if (!containFeautures.Contains(matchedFeatures[i]))
                                {
                                    matchedFeatures.Remove(matchedFeatures[i]);
                                    i--;
                                }
                            }
                        }
                    }

                    if (isInBracket)
                    {
                        isInBracket = !condition.IsRightBracket;
                        if (!isInBracket)
                        {
                            if (!currentConnectLogical)
                            {
                                if (currentMatchedFeatures != null)
                                {
                                    foreach (var feature in currentMatchedFeatures)
                                    {
                                        if (!matchedFeatures.Contains(feature))
                                        {
                                            matchedFeatures.Add(feature);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (currentMatchedFeatures != null)
                                {
                                    for (int i = 0; i < matchedFeatures.Count; i++)
                                    {
                                        if (!currentMatchedFeatures.Contains(matchedFeatures[i]))
                                        {
                                            matchedFeatures.Remove(matchedFeatures[i]);
                                            i--;
                                        }
                                    }
                                }
                            }

                            currentMatchedFeatures.ToList().Clear();
                            currentConnectLogical = true;
                        }
                        else
                            currentConnectLogical = connectLogical;
                    }
                    else
                    {
                        isInBracket = condition.IsLeftBracket;
                        if (isInBracket)
                            currentConnectLogical = connectLogical;
                    }
                }
                connectLogical = condition.Logical;
            }

            return matchedFeatures;
        }

        private void SyncConditions()
        {
            ScriptFilterCondition scriptFilterCondition = actualFilterStyle.Conditions.OfType<ScriptFilterCondition>().FirstOrDefault();
            actualFilterStyle.Conditions.Clear();
            actualFilterStyle.Filters.Clear();
            foreach (var condition in FilterConditions)
            {
                actualFilterStyle.Conditions.Add(condition.FilterCondition);
                string filter = GetFilterString(condition);
                //TODO: why do we need to add filter to style?
                //actualFilterStyle.Filters.Add(filter);
            }
            actualFilterStyle.Conditions.Add(scriptFilterCondition);
        }

        private string GetFilterString(FilterConditionViewModel filterCondition)
        {
            string filter = string.Empty;

            switch (filterCondition.MatchType.Key)
            {
                case FilterConditionType.Equal:
                    filter = string.Format(CultureInfo.InvariantCulture, "[{0}].ToString().ToLowerInvariant().Equals(\"{1}\")", filterCondition.FilterCondition.ColumnName, filterCondition.MatchExpression.ToLowerInvariant());
                    break;
                case FilterConditionType.Contains:
                    filter = string.Format(CultureInfo.InvariantCulture, "[{0}].ToString().ToLowerInvariant().Contains(\"{1}\")", filterCondition.FilterCondition.ColumnName, filterCondition.MatchExpression.ToLowerInvariant());
                    break;
                case FilterConditionType.StartsWith:
                    filter = string.Format(CultureInfo.InvariantCulture, "[{0}].ToString().ToLowerInvariant().StartsWith(\"{1}\")", filterCondition.FilterCondition.ColumnName, filterCondition.MatchExpression.ToLowerInvariant());
                    break;
                case FilterConditionType.EndsWith:
                    filter = string.Format(CultureInfo.InvariantCulture, "[{0}].ToString().ToLowerInvariant().EndsWith(\"{1}\")", filterCondition.FilterCondition.ColumnName, filterCondition.MatchExpression.ToLowerInvariant());
                    break;
                case FilterConditionType.DoesNotEqual:
                    filter = string.Format(CultureInfo.InvariantCulture, "![{0}].ToString().ToLowerInvariant().Equals(\"{1}\")", filterCondition.FilterCondition.ColumnName, filterCondition.MatchExpression.ToLowerInvariant());
                    break;
                case FilterConditionType.DoesNotContain:
                    filter = string.Format(CultureInfo.InvariantCulture, "![{0}].ToString().ToLowerInvariant().Contains(\"{1}\")", filterCondition.FilterCondition.ColumnName, filterCondition.MatchExpression.ToLowerInvariant());
                    break;
                case FilterConditionType.GreaterThan:
                    filter = string.Format(CultureInfo.InvariantCulture, "double.Parse([{0}].ToString()) > (double){1}", filterCondition.FilterCondition.ColumnName, filterCondition.MatchExpression);
                    break;
                case FilterConditionType.GreaterThanOrEqualTo:
                    filter = string.Format(CultureInfo.InvariantCulture, "double.Parse([{0}].ToString()) >= (double){1}", filterCondition.FilterCondition.ColumnName, filterCondition.MatchExpression);
                    break;
                case FilterConditionType.LessThan:
                    filter = string.Format(CultureInfo.InvariantCulture, "double.Parse([{0}].ToString()) < (double){1}", filterCondition.FilterCondition.ColumnName, filterCondition.MatchExpression);
                    break;
                case FilterConditionType.LessThanOrEqualTo:
                    filter = string.Format(CultureInfo.InvariantCulture, "double.Parse([{0}].ToString()) <= (double){1}", filterCondition.FilterCondition.ColumnName, filterCondition.MatchExpression);
                    break;
                case FilterConditionType.Custom:
                    break;
                case FilterConditionType.DynamicLanguage:
                    break;
                case FilterConditionType.DateRange:
                    break;
                case FilterConditionType.NumericRange:
                    filter = string.Format(CultureInfo.InvariantCulture, "double.Parse([{0}].ToString()) <= (double){1} && double.Parse([{0}].ToString()) >= (double){2}", filterCondition.FilterCondition.ColumnName, filterCondition.ToNumberic, filterCondition.FromNumberic);
                    break;
                case FilterConditionType.IsEmpty:
                    filter = string.Format(CultureInfo.InvariantCulture, "String.IsEmpty({0})", filterCondition.FilterCondition.ColumnName, filterCondition.MatchExpression);
                    break;
                case FilterConditionType.IsNotEmpty:
                    filter = string.Format(CultureInfo.InvariantCulture, "!String.IsEmpty({0})", filterCondition.FilterCondition.ColumnName, filterCondition.MatchExpression);
                    break;
                case FilterConditionType.ValidFeature:
                    break;
                default:
                    break;
            }

            return filter;
        }

        private void RemoveIgnoredStylePlugins()
        {
            var ignorePlugins = AvailableStylePlugins.Where(p => StyleHelper.GetCompositedStylePluginTypes()
                .Any(t => t.Equals(p.Key.GetType()))).ToArray();

            foreach (var ignorePlugin in ignorePlugins)
            {
                AvailableStylePlugins.Remove(ignorePlugin.Key);
            }
        }
    }
}