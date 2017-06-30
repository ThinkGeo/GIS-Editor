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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class SplitWizardShareObject : WizardShareObject
    {
        private string busyContent;
        private string tempFilePath;
        private string outputFileName;
        private bool isBusy;
        private bool overwriteOutputFiles;
        private bool hasSelectedFeatures;
        private bool useSelectedFeaturesOnly;
        private bool isTempFileChecked;
        private bool isOutputChecked;
        private FeatureSourceColumn selectedFeatureSourceColumn;
        private InMemoryFeatureLayer highlightFeatureLayer;
        private FeatureLayer selectedLayerToSplit;
        private Collection<string> outputShapeFileNames;
        private Collection<LayerOverlay> overlaysToRefresh;
        private ObservableCollection<FeatureLayer> layersReadyToSplit;
        private ObservableCollection<FeatureSourceColumn> columnsInSelectedLayer;
        private ObservableCollection<SplitFileModel> exportConfiguration;

        private Dictionary<string, string> columnNamesInSelectedLayer;
        private KeyValuePair<string, string> selectedFeatureSourceColumnName;

        public SplitWizardShareObject()
        {
            OutputMode = OutputMode.ToTemporary;
            isTempFileChecked = OutputMode == OutputMode.ToTemporary;
            layersReadyToSplit = new ObservableCollection<FeatureLayer>();
            columnsInSelectedLayer = new ObservableCollection<FeatureSourceColumn>();
            exportConfiguration = new ObservableCollection<SplitFileModel>();
            overlaysToRefresh = new Collection<LayerOverlay>();
            outputShapeFileNames = new Collection<string>();
            OverwriteOutputFiles = true;
            columnNamesInSelectedLayer = new Dictionary<string, string>();
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                RaisePropertyChanged("IsBusy");
            }
        }

        public bool IsTempFileChecked
        {
            get { return isTempFileChecked; }
            set
            {
                isTempFileChecked = value;
                if (isTempFileChecked)
                {
                    OutputMode = OutputMode.ToTemporary;
                }
                RaisePropertyChanged("IsTempFileChecked");
            }
        }

        public bool IsOutputChecked
        {
            get { return isOutputChecked; }
            set
            {
                isOutputChecked = value;
                if (isOutputChecked)
                {
                    OutputMode = OutputMode.ToFile;
                }
                RaisePropertyChanged("IsOutputChecked");
            }
        }

        public string BusyContent
        {
            get { return busyContent; }
            set
            {
                busyContent = value;
                RaisePropertyChanged("BusyContent");
            }
        }

        public Dictionary<string, string> ColumnNamesInSelectedLayer
        {
            get { return columnNamesInSelectedLayer; }
        }

        public KeyValuePair<string, string> SelectedFeatureSourceColumnName
        {
            get { return selectedFeatureSourceColumnName; }
            set 
            { 
                selectedFeatureSourceColumnName = value;
                SelectedFeatureSourceColumn = ColumnsInSelectedLayer.FirstOrDefault(c => c.ColumnName == value.Key);
            }
        }

        public ObservableCollection<FeatureLayer> LayersReadyToSplit
        {
            get { return layersReadyToSplit; }
        }

        public ObservableCollection<FeatureSourceColumn> ColumnsInSelectedLayer
        {
            get { return columnsInSelectedLayer; }
        }

        public ObservableCollection<SplitFileModel> ExportConfiguration
        {
            get { return exportConfiguration; }
        }

        public Collection<LayerOverlay> OverlaysToRefresh
        {
            get { return overlaysToRefresh; }
        }

        public FeatureLayer SelectedLayerToSplit
        {
            get { return selectedLayerToSplit; }
            set
            {
                selectedLayerToSplit = value;
                HasSelectedFeatures = HighlightFeatureLayer.InternalFeatures.Any(f => f.Tag == SelectedLayerToSplit);
                RaisePropertyChanged("SelectedLayerToSplit");
                RaisePropertyChanged("HasSelectedFeatures");
            }
        }

        public FeatureSourceColumn SelectedFeatureSourceColumn
        {
            get { return selectedFeatureSourceColumn; }
            set
            {
                selectedFeatureSourceColumn = value;
                RaisePropertyChanged("SelectedFeatureSourceColumn");
            }
        }

        public bool UseSelectedFeaturesOnly
        {
            get { return useSelectedFeaturesOnly; }
            set
            {
                useSelectedFeaturesOnly = value;
                RaisePropertyChanged("UseSelectedFeaturesOnly");
            }
        }

        public bool OverwriteOutputFiles
        {
            get { return overwriteOutputFiles; }
            set
            {
                overwriteOutputFiles = value;
                RaisePropertyChanged("OverwriteOutputFiles");
            }
        }

        public bool HasSelectedFeatures
        {
            get { return hasSelectedFeatures; }
            set
            {
                hasSelectedFeatures = value;
                RaisePropertyChanged("HasSelectedFeatures");
            }
        }

        public InMemoryFeatureLayer HighlightFeatureLayer
        {
            get { return highlightFeatureLayer; }
            set
            {
                highlightFeatureLayer = value;
                RaisePropertyChanged("HighlightFeatureLayer");
            }
        }

        public string OutputPath
        {
            get { return outputFileName; }
            set
            {
                outputFileName = value;
                RaisePropertyChanged("OutputPath");
            }
        }

        public Collection<string> OutputShapeFileNames
        {
            get { return outputShapeFileNames; }
        }

        public Thread CurrentThread { get; set; }

        protected override TaskPlugin GetTaskPluginCore()
        {
            OutputShapeFileNames.Clear();
            OverlaysToRefresh.Clear();

            FeatureSource featureSource = null;
            if (UseSelectedFeaturesOnly)
            {
                SaveSelectedFeaturesToTempFile();
                featureSource = new ShapeFileFeatureSource(tempFilePath);
            }
            else
            {
                featureSource = SelectedLayerToSplit.FeatureSource;
            }

            if (featureSource.IsOpen)
            {
                featureSource.Close();
                if (featureSource.Projection != null) featureSource.Projection.Close();
            }

            Dictionary<string, string> exportConfigs = new Dictionary<string, string>();

            var plugin = GisEditor.TaskManager.GetActiveTaskPlugins<SplitTaskPlugin>().FirstOrDefault();
            if (plugin != null)
            {
                InitializePlugin(plugin, featureSource, exportConfigs);
            }

            var configsNeedToExport = ExportConfiguration.Where(config => config.NeedsToExport);
            foreach (var item in configsNeedToExport)
            {
                string finalShapeFilePath = Path.Combine(OutputPath, item.OutputFileName + ".shp");
                if (File.Exists(finalShapeFilePath) && overwriteOutputFiles)
                {
                    CloseExistingLayersAndCollectOverlaysToRefresh(finalShapeFilePath);
                }
                OutputShapeFileNames.Add(finalShapeFilePath);
                exportConfigs.Add(item.ColumnValue, item.OutputFileName + ".shp");
            }

            return plugin;
        }

        private void InitializePlugin(SplitTaskPlugin plugin, FeatureSource featureSource, Dictionary<string, string> exportConfigs)
        {
            if (OutputMode == OutputMode.ToFile)
            {
                plugin.OutputPath = OutputPath;
            }
            else
            {
                plugin.OutputPath = FolderHelper.GetCurrentProjectTaskResultFolder();
                OutputPath = plugin.OutputPath;
            }
            plugin.Wkt = Proj4Projection.ConvertProj4ToPrj(GisEditor.ActiveMap.DisplayProjectionParameters);
            plugin.FeatureSource = featureSource;
            plugin.SplitColumnName = SelectedFeatureSourceColumn.ColumnName;
            plugin.ExportConfigs = exportConfigs;
            plugin.LayerName = SelectedLayerToSplit.Name;
            plugin.OverwriteOutputFiles = OverwriteOutputFiles;
        }

        protected override void LoadToMapCore()
        {
            if (ExportConfiguration.Count(tmpConfig => tmpConfig.NeedsToExport) > 0)
            {
                Collection<ShapeFileFeatureLayer> layers = GetShapeFileLayers(OutputShapeFileNames);
                GisEditor.ActiveMap.AddLayersBySettings(layers);
                GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.LoadToMapCoreDescription));

                foreach (var overlay in OverlaysToRefresh)
                {
                    overlay.Invalidate();
                }
            }
        }

        private Collection<ShapeFileFeatureLayer> GetShapeFileLayers(IEnumerable<string> fileNames)
        {
            GetLayersParameters getLayersParameters = new GetLayersParameters();
            foreach (string item in fileNames)
            {
                if (File.Exists(item))
                {
                    getLayersParameters.LayerUris.Add(new Uri(item));
                }
            }

            return GisEditor.LayerManager.GetLayers<ShapeFileFeatureLayer>(getLayersParameters);
        }

        private void SaveSelectedFeaturesToTempFile()
        {
            string tempDir = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, TempPath);
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            tempFilePath = Path.Combine(tempDir, "SplitTemp.shp");

            var selectedFeatures = HighlightFeatureLayer.InternalFeatures.Where(f => f.Tag == SelectedLayerToSplit);

            string projectionInWKT = Proj4Projection.ConvertProj4ToPrj(GisEditor.ActiveMap.DisplayProjectionParameters);
            FileExportInfo info = new FileExportInfo(selectedFeatures, ColumnsInSelectedLayer, tempFilePath, projectionInWKT);

            ShapeFileExporter exporter = new ShapeFileExporter();
            exporter.ExportToFile(info);
        }

        private void CloseExistingLayersAndCollectOverlaysToRefresh(string shapefileLayerPath)
        {
            foreach (var overlay in GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>())
            {
                foreach (var layer in overlay.Layers.OfType<ShapeFileFeatureLayer>())
                {
                    if (layer.ShapePathFilename.Equals(shapefileLayerPath, StringComparison.OrdinalIgnoreCase))
                    {
                        lock (layer)
                        {
                            layer.Close();
                            if (layer.FeatureSource.Projection != null)
                            {
                                layer.FeatureSource.Projection.Close();
                            }
                        }
                        if (!OverlaysToRefresh.Contains(overlay))
                        {
                            OverlaysToRefresh.Add(overlay);
                        }
                    }
                }
            }
        }

        public void GenerateExportConfigAsync()
        {
            ExportConfiguration.Clear();
            CurrentThread = new Thread(new ThreadStart(GenerateExportConfig));

            IsBusy = true;
            BusyContent = "Analyzing...";
            CurrentThread.Start();

            while (IsBusy)
            {
                System.Windows.Forms.Application.DoEvents();
                Thread.SpinWait(100);
            }
        }

        private void GenerateExportConfig()
        {
            Collection<SplitFileModel> exportedModels = new Collection<SplitFileModel>();
            SelectedLayerToSplit.SafeProcess(() =>
            {
                string selectedColumnName = SelectedFeatureSourceColumn.ColumnName;
                Collection<Tuple<string, int>> distinctColumnValues = new Collection<Tuple<string, int>>();
                if (UseSelectedFeaturesOnly)
                {
                    var tmpColumnValues = HighlightFeatureLayer.InternalFeatures
                        .Where(tmpFeature => tmpFeature.Tag.Equals(SelectedLayerToSplit))
                        .Select(tmpFeature => tmpFeature.ColumnValues[selectedColumnName]);

                    foreach (var item in tmpColumnValues.GroupBy(tmpColumnValue => tmpColumnValue))
                    {
                        distinctColumnValues.Add(new Tuple<string, int>(item.Key, item.Count()));
                    }
                }
                else
                {
                    var tmpShapeFileFeatureLayer = SelectedLayerToSplit as ShapeFileFeatureLayer;
                    Collection<string> values = new Collection<string>();
                    if (tmpShapeFileFeatureLayer != null)
                    {
                        string dbfPath = Path.ChangeExtension(tmpShapeFileFeatureLayer.ShapePathFilename, ".dbf");
                        GeoDbf geoDbf = new GeoDbf(dbfPath, GeoFileReadWriteMode.Read, tmpShapeFileFeatureLayer.Encoding);
                        geoDbf.Open();

                        for (int i = 1; i <= geoDbf.RecordCount; i++)
                        {
                            var columnValue = geoDbf.ReadRecordAsString(i)[selectedColumnName];
                            values.Add(columnValue);
                        }
                        geoDbf.Close();
                    }
                    else
                    {
                        foreach (var feature in SelectedLayerToSplit.QueryTools.GetAllFeatures(new string[] { selectedColumnName }))
                        {
                            values.Add(feature.ColumnValues[selectedColumnName]);
                        }
                    }

                    var groupedTmpColumnValues = values.GroupBy(tmpColumnValue => tmpColumnValue);
                    foreach (var item in groupedTmpColumnValues)
                    {
                        distinctColumnValues.Add(new Tuple<string, int>(item.Key, item.Count()));
                    }
                }

                foreach (var distinctValue in distinctColumnValues)
                {
                    string columnValue = distinctValue.Item1;
                    if (!String.IsNullOrEmpty(columnValue))
                    {
                        Regex regex = new Regex(@"\w*");
                        var matches = regex.Matches(columnValue);
                        StringBuilder fileNameBuilder = new StringBuilder();
                        foreach (Match match in matches)
                        {
                            fileNameBuilder.Append(match.Value);
                        }
                        columnValue = fileNameBuilder.ToString();
                    }

                    SplitFileModel model = new SplitFileModel();
                    model.ColumnValue = String.IsNullOrEmpty(distinctValue.Item1) ? "<Blank>" : distinctValue.Item1;
                    model.FeatureCount = distinctValue.Item2;
                    model.OutputFileName = String.Format(CultureInfo.InvariantCulture
                        , "{0}_{1}"
                        , SelectedLayerToSplit.Name
                        , String.IsNullOrEmpty(columnValue) ? "unknown" : columnValue);

                    if (exportedModels
                        .Any(tmpModel => tmpModel.OutputFileName.Equals(model.OutputFileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        var count = exportedModels.Count(tmpModel
                            => tmpModel.OutputFileName.Equals(model.OutputFileName, StringComparison.OrdinalIgnoreCase));
                        model.OutputFileName += String.Format(CultureInfo.InvariantCulture, "_{0}", count);
                        //model.OutputFileName = model.OutputFileName.Replace(".shp"
                        //, String.Format(CultureInfo.InvariantCulture, "_{0}.shp", count));
                    }

                    exportedModels.Add(model);
                }
            });

#if GISEditorUnitTest
            foreach (var model in exportedModels)
            {
               ExportConfiguration.Add(model);
            }
#else
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (var model in exportedModels)
                    {
                        ExportConfiguration.Add(model);
                    }
                }));
            }
#endif
            IsBusy = false;
            BusyContent = String.Empty;
            CurrentThread = null;
        }
    }
}