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
using System.IO;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class GridWizardShareObject : WizardShareObject
    {
        private bool hasSelectedFeatures;
        private bool onlyUseSelectedFeatures;
        private bool isAllPointsChecked;
        private bool isOnlyPointsEnable;
        private bool isOnlyPointsChecked;
        private double power;
        private double cellSize;
        private double searchRadius;
        private string warningMessage;
        private Visibility isSelectedIDW;
        private GridDefinition gridDefinition;

        [NonSerialized]
        private RelayCommand viewLayerDataCommand;

        private FeatureLayer selectedFeatureLayer;
        private DistanceUnit selectedRadiusUnit;
        private DistanceUnit selectedCellSizeDistanceUnit;
        private FeatureSourceColumn selectedDataColumn;
        private Dictionary<string, ErrorInfo> errorRecord;
        private Dictionary<string, string> dataColumnNames;
        private KeyValuePair<string, string> selectedDataColumnName;
        private GridInterpolationModel selectedInterpolationAlgorithms;
        private ObservableCollection<FeatureLayer> featureLayers;
        private ObservableCollection<FeatureSourceColumn> dataColumns;
        private ObservableCollection<GridInterpolationModel> interpolationAlgorithms;

        public GridWizardShareObject()
        {
            power = 2;
            cellSize = 1000;
            searchRadius = 100;
            isAllPointsChecked = true;
            warningMessage = string.Empty;
            isSelectedIDW = Visibility.Collapsed;
            ExtensionFilter = "Grid files(*.grd)|*.grd";

            featureLayers = new ObservableCollection<FeatureLayer>();
            interpolationAlgorithms = new ObservableCollection<GridInterpolationModel>();
            dataColumns = new ObservableCollection<FeatureSourceColumn>();
            dataColumnNames = new Dictionary<string, string>();

            InitializePointBaseFeatureLayers();
            interpolationAlgorithms.Add(new InverseDistanceWeightedGridInterpolationModel());
            selectedInterpolationAlgorithms = interpolationAlgorithms.FirstOrDefault();
        }

        public string WizardName
        {
            get { return "Grid"; }
        }

        public double Power
        {
            get { return power; }
            set
            {
                power = value;
                RaisePropertyChanged("Power");
            }
        }

        public double SearchRadius
        {
            get { return searchRadius; }
            set
            {
                searchRadius = value;
                RaisePropertyChanged("SearchRadius");
            }
        }

        public DistanceUnit SelectedRadiusUnit
        {
            get { return selectedRadiusUnit; }
            set
            {
                selectedRadiusUnit = value;
                RaisePropertyChanged("SelectedRadiusUnit");
            }
        }

        public bool IsAllPointsChecked
        {
            get { return isAllPointsChecked; }
            set
            {
                isAllPointsChecked = value;
                if (value)
                {
                    IsOnlyPointsEnable = false;
                    IsOnlyPointsChecked = false;
                }
                RaisePropertyChanged("IsAllPointsChecked");
            }
        }

        public bool IsOnlyPointsChecked
        {
            get { return isOnlyPointsChecked; }
            set
            {
                isOnlyPointsChecked = value;
                if (value)
                {
                    IsAllPointsChecked = false;
                    IsOnlyPointsEnable = true;
                }
                RaisePropertyChanged("IsOnlyPointsChecked");
            }
        }

        public bool IsOnlyPointsEnable
        {
            get { return isOnlyPointsEnable; }
            set
            {
                isOnlyPointsEnable = value;
                RaisePropertyChanged("IsOnlyPointsEnable");
            }
        }

        public Visibility IsSelectedIDW
        {
            get
            {
                if (selectedInterpolationAlgorithms is InverseDistanceWeightedGridInterpolationModel)
                    isSelectedIDW = Visibility.Visible;
                else
                    isSelectedIDW = Visibility.Collapsed;
                return isSelectedIDW;
            }
            set
            {
                isSelectedIDW = value;
                RaisePropertyChanged("IsSelectedIDW");
            }
        }

        public GridDefinition GridDefinition
        {
            get { return gridDefinition; }
            set { gridDefinition = value; }
        }

        public string WarningMessage
        {
            get { return warningMessage; }
            set
            {
                warningMessage = value;
                RaisePropertyChanged("WarningMessage");
            }
        }

        public double CellSize
        {
            get { return cellSize; }
            set
            {
                cellSize = value;
                RaisePropertyChanged("CellSize");
            }
        }

        public KeyValuePair<string, string> SelectedDataColumnName
        {
            get { return selectedDataColumnName; }
            set
            {
                selectedDataColumnName = value;
                SelectedDataColumn = dataColumns.FirstOrDefault(c => c.ColumnName == selectedDataColumnName.Key); ;
            }
        }

        public FeatureSourceColumn SelectedDataColumn
        {
            get { return selectedDataColumn; }
            set
            {
                selectedDataColumn = value;
                RaisePropertyChanged("SelectedDataColumn");
            }
        }

        public DistanceUnit SelectedCellSizeDistanceUnit
        {
            get { return selectedCellSizeDistanceUnit; }
            set
            {
                selectedCellSizeDistanceUnit = value;
                RaisePropertyChanged("SelectedCellSizeDistanceUnit");
            }
        }

        public GridInterpolationModel SelectedInterpolationAlgorithms
        {
            get { return selectedInterpolationAlgorithms; }
            set
            {
                selectedInterpolationAlgorithms = value;

                if (selectedInterpolationAlgorithms is InverseDistanceWeightedGridInterpolationModel)
                    IsSelectedIDW = Visibility.Visible;
                else
                    IsSelectedIDW = Visibility.Collapsed;

                RaisePropertyChanged("SelectedInterpolationAlgorithms");
            }
        }

        public ObservableCollection<GridInterpolationModel> InterpolationAlgorithms
        {
            get { return interpolationAlgorithms; }
        }

        public Dictionary<string, string> DataColumnNames
        {
            get { return dataColumnNames; }
        }

        public ObservableCollection<FeatureSourceColumn> DataColumns
        {
            get { return dataColumns; }
        }

        public FeatureLayer SelectedFeatureLayer
        {
            get { return selectedFeatureLayer; }
            set
            {
                selectedFeatureLayer = value;
                if (selectedFeatureLayer != null)
                {
                    HasSelectedFeatures = GisEditor.SelectionManager.GetSelectedFeatures().Any(f => f.Tag != null && f.Tag == selectedFeatureLayer);

                    try
                    {
                        selectedFeatureLayer.SafeProcess(() =>
                        {
                            dataColumns.Clear();
                            dataColumnNames.Clear();
                            foreach (var column in selectedFeatureLayer.QueryTools.GetColumns())
                            {
                                string alias = selectedFeatureLayer.FeatureSource.GetColumnAlias(column.ColumnName);
                                dataColumns.Add(column);
                                dataColumnNames.Add(column.ColumnName, alias);
                            }

                            if (SelectedDataColumn == null) SelectedDataColumn = dataColumns.FirstOrDefault();
                        });
                    }
                    catch (Exception ex)
                    {
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                        System.Windows.Forms.MessageBox.Show(ex.Message, "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    }
                }
                RaisePropertyChanged("SelectedFeatureLayer");
            }
        }

        public ObservableCollection<FeatureLayer> FeatureLayers
        {
            get { return featureLayers; }
        }

        public bool HasSelectedFeatures
        {
            get { return hasSelectedFeatures; }
            set
            {
                hasSelectedFeatures = value;
                RaisePropertyChanged("HasSelectedFeatures");
                if (!value)
                {
                    OnlyUseSelectedFeatures = false;
                }
            }
        }

        public bool OnlyUseSelectedFeatures
        {
            get { return onlyUseSelectedFeatures; }
            set
            {
                onlyUseSelectedFeatures = value;
                RaisePropertyChanged("OnlyUseSelectedFeatures");
            }
        }

        public RelayCommand ViewLayerDataCommand
        {
            get
            {
                if (viewLayerDataCommand == null)
                {
                    viewLayerDataCommand = new RelayCommand(() =>
                    {
                        DataViewerUserControl content = new DataViewerUserControl(SelectedFeatureLayer);
                        content.ShowDialog();
                    });
                }
                return viewLayerDataCommand;
            }
        }

        protected override TaskPlugin GetTaskPluginCore()
        {
            var plugin = GisEditor.TaskManager.GetActiveTaskPlugins<GridTaskPlugin>().FirstOrDefault();
            if (plugin != null)
            {
                PrepareTaskParameters(plugin);
            }

            return plugin;
        }

        private void PrepareTaskParameters(GridTaskPlugin plugin)
        {
            if (SelectedInterpolationAlgorithms is InverseDistanceWeightedGridInterpolationModel)
            {
                (SelectedInterpolationAlgorithms as InverseDistanceWeightedGridInterpolationModel).Power = Power;
                if (IsAllPointsChecked)
                {
                    (SelectedInterpolationAlgorithms as InverseDistanceWeightedGridInterpolationModel).SearchRadius = 99999999;
                }
                else
                {
                    SearchRadius = Conversion.ConvertMeasureUnits(SearchRadius, SelectedRadiusUnit, DistanceUnit.Meter);
                    (SelectedInterpolationAlgorithms as InverseDistanceWeightedGridInterpolationModel).SearchRadius = SearchRadius;
                }
            }

            if (OutputMode == OutputMode.ToFile)
            {
                plugin.OutputPathFileName = OutputPathFileName;
            }
            else
            {
                string tempPathFileName = Path.Combine(FolderHelper.GetCurrentProjectTaskResultFolder(), TempFileName) + ".grd";
                plugin.OutputPathFileName = tempPathFileName;
                OutputPathFileName = tempPathFileName;
            }
            plugin.GridInterpolationModel = SelectedInterpolationAlgorithms;
            plugin.GridDefinition = GridDefinition;
        }

        protected override void LoadToMapCore()
        {
            ///var gridPlugin = GisEditor.LayerManager.GetSortedPlugins<GridLayerPlugin>().FirstOrDefault();
            var getLayersParameters = new GetLayersParameters();
            getLayersParameters.LayerUris.Add(new Uri(OutputPathFileName));
            var layers = GisEditor.LayerManager.GetLayers<GridFeatureLayer>(getLayersParameters);

            //var layers = gridPlugin.GetLayers(getLayersParameters);
            GisEditor.ActiveMap.AddLayersBySettings(layers);
            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.LoadToMapCoreDescription));
        }

        private void LogError(UpdatingTaskProgressEventArgs args)
        {
            string featureId = args.Message;
            string exceptionMessage = args.Error.Message;
            if (errorRecord == null) errorRecord = new Dictionary<string, ErrorInfo>();
            if (!errorRecord.ContainsKey(featureId + exceptionMessage))
            {
                errorRecord.Add(featureId + exceptionMessage, new ErrorInfo
                {
                    ID = featureId,
                    ErrorMessage = exceptionMessage
                });
            }
        }

        private void ReportErrorIfAny()
        {
            if (errorRecord != null && errorRecord.Count > 0)
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("InvalidFeatures"), "Alert");
            }
        }

        private void InitializePointBaseFeatureLayers()
        {
            if (GisEditor.ActiveMap != null)
            {
                foreach (var featureLayer in GisEditor.ActiveMap.GetFeatureLayers(true))
                {
                    var featureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
                    if (featureLayerPlugin != null)
                    {
                        if (featureLayerPlugin.GetFeatureSimpleShapeType(featureLayer) == SimpleShapeType.Point)
                        {
                            featureLayers.Add(featureLayer);
                        }
                    }
                }
            }
        }
    }
}