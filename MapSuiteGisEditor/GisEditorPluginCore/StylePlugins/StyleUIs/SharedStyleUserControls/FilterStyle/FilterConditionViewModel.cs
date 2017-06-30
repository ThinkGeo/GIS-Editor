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
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class FilterConditionViewModel : ViewModelBase
    {
        private static readonly string doubleTypeEqualRegex = @"(\\.0+|0+|$)";

        private string matchExpression;
        private string fromDate;
        private string toDate;
        private bool isIgnoreCase;
        private bool isColumnNamesEnabled;
        private bool matchExpressionIsEnabled;
        private bool validStatus;
        private Visibility basedOnVisibility;
        private FilterMode filterMode;
        private double fromNumberic;
        private double toNumberic;
        private FilterCondition condition;
        private RelayCommand viewDataCommand;
        private Dictionary<string, string> columnNames;
        private KeyValuePair<FilterConditionType, Tuple<string, string>> matchType;
        private Dictionary<FilterConditionType, Tuple<string, string>> filterConditionTemplates;
        private Dictionary<string, IntermediateColumnType> columnNameTypes;
        private Visibility matchDateVisible;
        private Visibility matchValidFeatureVisible;
        private Visibility matchValueVisible;
        private Visibility matchNumbericVisible;
        private Visibility areaUnitsVisibility;
        private Visibility columnNamesVisibility;
        private Collection<AreaUnit> areaUnits;
        private AreaUnit selectedAreaUnit;

        private StyleBuilderArguments requiredValues;

        public FilterConditionViewModel(StyleBuilderArguments requiredValues)
            : this(requiredValues, null)
        { }

        public FilterConditionViewModel(StyleBuilderArguments requiredValues, FilterCondition condition)
        {
            columnNameTypes = new Dictionary<string, IntermediateColumnType>();

            basedOnVisibility = Visibility.Collapsed;
            if (requiredValues.FeatureLayer != null)
            {
                var layerPlugin = GisEditor.LayerManager.GetLayerPlugins(requiredValues.FeatureLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
                if (layerPlugin != null)
                {
                    var shapeType = layerPlugin.GetFeatureSimpleShapeType(requiredValues.FeatureLayer);
                    basedOnVisibility = shapeType == SimpleShapeType.Area ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            areaUnits = new Collection<AreaUnit>(Enum.GetValues(typeof(AreaUnit)).OfType<AreaUnit>().ToList());
            AreaFilterCondition calculatedAreaFilterCondition = condition as AreaFilterCondition;
            if (calculatedAreaFilterCondition != null)
            {
                columnNamesVisibility = Visibility.Collapsed;
                areaUnitsVisibility = Visibility.Visible;
                this.filterMode = FilterMode.Area;
                this.selectedAreaUnit = calculatedAreaFilterCondition.AreaUnit;
            }
            else
            {
                columnNamesVisibility = Visibility.Visible;
                areaUnitsVisibility = Visibility.Collapsed;
            }

            if (condition == null)
            {
                condition = new FilterCondition();
                condition.Expression = ".*.*";
                condition.Logical = true;
            }
            this.validStatus = true;
            this.matchExpressionIsEnabled = true;
            this.isIgnoreCase = condition.RegexOptions == System.Text.RegularExpressions.RegexOptions.IgnoreCase;
            this.condition = condition;
            this.columnNames = new Dictionary<string, string>();
            this.requiredValues = requiredValues;
            foreach (var item in requiredValues.ColumnNames)
            {
                string alias = requiredValues.FeatureLayer.FeatureSource.GetColumnAlias(item);
                this.columnNames[item] = alias;
            }
            if (!string.IsNullOrEmpty(condition.ColumnName) && columnNames.Values.Contains(condition.ColumnName, StringComparer.OrdinalIgnoreCase))
            {
                condition.ColumnName = columnNames.FirstOrDefault(c => c.Value.Equals(condition.ColumnName, StringComparison.InvariantCultureIgnoreCase)).Key;
            }
            this.filterConditionTemplates = FilterHelper.GetFilterConditionTemplates(filterMode);
            this.InitializeCommands();
            this.InitializeProperties();
        }

        public bool ValidStatus
        {
            get { return validStatus; }
            set
            {
                validStatus = value;
                RaisePropertyChanged(() => ValidStatus);
            }
        }

        public bool IsValid
        {
            get { return GetIsValid(); }
        }

        public FilterMode FilterMode
        {
            get { return filterMode; }
            set
            {
                filterMode = value;
                if (filterMode == FilterMode.Attributes)
                {
                    IsColumnNamesEnabled = true;
                    ColumnNamesVisibility = Visibility.Visible;
                    AreaUnitsVisibility = Visibility.Collapsed;
                }
                else
                {
                    IsColumnNamesEnabled = false;
                    AreaUnitsVisibility = Visibility.Visible;
                    ColumnNamesVisibility = Visibility.Collapsed;
                }
                this.filterConditionTemplates = FilterHelper.GetFilterConditionTemplates(filterMode);
                if (!filterConditionTemplates.ContainsKey(matchType.Key))
                {
                    MatchType = filterConditionTemplates.FirstOrDefault();
                }

                RaisePropertyChanged(() => FilterConditionTemplates);
            }
        }

        public Dictionary<FilterConditionType, Tuple<string, string>> FilterConditionTemplates
        {
            get { return filterConditionTemplates; }
        }

        public Dictionary<string, IntermediateColumnType> ColumnNameTypes
        {
            get { return columnNameTypes; }
        }

        public RelayCommand ViewDataCommand
        {
            get { return viewDataCommand; }
        }

        public string ColumnName
        {
            get
            {
                return SelectedColumnName.Key;
            }
        }

        public Visibility BasedOnVisibility
        {
            get { return basedOnVisibility; }
            set
            {
                basedOnVisibility = value;
                RaisePropertyChanged(() => BasedOnVisibility);
            }
        }

        public KeyValuePair<string, string> SelectedColumnName
        {
            get { return columnNames.FirstOrDefault(c => c.Key == condition.ColumnName); }
            set
            {
                if (String.IsNullOrEmpty(condition.ColumnName) || !condition.ColumnName.Equals(value.Key, StringComparison.Ordinal))
                {
                    condition.ColumnName = value.Key;
                    RaisePropertyChanged(() => SelectedColumnName);
                    RaisePropertyChanged(() => IsValid);
                }
            }
        }

        public Dictionary<string, string> ColumnNames
        {
            get { return columnNames; }
        }

        public double FromNumberic
        {
            get { return fromNumberic; }
            set
            {
                fromNumberic = value;
                MatchExpression = fromNumberic.ToString() + " to " + toNumberic.ToString();
            }
        }

        public double ToNumberic
        {
            get { return toNumberic; }
            set
            {
                toNumberic = value;
                MatchExpression = fromNumberic.ToString() + " to " + toNumberic.ToString();
            }
        }

        public string FromDate
        {
            get { return fromDate; }
            set
            {
                fromDate = value;
                var dateCondition = condition as DateRangeFilterCondition;
                if (dateCondition != null)
                {
                    dateCondition.FromDate = DateTime.Parse(fromDate);
                }
            }
        }

        public string ToDate
        {
            get { return toDate; }
            set
            {
                toDate = value;
                var dateCondition = condition as DateRangeFilterCondition;
                if (dateCondition != null)
                {
                    dateCondition.ToDate = DateTime.Parse(toDate);
                }
            }
        }

        public bool MatchExpressionIsEnabled
        {
            get { return matchExpressionIsEnabled; }
            set
            {
                matchExpressionIsEnabled = value;
                RaisePropertyChanged(() => MatchExpressionIsEnabled);
            }
        }

        public KeyValuePair<FilterConditionType, Tuple<string, string>> MatchType
        {
            get { return matchType; }
            set
            {
                if (!matchType.Equals(value))
                {
                    matchType = value;
                    if (matchType.Key == FilterConditionType.IsEmpty || matchType.Key == FilterConditionType.IsNotEmpty)
                    {
                        MatchExpression = string.Empty;
                        MatchExpressionIsEnabled = false;
                    }
                    else
                    {
                        MatchExpressionIsEnabled = true;
                    }

                    isColumnNamesEnabled = true;
                    if (matchType.Key == FilterConditionType.DateRange)
                    {
                        matchDateVisible = Visibility.Visible;
                        matchValueVisible = Visibility.Collapsed;
                        matchNumbericVisible = Visibility.Collapsed;
                        matchValidFeatureVisible = Visibility.Collapsed;

                        if (!(condition is DateRangeFilterCondition))
                        {
                            string columnName = condition.ColumnName;
                            condition = new DateRangeFilterCondition();
                            condition.ColumnName = columnName;
                        }

                        fromDate = ((DateRangeFilterCondition)condition).FromDate.ToShortDateString();
                        toDate = ((DateRangeFilterCondition)condition).ToDate.ToShortDateString();
                        RaisePropertyChanged(() => FromDate);
                        RaisePropertyChanged(() => ToDate);
                    }
                    else if (matchType.Key == FilterConditionType.NumericRange)
                    {
                        matchDateVisible = Visibility.Collapsed;
                        matchValueVisible = Visibility.Collapsed;
                        matchNumbericVisible = Visibility.Visible;
                        matchValidFeatureVisible = Visibility.Collapsed;
                        string columnName = condition.ColumnName;
                        if (condition is DateRangeFilterCondition)
                        {
                            condition = new FilterCondition();
                            condition.ColumnName = columnName;
                            condition.Expression = "^$";
                        }

                        if (condition.Expression.StartsWith("Number"))
                        {
                            string[] values = condition.Expression.Substring(6).Split(new string[] { "to", "," }, StringSplitOptions.RemoveEmptyEntries);
                            try
                            {
                                fromNumberic = double.Parse(values[0]);
                                toNumberic = double.Parse(values[1]);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(string.Format(CultureInfo.InvariantCulture, "{0} Parameters name:{1}, {2}", ex.Message, values[0], values[1]));
                            }
                        }
                    }
                    else if (matchType.Key == FilterConditionType.ValidFeature)
                    {
                        string columnName = condition.ColumnName;
                        if (!(condition is ValidFeatureFilterCondition))
                        {
                            condition = new ValidFeatureFilterCondition();
                            condition.ColumnName = columnName;
                        }
                        ValidStatus = ((ValidFeatureFilterCondition)condition).ValidationType == FeatureValidationType.Valid;
                        matchValidFeatureVisible = Visibility.Visible;
                        matchDateVisible = Visibility.Collapsed;
                        matchNumbericVisible = Visibility.Collapsed;
                        matchValueVisible = Visibility.Collapsed;
                        isColumnNamesEnabled = false;
                    }
                    else
                    {
                        string columnName = condition.ColumnName;
                        if (condition is DateRangeFilterCondition)
                        {
                            condition = new FilterCondition();
                            condition.ColumnName = columnName;
                            condition.Expression = "^$";
                        }
                        matchDateVisible = Visibility.Collapsed;
                        matchNumbericVisible = Visibility.Collapsed;
                        matchValueVisible = Visibility.Visible;
                        matchValidFeatureVisible = Visibility.Collapsed;
                    }

                    RaisePropertyChanged(() => MatchDateVisible);
                    RaisePropertyChanged(() => MatchValueVisible);
                    RaisePropertyChanged(() => MatchNumbericVisible);
                    RaisePropertyChanged(() => MatchValidFeatureVisible);
                    RaisePropertyChanged(() => MatchType);
                    RaisePropertyChanged(() => IsValid);
                    RaisePropertyChanged(() => IsColumnNamesEnabled);
                }
            }
        }

        public string MatchExpression
        {
            get { return matchExpression; }
            set
            {
                if (string.IsNullOrEmpty(matchExpression) || matchExpression != value)
                {
                    matchExpression = value;
                    RaisePropertyChanged(() => MatchExpression);
                    RaisePropertyChanged(() => IsValid);
                }
            }
        }

        public bool Logical
        {
            get { return condition.Logical; }
            set
            {
                condition.Logical = value;
                RaisePropertyChanged(() => Logical);
            }
        }

        public bool IsLeftBracket
        {
            get { return condition.IsLeftBracket; }
            set
            {
                condition.IsLeftBracket = value;
                RaisePropertyChanged(() => IsLeftBracket);
            }
        }

        public bool IsRightBracket
        {
            get { return condition.IsRightBracket; }
            set
            {
                condition.IsRightBracket = value;
                RaisePropertyChanged(() => IsRightBracket);
            }
        }

        public bool IsIgnoreCase
        {
            get { return isIgnoreCase; }
            set
            {
                isIgnoreCase = value;

                if (isIgnoreCase)
                {
                    condition.RegexOptions = System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                }
                else
                {
                    condition.RegexOptions = System.Text.RegularExpressions.RegexOptions.None;
                }

                RaisePropertyChanged(() => IsIgnoreCase);
            }
        }

        public FilterCondition FilterCondition
        {
            get
            {
                var dateCondition = condition as DateRangeFilterCondition;
                var validCondition = condition as ValidFeatureFilterCondition;
                if (filterMode == FilterMode.Attributes && condition is AreaFilterCondition)
                {
                    var tempColumnName = ColumnName;
                    condition = new FilterCondition();
                    condition.ColumnName = tempColumnName;
                }

                if (dateCondition != null)
                {
                    condition.Name = (string)(new FilterStyleMatchTypeToStringConverter().Convert(this, null, null, CultureInfo.InvariantCulture));
                    condition.Expression = String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}{3}"
                      , MatchType.Value.Item1
                      , dateCondition.FromDate.ToShortDateString()
                      , dateCondition.ToDate.ToShortDateString()
                      , MatchType.Value.Item2);
                }
                else if (validCondition != null)
                {
                    condition.Name = (string)(new FilterStyleMatchTypeToStringConverter().Convert(this, null, null, CultureInfo.InvariantCulture));
                    condition.Expression = String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}"
                                               , MatchType.Value.Item1
                                               , ValidStatus.ToString()
                                               , MatchType.Value.Item2);
                    validCondition.ValidationType = ValidStatus ? FeatureValidationType.Valid : FeatureValidationType.Invalid;
                }
                else
                {
                    condition.Name = (string)(new FilterStyleMatchTypeToStringConverter().Convert(this, null, null, CultureInfo.InvariantCulture));
                    if (matchType.Key == FilterConditionType.NumericRange)
                    {
                        condition.Expression = String.Format(CultureInfo.InvariantCulture, "Number{0},{1}"
                           , fromNumberic.ToString()
                           , toNumberic.ToString());
                    }
                    else
                    {
                        if (ColumnName != null && ColumnNameTypes.ContainsKey(ColumnName) && ColumnNameTypes[ColumnName] == IntermediateColumnType.Double && MatchType.Key == FilterConditionType.Equal)
                        {
                            condition.Expression = String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}"
                                , "^"
                                , MatchExpression
                                , doubleTypeEqualRegex);
                        }
                        else
                        {

                            condition.Expression = String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}"
                                , MatchType.Value.Item1
                                , MatchExpression
                                , MatchType.Value.Item2);
                        }
                    }
                }
                if (filterMode == FilterMode.Area)
                {
                    var calculatedAreaFilterCondition = new AreaFilterCondition();
                    calculatedAreaFilterCondition.AreaUnit = selectedAreaUnit;
                    calculatedAreaFilterCondition.Expression = condition.Expression;
                    calculatedAreaFilterCondition.ColumnName = condition.ColumnName;
                    calculatedAreaFilterCondition.Name = condition.Name;
                    condition = calculatedAreaFilterCondition;
                }
                return condition;
            }
            set
            {
                condition = value;
                RaisePropertyChanged(() => FilterCondition);
            }
        }

        public Visibility MatchNumbericVisible
        {
            get { return matchNumbericVisible; }
        }

        public Visibility MatchValueVisible
        {
            get { return matchValueVisible; }
        }

        public Visibility MatchValidFeatureVisible
        {
            get { return matchValidFeatureVisible; }
        }

        public Visibility MatchDateVisible
        {
            get { return matchDateVisible; }
        }

        public bool IsColumnNamesEnabled
        {
            get { return isColumnNamesEnabled; }
            set
            {
                isColumnNamesEnabled = value;
                RaisePropertyChanged(() => IsColumnNamesEnabled);
            }
        }

        public Visibility AreaUnitsVisibility
        {
            get { return areaUnitsVisibility; }
            set
            {
                areaUnitsVisibility = value;
                RaisePropertyChanged(() => AreaUnitsVisibility);
            }
        }

        public Visibility ColumnNamesVisibility
        {
            get { return columnNamesVisibility; }
            set
            {
                columnNamesVisibility = value;
                RaisePropertyChanged(() => ColumnNamesVisibility);
            }
        }

        public AreaUnit SelectedAreaUnit
        {
            get { return selectedAreaUnit; }
            set
            {
                selectedAreaUnit = value;
                RaisePropertyChanged(() => SelectedAreaUnit);
            }
        }

        public Collection<AreaUnit> AreaUnits
        {
            get { return areaUnits; }
        }

        private void InitializeProperties()
        {
            var dateCondition = condition as DateRangeFilterCondition;

            if (dateCondition != null)
            {
                var matchingTemplate = filterConditionTemplates.FirstOrDefault(t =>
                {
                    if (t.Key == FilterConditionType.DateRange) return true;
                    else return false;
                });

                MatchType = matchingTemplate;

                MatchExpression = matchingTemplate.Value.Item1 + dateCondition.FromDate.ToShortDateString() + "," + dateCondition.ToDate.ToShortDateString() + matchingTemplate.Value.Item2;
            }
            else if (!String.IsNullOrEmpty(condition.Expression))
            {
                if (condition.Expression == "^$")
                {
                    MatchType = filterConditionTemplates.FirstOrDefault(f => f.Key == FilterConditionType.IsEmpty);
                }
                else if (condition.Expression == "^(?!).*?$")
                {
                    MatchType = filterConditionTemplates.FirstOrDefault(f => f.Key == FilterConditionType.IsNotEmpty);
                }
                else
                {
                    MatchType = filterConditionTemplates.FirstOrDefault(t =>
                    {
                        if (!string.IsNullOrEmpty(t.Value.Item1) && !condition.Expression.StartsWith(t.Value.Item1))
                        {
                            return false;
                        }
                        if (!string.IsNullOrEmpty(t.Value.Item2) && !condition.Expression.EndsWith(t.Value.Item2))
                        {
                            return false;
                        }
                        return true;
                    });

                    if (condition.Expression.StartsWith("^") && condition.Expression.EndsWith(doubleTypeEqualRegex))
                    {
                        MatchType = filterConditionTemplates.FirstOrDefault(t => t.Key == FilterConditionType.Equal);
                    }
                }
                if (MatchType.Key == FilterConditionType.NumericRange)
                {
                    condition.Expression = condition.Expression.Replace(",", " to ");
                }

                int matchValueLength = condition.Expression.Length - MatchType.Value.Item1.Length - MatchType.Value.Item2.Length;
                if (condition.Expression.StartsWith("^") && condition.Expression.EndsWith(doubleTypeEqualRegex))
                {
                    matchValueLength = condition.Expression.Length - MatchType.Value.Item1.Length - doubleTypeEqualRegex.Length;
                }
                if (matchValueLength > 0)
                {
                    MatchExpression = condition.Name.Substring(condition.Name.Length - matchValueLength - 1, matchValueLength);
                }
                else MatchExpression = string.Empty;
            }
        }

        private void InitializeCommands()
        {
            viewDataCommand = new RelayCommand(() =>
            {
                DataViewerUserControl content = new DataViewerUserControl();
                content.ShowDialog();
            });
        }

        private bool GetIsValid()
        {
            if (MatchType.Key == FilterConditionType.Equal
                || MatchType.Key == FilterConditionType.DoesNotEqual
                || matchType.Key == FilterConditionType.IsEmpty
                || matchType.Key == FilterConditionType.IsNotEmpty)
            {
                return !String.IsNullOrEmpty(ColumnName);
            }
            else if (matchType.Key == FilterConditionType.ValidFeature)
            {
                return true;
            }
            else
            {
                return !String.IsNullOrEmpty(ColumnName)
                    && (!String.IsNullOrEmpty(MatchExpression) || (matchType.Key == FilterConditionType.DateRange && (!string.IsNullOrEmpty(fromDate) || !string.IsNullOrEmpty(toDate))));
            }
        }

        internal FilterConditionViewModel CloneDeep()
        {
            var clonedViewModel = new FilterConditionViewModel(this.requiredValues);
            clonedViewModel.SelectedColumnName = SelectedColumnName;
            clonedViewModel.FilterCondition = FilterCondition;
            clonedViewModel.FilterMode = FilterMode;
            clonedViewModel.SelectedAreaUnit = SelectedAreaUnit;
            clonedViewModel.IsIgnoreCase = IsIgnoreCase;

            clonedViewModel.MatchExpression = MatchExpression;
            clonedViewModel.MatchType = MatchType;
            clonedViewModel.FromDate = FromDate;
            clonedViewModel.ToDate = ToDate;
            return clonedViewModel;
        }
    }
}