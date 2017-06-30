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


using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    [Obfuscation]
    internal class AdvancedQueryViewModel : ViewModelBase
    {
        private static string removeConditionsMessage = GisEditor.LanguageManager.GetStringResource("AdvancedQueryViewModelRemoveConditionsMessage");
        private QueryMatchMode selectedQueryMatchMode;
        private ObservableCollection<QueryConditionViewModel> queryConditions;
        private AddHighlightFeaturesMode highlightMode;
        private AdvancedQueryModel model;
        private FeatureLayer selectedLayer;
        private IEnumerable<Feature> resultFeatures;
        private bool closeWhenQueryFinished;

        [NonSerialized]
        private RelayCommand findCommand;

        [NonSerialized]
        private RelayCommand cancelCommand;

        [NonSerialized]
        private RelayCommand addConditionCommand;

        [NonSerialized]
        private RelayCommand<QueryConditionViewModel> deleteConditionCommand;

        [NonSerialized]
        private RelayCommand<QueryConditionViewModel> editConditionCommand;

        public AdvancedQueryViewModel(FeatureLayer featureLayer)
        {
            queryConditions = new ObservableCollection<QueryConditionViewModel>();
            model = new AdvancedQueryModel();
        }

        public bool CloseWhenQueryFinished
        {
            get { return closeWhenQueryFinished; }
            set { closeWhenQueryFinished = value; }
        }

        public FeatureLayer SelectedLayer
        {
            get { return selectedLayer; }
            set { selectedLayer = value; }
        }

        public QueryMatchMode SelectedQueryMatchMode
        {
            get { return selectedQueryMatchMode; }
            set
            {
                selectedQueryMatchMode = value;
                RaisePropertyChanged(() => SelectedQueryMatchMode);
            }
        }

        public bool IsQueryMatchModeEnabled
        {
            get
            {
                bool isQueryMatchModeEnabled = true;
                if (queryConditions.Count >= 2)
                {
                    Layer tempLayer = queryConditions.FirstOrDefault().Layer;
                    foreach (var condition in queryConditions)
                    {
                        if (condition.Layer != tempLayer)
                        {
                            isQueryMatchModeEnabled = false;
                            break;
                        }
                    }
                }

                return isQueryMatchModeEnabled;
            }
        }

        public AddHighlightFeaturesMode HighlightMode
        {
            get { return highlightMode; }
            set
            {
                highlightMode = value;
                RaisePropertyChanged(() => HighlightMode);
            }
        }

        public IEnumerable<Feature> ResultFeatures
        {
            get { return resultFeatures; }
            set { resultFeatures = value; }
        }

        public RelayCommand FindCommand
        {
            get
            {
                if (findCommand == null)
                {
                    findCommand = new RelayCommand(() =>
                    {
                        if (GisEditor.ActiveMap != null)
                        {
                            var allFeatureLayersInMap = GisEditor.ActiveMap.GetFeatureLayers(true);
                            var notExistConditions = queryConditions.Where(c => !allFeatureLayersInMap.Contains(c.Layer)).ToArray();
                            foreach (var condition in notExistConditions)
                            {
                                queryConditions.Remove(condition);
                            }
                            ResultFeatures = model.FindFeatures(queryConditions, SelectedQueryMatchMode);
                            if (CloseWhenQueryFinished)
                            {
                                MessengerInstance.Send(true, this);
                            }
                            else
                            {
                                var highlightOverlay = GisEditor.SelectionManager.GetSelectionOverlay();
                                if (highlightOverlay != null)
                                {
                                    highlightOverlay.AddHighlightFeatures(ResultFeatures, HighlightMode);
                                    InMemoryFeatureLayer highlightLayer = highlightOverlay.HighlightFeatureLayer;
                                    RectangleShape resultExtent = ExtentHelper.GetBoundingBoxOfItems(highlightLayer.InternalFeatures);

                                    if (resultExtent != null)
                                    {
                                        var scale = ExtentHelper.GetScale(resultExtent, (float)GisEditor.ActiveMap.ActualWidth, GisEditor.ActiveMap.MapUnit);
                                        GisEditor.ActiveMap.ZoomTo(resultExtent.GetCenterPoint(), scale);
                                    }
                                    else
                                    {
                                        System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GeneralErrorInfo"));
                                        GisEditor.ActiveMap.Refresh(highlightOverlay);
                                    }
                                    if (notExistConditions.Count() > 0)
                                    {
                                        StringBuilder sb = new StringBuilder();
                                        foreach (var item in notExistConditions)
                                        {
                                            sb.Append(item.Layer.Name + ",");
                                        }
                                        DialogMessage dm = new DialogMessage(string.Format(removeConditionsMessage, sb.ToString()), null) { Caption = "Alert" };
                                        MessengerInstance.Send(dm, this);
                                    }
                                }
                            }
                        }
                    });
                } return findCommand;
            }
        }

        public RelayCommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new RelayCommand(() =>
                    {
                        MessengerInstance.Send(false, this);
                    });
                } return cancelCommand;
            }
        }

        public RelayCommand AddConditionCommand
        {
            get
            {
                if (addConditionCommand == null)
                {
                    addConditionCommand = new RelayCommand(() =>
                    {
                        QueryConditionViewModel condition = new QueryConditionViewModel();
                        condition.Layer = SelectedLayer;
                        QueryConditionWindow builder = new QueryConditionWindow(condition);
                        if (builder.ShowDialog().GetValueOrDefault())
                        {
                            Conditions.Add(builder.QueryCondition);
                            RaisePropertyChanged(() => IsQueryMatchModeEnabled);
                        }
                    });
                } return addConditionCommand;
            }
        }

        public RelayCommand<QueryConditionViewModel> DeleteConditionCommand
        {
            get
            {
                if (deleteConditionCommand == null)
                {
                    deleteConditionCommand = new RelayCommand<QueryConditionViewModel>((conditionViewModel) =>
                    {
                        if (Conditions.Contains(conditionViewModel))
                        {
                            Conditions.Remove(conditionViewModel);
                        }
                    });
                } return deleteConditionCommand;
            }
        }

        public RelayCommand<QueryConditionViewModel> EditConditionCommand
        {
            get
            {
                if (editConditionCommand == null)
                {
                    editConditionCommand = new RelayCommand<QueryConditionViewModel>((conditionViewModel) =>
                    {
                        QueryConditionWindow builder = new QueryConditionWindow(conditionViewModel);
                        builder.ShowDialog();
                    });
                }
                return editConditionCommand;
            }
        }

        public ObservableCollection<QueryConditionViewModel> Conditions { get { return queryConditions; } }

        public static IEnumerable<Feature> FilterFeatures(QueryConditionViewModel condition)
        {
            IEnumerable<Feature> filteredFeatures = new Collection<Feature>();
            
            bool isContinue = MessageBoxHelper.ShowWarningMessageIfSoManyCount(condition.Layer);
            if (!isContinue) return new Collection<Feature>();

            condition.Layer.SafeProcess(() =>
            {
                var allFeatures = condition.Layer.QueryTools.GetAllFeatures(condition.Layer.GetDistinctColumnNames());

                switch (condition.QueryOperator.QueryOperaterType)
                {
                    case QueryOperaterType.Equal:
                        filteredFeatures = allFeatures.Where(feature => GetFeatureValue(feature, condition.ColumnName).Equals(condition.MatchValue, StringComparison.InvariantCultureIgnoreCase));
                        break;

                    case QueryOperaterType.Contains:
                        filteredFeatures = allFeatures.Where(feature => GetFeatureValue(feature, condition.ColumnName).IndexOf(condition.MatchValue, StringComparison.InvariantCultureIgnoreCase) != -1);
                        break;

                    case QueryOperaterType.StartsWith:
                        filteredFeatures = allFeatures.Where(feature => GetFeatureValue(feature, condition.ColumnName).StartsWith(condition.MatchValue, StringComparison.InvariantCultureIgnoreCase));
                        break;

                    case QueryOperaterType.EndsWith:
                        filteredFeatures = allFeatures.Where(feature => GetFeatureValue(feature, condition.ColumnName).EndsWith(condition.MatchValue, StringComparison.InvariantCultureIgnoreCase));
                        break;

                    case QueryOperaterType.DoesNotEqual:
                        filteredFeatures = allFeatures.Where(feature => !GetFeatureValue(feature, condition.ColumnName).Equals(condition.MatchValue, StringComparison.InvariantCultureIgnoreCase));
                        break;

                    case QueryOperaterType.DoesNotContain:
                        filteredFeatures = allFeatures.Where(feature => GetFeatureValue(feature, condition.ColumnName).IndexOf(condition.MatchValue, StringComparison.InvariantCultureIgnoreCase) == -1);
                        break;

                    case QueryOperaterType.GreaterThan:
                        filteredFeatures = allFeatures.Where(feature => CompareStringAsNumber(GetFeatureValue(feature, condition.ColumnName), condition.MatchValue, QueryOperaterType.GreaterThan));
                        break;

                    case QueryOperaterType.GreaterThanOrEqualTo:
                        filteredFeatures = allFeatures.Where(feature => CompareStringAsNumber(GetFeatureValue(feature, condition.ColumnName), condition.MatchValue, QueryOperaterType.GreaterThanOrEqualTo));
                        break;

                    case QueryOperaterType.LessThan:
                        filteredFeatures = allFeatures.Where(feature => CompareStringAsNumber(GetFeatureValue(feature, condition.ColumnName), condition.MatchValue, QueryOperaterType.LessThan));
                        break;

                    case QueryOperaterType.LessThanOrEqualTo:
                        filteredFeatures = allFeatures.Where(feature => CompareStringAsNumber(GetFeatureValue(feature, condition.ColumnName), condition.MatchValue, QueryOperaterType.LessThanOrEqualTo));
                        break;
                    default:
                        break;
                }
            });

            Feature[] results = filteredFeatures.ToArray();
            for (int i = 0; i < results.Length; i++)
            {
                results[i].Tag = condition.Layer;
            }

            return results;
        }

        private static string GetFeatureValue(Feature feature, string columnName)
        {
            if (feature.ColumnValues.ContainsKey(columnName))
            {
                return feature.ColumnValues[columnName];
            }
            //else if (feature.LinkColumnValues.ContainsKey(columnName))
            //{
            //    return string.Join(Environment.NewLine, feature.LinkColumnValues[columnName].Select(v => v.Value.ToString()));
            //}
            return "";
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

        private static void UsingFeatureLayer<T>(T layer, Action<T> action, bool requireEditing = true)
            where T : FeatureLayer
        {
            layer.SafeProcess(() =>
            {
                bool inTrans = layer.EditTools.IsInTransaction;
                bool needToSwitchTrans = !inTrans && requireEditing;

                if (needToSwitchTrans)
                {
                    layer.EditTools.BeginTransaction();
                }

                action(layer);

                if (needToSwitchTrans)
                {
                    layer.EditTools.CommitTransaction();
                }
            });
        }
    }
}