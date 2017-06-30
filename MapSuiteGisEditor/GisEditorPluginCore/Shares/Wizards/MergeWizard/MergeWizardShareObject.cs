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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class MergeWizardShareObject : WizardShareObject
    {
        private string tempFilePath;
        private bool doesAddToMap;
        private bool isCheckBoxEnable;
        private bool onlyMergeSelectedFeatures;
        private SimpleShapeType mergedType;
        private FeatureLayer selectedLayerForColumns;
        private ObservableCollection<FeatureLayer> selectedLayers;
        private ObservableCollection<FeatureSourceColumnDefinition> avaliableColumns;
        private ObservableCollection<FeatureSourceColumnDefinition> includedColumns;
        private Dictionary<FeatureLayer, ObservableCollection<FeatureSourceColumnDefinition>> layerColumnPair;

        public MergeWizardShareObject()
        {

            MergedType = SimpleShapeType.Area;
            DoesAddToMap = true;
            selectedLayers = new ObservableCollection<FeatureLayer>();
            avaliableColumns = new ObservableCollection<FeatureSourceColumnDefinition>();
            IncludedColumns = new ObservableCollection<FeatureSourceColumnDefinition>();
            layerColumnPair = new Dictionary<FeatureLayer, ObservableCollection<FeatureSourceColumnDefinition>>();
        }

        public string WizardName
        {
            get { return "Merge"; }
        }

        public SimpleShapeType MergedType
        {
            get { return mergedType; }
            set
            {
                mergedType = value;
                RaisePropertyChanged("MergedType");
                RaisePropertyChanged("AvaliableLayers");
            }
        }

        public IEnumerable<FeatureLayer> AvaliableLayers
        {
            get
            {
                var allFeatureLayers = GisEditor.ActiveMap.GetFeatureLayers(true);
                foreach (var featureLayer in allFeatureLayers)
                {
                    var featureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
                    if (featureLayerPlugin.GetFeatureSimpleShapeType(featureLayer) == MergedType)
                    {
                        yield return featureLayer;
                    }
                }
            }
        }

        public ObservableCollection<FeatureLayer> SelectedLayers
        {
            get { return selectedLayers; }
            set
            {
                selectedLayers = value;
                RaisePropertyChanged("SelectedLayers");
            }
        }

        public bool OnlyMergeSelectedFeatures
        {
            get { return onlyMergeSelectedFeatures; }
            set
            {
                onlyMergeSelectedFeatures = value;
                IsCheckBoxEnable = OnlyMergeSelectedFeatures;
                RaisePropertyChanged("OnlyMergeSelectedFeatures");
            }
        }

        public ObservableCollection<FeatureSourceColumnDefinition> AvaliableColumns
        {
            get { return avaliableColumns; }
        }

        public ObservableCollection<FeatureSourceColumnDefinition> IncludedColumns
        {
            get { return includedColumns; }
            set
            {
                includedColumns = value;
                RaisePropertyChanged("IncludedColumns");
            }
        }

        public bool DoesAddToMap
        {
            get { return doesAddToMap; }
            set
            {
                doesAddToMap = value;
                RaisePropertyChanged("DoesAddToMap");
            }
        }

        public FeatureLayer SelectedLayerForColumns
        {
            get { return selectedLayerForColumns; }
            set
            {
                selectedLayerForColumns = value;
                RaisePropertyChanged("SelectedLayerForColumns");
                avaliableColumns = layerColumnPair.Where(l => l.Key == selectedLayerForColumns).FirstOrDefault().Value;
                RaisePropertyChanged("AvaliableColumns");
            }
        }

        public Dictionary<FeatureLayer, ObservableCollection<FeatureSourceColumnDefinition>> LayerColumnPair
        {
            get { return layerColumnPair; }
        }

        public bool IsCheckBoxEnable
        {
            get { return isCheckBoxEnable; }
            set
            {
                isCheckBoxEnable = value;
                RaisePropertyChanged("IsCheckBoxEnable");
            }
        }

        protected override TaskPlugin GetTaskPluginCore()
        {
            List<FeatureSource> featureSources = null;
            if (OnlyMergeSelectedFeatures)
            {
                SaveSelectedFeaturesToTempFile();
                featureSources = new List<FeatureSource> { new ShapeFileFeatureSource(tempFilePath) };
            }
            else
            {
                featureSources = SelectedLayers.Select(featureLayer => featureLayer.FeatureSource).ToList();
                foreach (var featureSource in featureSources)
                {
                    featureSource.Close();
                    if (featureSource.Projection != null) featureSource.Projection.Close();
                }
            }

            var columns = IncludedColumns.Select(c => c.ToFeatureSourceColumn()).ToArray();
            var plugin = GisEditor.TaskManager.GetActiveTaskPlugins<MergeTaskPlugin>().FirstOrDefault();
            if (plugin != null)
            {
                InitializePlugin(plugin, featureSources, columns);
            }

            return plugin;
        }

        private void InitializePlugin(MergeTaskPlugin plugin, List<FeatureSource> featureSources, FeatureSourceColumn[] columns)
        {
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
            plugin.Wkt = Proj4Projection.ConvertProj4ToPrj(GisEditor.ActiveMap.DisplayProjectionParameters);
            plugin.FeatureSources = featureSources;
            plugin.Columns = columns;
        }

        protected override void LoadToMapCore()
        {
            if (File.Exists(OutputPathFileName))
            {
                var getLayersParameters = new GetLayersParameters();
                getLayersParameters.LayerUris.Add(new Uri(OutputPathFileName));
                var layers = GisEditor.LayerManager.GetLayers<ShapeFileFeatureLayer>(getLayersParameters);
                GisEditor.ActiveMap.Dispatcher.BeginInvoke(new Action(() =>
                {
                    GisEditor.ActiveMap.AddLayersBySettings(layers);
                    GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.LoadToMapCoreDescription));
                }));
            }

            try
            {
                ShapeFileFeatureLayerExtension.RemoveShapeFiles(tempFilePath);
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Error, ex.Message, ex);
            }
        }

        private void SaveSelectedFeaturesToTempFile()
        {
            string tempDir = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, TempPath);
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            tempFilePath = Path.Combine(tempDir, "MergeTemp.shp");

            var selectionOverlay = GisEditor.SelectionManager.GetSelectionOverlay();
            if (selectionOverlay != null)
            {
                var selectedFeatures = selectionOverlay.HighlightFeatureLayer.InternalFeatures
                    .Where(tmpFeature => SelectedLayers.Contains((Layer)tmpFeature.Tag));

                string projectionInWKT = Proj4Projection.ConvertProj4ToPrj(GisEditor.ActiveMap.DisplayProjectionParameters);
                FileExportInfo info = new FileExportInfo(selectedFeatures, GetColumnsOfSelectedLayers(), tempFilePath, projectionInWKT);
                ShapeFileExporter exporter = new ShapeFileExporter();
                exporter.ExportToFile(info);
            }
        }

        private IEnumerable<FeatureSourceColumn> GetColumnsOfSelectedLayers()
        {
            Collection<FeatureSourceColumn> columns = new Collection<FeatureSourceColumn>();
            foreach (FeatureLayer layer in SelectedLayers)
            {
                layer.SafeProcess(() =>
                {
                    foreach (var column in layer.FeatureSource.GetColumns())
                    {
                        columns.Add(column);
                    }
                });
                //layer.Open();
                //foreach (var column in layer.FeatureSource.GetColumns())
                //{
                //    yield return column;
                //}
                //layer.Close();
            }
            return columns;
        }
    }
}