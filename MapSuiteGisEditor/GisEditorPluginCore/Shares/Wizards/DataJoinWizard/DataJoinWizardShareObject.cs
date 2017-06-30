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
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class DataJoinWizardShareObject : WizardShareObject
    {
        private bool isIncludeAllFeatures;
        private bool onlyUseSelectedFeatures;
        private string customDelimiter;
        private string selectedDataFilePath;
        private UserControl customContent;
        private FeatureLayer selectedFeatureLayer;
        private RelayCommand viewLayerDataCommand;
        private Dictionary<string, string> delimiters;
        private Dictionary<string, string> invalidColumns;
        private RelayCommand viewDelimitedFileDataCommand;
        private DataJoinAdapter dataJoinAdapter;
        private KeyValuePair<string, string> selectedDelimiter;
        private FeatureSourceColumn selectedIncludeColumnItem;
        private AvailableColumnOption selectedAvailableColumnOption;
        private ObservableCollection<AvailableColumnOption> availableColumnOptions;
        private ObservableCollection<FeatureLayer> featureLayers;
        private ObservableCollection<MatchCondition> matchConditions;
        private ObservableCollection<FeatureSourceColumn> layerColumns;
        private ObservableCollection<FeatureSourceColumn> sourceColumnsList;
        private ObservableCollection<FeatureSourceColumn> includedColumnsList;
        private ObservableCollection<FeatureSourceColumn> delimitedFileColumns;

        public DataJoinWizardShareObject()
        {
            isIncludeAllFeatures = false;
            invalidColumns = new Dictionary<string, string>();
            featureLayers = new ObservableCollection<FeatureLayer>();
            matchConditions = new ObservableCollection<MatchCondition>();
            layerColumns = new ObservableCollection<FeatureSourceColumn>();
            sourceColumnsList = new ObservableCollection<FeatureSourceColumn>();
            includedColumnsList = new ObservableCollection<FeatureSourceColumn>();
            delimitedFileColumns = new ObservableCollection<FeatureSourceColumn>();

            InitSelectableFeatureLayers();

            delimiters = new DelimiterDictionary();
            selectedDelimiter = new KeyValuePair<string, string>("Comma", ",");

            availableColumnOptions = new ObservableCollection<AvailableColumnOption>();
            availableColumnOptions.Add(AvailableColumnOption.LayerandDelimitedFile);
            availableColumnOptions.Add(AvailableColumnOption.Layer);
            availableColumnOptions.Add(AvailableColumnOption.DelimitedFile);
            selectedAvailableColumnOption = AvailableColumnOption.LayerandDelimitedFile;
        }

        public string WizardName
        {
            get { return "DataJoin"; }
        }

        public ObservableCollection<MatchCondition> MatchConditions
        {
            get { return matchConditions; }
        }

        public Dictionary<string, string> InvalidColumns
        {
            get { return invalidColumns; }
        }

        public bool IsIncludeAllFeatures
        {
            get { return isIncludeAllFeatures; }
            set
            {
                isIncludeAllFeatures = value;
                RaisePropertyChanged("IsIncludeAllFeatures");
            }
        }

        public FeatureSourceColumn SelectedIncludeColumnItem
        {
            get { return selectedIncludeColumnItem; }
            set
            {
                selectedIncludeColumnItem = value;
                RaisePropertyChanged("SelectedIncludeColumnItem");
            }
        }

        public ObservableCollection<FeatureSourceColumn> SourceColumnsList
        {
            get { return sourceColumnsList; }
            set
            {
                sourceColumnsList = value;
                RaisePropertyChanged("SourceColumnsList");
            }
        }

        public ObservableCollection<FeatureSourceColumn> IncludedColumnsList
        {
            get { return includedColumnsList; }
            set
            {
                includedColumnsList = value;
                RaisePropertyChanged("IncludedColumnsList");
            }
        }

        public ObservableCollection<AvailableColumnOption> AvailableColumnOptions
        {
            get { return availableColumnOptions; }
        }

        public AvailableColumnOption SelectedAvailableColumnOption
        {
            get { return selectedAvailableColumnOption; }
            set
            {
                selectedAvailableColumnOption = value;
                switch (selectedAvailableColumnOption)
                {
                    case AvailableColumnOption.LayerandDelimitedFile:
                        ResetSourceColumnsList(layerColumns.Concat(delimitedFileColumns));
                        break;
                    case AvailableColumnOption.Layer:
                        ResetSourceColumnsList(layerColumns);
                        break;
                    case AvailableColumnOption.DelimitedFile:
                        ResetSourceColumnsList(delimitedFileColumns);
                        break;
                }
                RaisePropertyChanged("SelectedAvailableColumn");
            }
        }

        public RelayCommand ViewDelimitedFileDataCommand
        {
            get
            {
                if (viewDelimitedFileDataCommand == null)
                {
                    viewDelimitedFileDataCommand = new RelayCommand(() =>
                    {
                        DataViewer previewInputFile = new DataViewer(ReadDataToDataGrid(selectedDataFilePath, Delimiters[SelectedDelimiter.Key]));
                        previewInputFile.ShowDialog();
                    });
                }
                return viewDelimitedFileDataCommand;
            }
        }

        public ObservableCollection<FeatureSourceColumn> DelimitedFileColumns
        {
            get { return delimitedFileColumns; }
        }

        public ObservableCollection<FeatureSourceColumn> LayerColumns
        {
            get { return layerColumns; }
        }

        public string CustomDelimiter
        {
            get { return customDelimiter; }
            set
            {
                customDelimiter = value;
                Delimiters["Custom"] = customDelimiter;
                if (SelectedDelimiter.Key == "Custom")
                    SelectedDelimiter = new KeyValuePair<string, string>("Custom", customDelimiter);
                RaisePropertyChanged("CustomDelimiter");
            }
        }

        public DataJoinAdapter DataJoinAdapter
        {
            get { return dataJoinAdapter; }
            set
            {
                dataJoinAdapter = value;
                RaisePropertyChanged("DataJoinAdapter");
            }
        }

        public UserControl CustomContent
        {
            get { return customContent; }
            set
            {
                customContent = value;
                RaisePropertyChanged("CustomContent");
            }
        }

        public string SelectedDataFilePath
        {
            get { return selectedDataFilePath; }
            set
            {
                selectedDataFilePath = value;
                if (selectedDataFilePath != null)
                {
                    SourceColumnsList.Clear();
                    SelectedFeatureLayer.SafeProcess(() =>
                    {
                        foreach (var column in SelectedFeatureLayer.QueryTools.GetColumns())
                        {
                            SourceColumnsList.Add(column);
                        }
                    });

                    delimitedFileColumns.Clear();

                    CustomContent = null;
                    DataJoinAdapter = DataJoinAdapter.GetInstance(selectedDataFilePath);
                    if (DataJoinAdapter != null)
                    {
                        var customControl = DataJoinAdapter.GetConfigurationContent();
                        if (customControl != null)
                        {
                            customControl.DataContext = this;
                            CustomContent = customControl;
                        }
                        ReadDataColumnToAdd();
                    }

                    MatchConditions.Clear();
                    MatchConditions.Add(new MatchCondition(1, layerColumns, delimitedFileColumns, MatchConditions));

                    if (layerColumns.Count > 0)
                        MatchConditions[0].SelectedLayerColumn = layerColumns[0];
                    if (delimitedFileColumns.Count > 0)
                        MatchConditions[0].SelectedDelimitedColumn = delimitedFileColumns[0];
                }
                RaisePropertyChanged("SelectedDataFilePath");
            }
        }

        public bool IsCustom
        {
            get { return SelectedDelimiter.Key.Equals("Custom", StringComparison.Ordinal); }
        }

        public KeyValuePair<string, string> SelectedDelimiter
        {
            get { return selectedDelimiter; }
            set
            {
                selectedDelimiter = value;
                SelectedDataFilePath = SelectedDataFilePath;
                if (selectedDelimiter.Key != "Custom")
                    CustomDelimiter = "";
                RaisePropertyChanged("SelectedDelimiter");
                RaisePropertyChanged("IsCustom");
            }
        }

        public Dictionary<string, string> Delimiters
        {
            get { return delimiters; }
        }

        public bool HasSelectedFeatures
        {
            get
            {
                var result = GisEditor.SelectionManager.GetSelectedFeatures().Any(f => f.Tag != null && f.Tag == selectedFeatureLayer);
                if (!result) OnlyUseSelectedFeatures = false;
                return result;
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
                        DataViewerUserControl content = new DataViewerUserControl((ShapeFileFeatureLayer)SelectedFeatureLayer);
                        content.ShowDialog();
                    });
                }
                return viewLayerDataCommand;
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

        public ObservableCollection<FeatureLayer> FeatureLayers
        {
            get { return featureLayers; }
        }

        public FeatureLayer SelectedFeatureLayer
        {
            get { return selectedFeatureLayer; }
            set
            {
                selectedFeatureLayer = value;
                if (selectedFeatureLayer != null)
                {
                    layerColumns.Clear();
                    selectedFeatureLayer.SafeProcess(() =>
                    {
                        foreach (var column in selectedFeatureLayer.QueryTools.GetColumns())
                        {
                            layerColumns.Add(column);
                        }
                    });
                }
                RaisePropertyChanged("SelectedFeatureLayer");
                RaisePropertyChanged("HasSelectedFeatures");
            }
        }

        protected override TaskPlugin GetTaskPluginCore()
        {
            var plugin = GisEditor.TaskManager.GetActiveTaskPlugins<DataJoinTaskPlugin>().FirstOrDefault();
            if (plugin != null)
            {
                PrepareTaskParameters(plugin);
            }
            return plugin;
        }

        protected override void LoadToMapCore()
        {
            var getLayersParameters = new GetLayersParameters();
            getLayersParameters.LayerUris.Add(new Uri(OutputPathFileName));
            var layers = GisEditor.LayerManager.GetLayers<ShapeFileFeatureLayer>(getLayersParameters);
            if (layers.Count > 0)
            {
                GisEditor.ActiveMap.AddLayersBySettings(layers);
                GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.LoadToMapCoreDescription));
            }
        }

        private void PrepareTaskParameters(DataJoinTaskPlugin plugin)
        {
            plugin.InvalidColumns = InvalidColumns;
            plugin.Delimiter = SelectedDelimiter.Value;
            plugin.DataJoinAdapter = DataJoinAdapter;
            plugin.SourceLayerColumns = LayerColumns;
            plugin.CsvFilePath = SelectedDataFilePath;
            plugin.IncludedColumnsList = IncludedColumnsList;
            plugin.IsIncludeAllFeatures = IsIncludeAllFeatures;
            plugin.MatchConditions = MatchConditions;
            plugin.DisplayProjectionParameters = GisEditor.ActiveMap.DisplayProjectionParameters;
            if (OutputMode == OutputMode.ToFile)
            {
                plugin.OutputPathFileName = OutputPathFileName;
            }
            else
            {
                string tempPathFileName = Path.Combine(FolderHelper.GetCurrentProjectTaskResultFolder(), TempFileName) + ".shp";
                plugin.OutputPathFileName = tempPathFileName;
                OutputPathFileName = tempPathFileName;
            }
            if (SelectedFeatureLayer is ShapeFileFeatureLayer)
                plugin.CodePage = ((ShapeFileFeatureLayer)SelectedFeatureLayer).Encoding.CodePage;
            else plugin.CodePage = Encoding.Default.CodePage;

            SelectedFeatureLayer.SafeProcess(() =>
            {
                if (HasSelectedFeatures && OnlyUseSelectedFeatures && GisEditor.SelectionManager.GetSelectionOverlay() != null)
                {
                    plugin.Features = GisEditor.SelectionManager.GetSelectionOverlay().HighlightFeatureLayer.InternalFeatures
                                 .Where(f => f.Tag != null && f.Tag == selectedFeatureLayer).ToList();
                }
                else if (SelectedFeatureLayer is ShapeFileFeatureLayer)
                {
                    SelectedFeatureLayer.FeatureSource.Close();
                    var clonedFeatureSource = SelectedFeatureLayer.FeatureSource.CloneDeep();
                    SelectedFeatureLayer.FeatureSource.Open();
                    plugin.ShapeFileFeatureSource = (ShapeFileFeatureSource)clonedFeatureSource;
                }
                else
                {
                    plugin.Features = SelectedFeatureLayer.FeatureSource.GetAllFeatures(SelectedFeatureLayer.FeatureSource.GetDistinctColumnNames()).ToList();
                }
            });
        }

        private void InitSelectableFeatureLayers()
        {
            if (GisEditor.ActiveMap != null)
            {
                foreach (var featureLayer in GisEditor.ActiveMap.GetFeatureLayers(true))
                {
                    featureLayer.SafeProcess(() =>
                    {
                        featureLayers.Add(featureLayer);
                        if (featureLayers.Count > 0) selectedFeatureLayer = featureLayers[0];
                        if (selectedFeatureLayer != null)
                        {
                            layerColumns.Clear();
                            selectedFeatureLayer.SafeProcess(() =>
                            {
                                foreach (var column in selectedFeatureLayer.QueryTools.GetColumns())
                                {
                                    layerColumns.Add(column);
                                }
                            });
                        }
                    });
                }
            }
        }

        public DataTable ReadDataToDataGrid(string inputFilePath, string customParameter)
        {
            DataTable PreviewDataTable = new DataTable();
            if (File.Exists(inputFilePath))
            {
                return DataJoinAdapter.ReadDataToDataGrid(inputFilePath, customParameter);
            }
            return PreviewDataTable;
        }

        private void ReadDataColumnToAdd()
        {
            //CsvFeatureSource csvFeatureSource = new CsvFeatureSource(SelectedDataFilePath);
            //csvFeatureSource.Delimiter = Delimiters[SelectedDelimiter.Key];
            //csvFeatureSource.Open();
            //foreach (var column in csvFeatureSource.GetColumns())
            //{
            //    DataJoinFeatureSourceColumn csvColumn = new DataJoinFeatureSourceColumn(column.ColumnName, column.TypeName, column.MaxLength);
            //    delimitedFileColumns.Add(csvColumn);
            //    SourceColumnsList.Add(csvColumn);
            //}
            //csvFeatureSource.Close();

            Collection<FeatureSourceColumn> columns = DataJoinAdapter.GetColumnToAdd(SelectedDataFilePath, Delimiters[SelectedDelimiter.Key]);

            foreach (var column in columns)
            {
                delimitedFileColumns.Add(column);
                SourceColumnsList.Add(column);
            }
        }

        private void ResetSourceColumnsList(IEnumerable<FeatureSourceColumn> columns)
        {
            SourceColumnsList.Clear();
            foreach (var item in columns)
            {
                if (!IncludedColumnsList.Contains(item))
                    SourceColumnsList.Add(item);
            }
        }
    }
}
