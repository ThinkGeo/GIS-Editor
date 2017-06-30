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
    public class BlendWizardShareObject : WizardShareObject
    {
        private int progress;
        private string tempFilePath;
        private string busyContent;
        private bool isCombine;
        private bool isIntersect;
        private bool outputToMap;
        private bool outputToFile;
        private bool addFileToMap;
        private bool blendSelectedFeaturesOnly;
        private ObservableCollection<FeatureLayer> areaLayers;
        private ObservableCollection<FeatureLayer> selectedAreaLayers;
        private ObservableCollection<FeatureSourceColumnDefinition> columnsToInclude;
        private SelectionTrackInteractiveOverlay overlay;
        private List<string> tempFilesForIntersect;
        private bool hasSelectedFeatures;

        public BlendWizardShareObject()
            : base()
        {
            areaLayers = new ObservableCollection<FeatureLayer>();
            columnsToInclude = new ObservableCollection<FeatureSourceColumnDefinition>();
            selectedAreaLayers = new ObservableCollection<FeatureLayer>();
            selectedAreaLayers.CollectionChanged += (sender, arg) => RaisePropertyChanged("SelectedAreaLayers");

            IsCombine = true;
            IsIntersect = false;
            OutputToMap = false;
            OutputToFile = true;
            AddFileToMap = true;

            InitAreaLayers();
            BusyContent = "Processing";
        }

        public string WizardName
        {
            get { return "Blend"; }
        }

        public ObservableCollection<FeatureLayer> AreaLayers
        {
            get { return areaLayers; }
        }

        public ObservableCollection<FeatureLayer> SelectedAreaLayers
        {
            get { return selectedAreaLayers; }
        }

        public ObservableCollection<FeatureSourceColumnDefinition> ColumnsToInclude
        {
            get { return columnsToInclude; }
        }

        public bool BlendSelectedFeaturesOnly
        {
            get { return blendSelectedFeaturesOnly; }
            set { blendSelectedFeaturesOnly = value; }
        }

        public bool IsCombine
        {
            get { return isCombine; }
            set { isCombine = value; }
        }

        public bool IsIntersect
        {
            get { return isIntersect; }
            set { isIntersect = value; }
        }

        public bool OutputToMap
        {
            get { return outputToMap; }
            set { outputToMap = value; }
        }

        public bool OutputToFile
        {
            get { return outputToFile; }
            set { outputToFile = value; }
        }

        public bool AddFileToMap
        {
            get { return addFileToMap; }
            set { addFileToMap = value; }
        }

        public SelectionTrackInteractiveOverlay Overlay
        {
            get { return overlay; }
            set
            {
                overlay = value;
            }
        }

        public int Progress
        {
            get { return progress; }
            set { progress = value; }
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

        public bool HasSelectedFeatures
        {
            get { return hasSelectedFeatures; }
            set
            {
                hasSelectedFeatures = value;
                RaisePropertyChanged("HasSelectedFeatures");
            }
        }

        protected override TaskPlugin GetTaskPluginCore()
        {
            List<FeatureLayer> featureLayers = null;
            if (BlendSelectedFeaturesOnly)
            {
                SaveFeaturesToTempFile();

                if (IsIntersect)
                {
                    featureLayers = tempFilesForIntersect.Select(fileName => new ShapeFileFeatureLayer(fileName)).Cast<FeatureLayer>().ToList();
                }
                else
                {
                    featureLayers = new List<FeatureLayer> { new ShapeFileFeatureLayer(tempFilePath) };
                }
            }
            else
            {
                featureLayers = SelectedAreaLayers.ToList();
            }

            featureLayers.ForEach(featureSource =>
            {
                if (featureSource.IsOpen) featureSource.Close();
                if (featureSource.FeatureSource.Projection != null) featureSource.FeatureSource.Projection.Close();
            });

            var plugin = GisEditor.TaskManager.GetActiveTaskPlugins<BlendTaskPlugin>().FirstOrDefault();
            if (plugin != null)
            {
                InitializePlugin(plugin, featureLayers);
            }

            return plugin;
        }

        private void InitializePlugin(BlendTaskPlugin plugin, List<FeatureLayer> featureSources)
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
            plugin.FeatureLayers = featureSources;
            plugin.RenameDictionary = FeatureSourceColumnDefinition.RenameDictionary;
            plugin.IsIntersect = IsIntersect;
            plugin.IsCombine = IsCombine;
            plugin.OutputToFile = OutputToFile;
            plugin.DisplayProjectionParameters = GisEditor.ActiveMap.DisplayProjectionParameters;
            plugin.ColumnsToInclude = ColumnsToInclude.Select(c => c.ToFeatureSourceColumn()).ToList();
        }

        private void InitAreaLayers()
        {
            if (GisEditor.ActiveMap != null)
            {
                var areaLayers = from tmpLayer in GisEditor.ActiveMap.GetFeatureLayers(true)
                                 where new Func<FeatureLayer, bool>(CheckIsAreaBasedFeatureLayer)(tmpLayer)
                                 select tmpLayer;

                foreach (var layer in areaLayers)
                {
                    AreaLayers.Add(layer);
                }
            }
        }

        protected override void LoadToMapCore()
        {
            if (File.Exists(OutputPathFileName))
            {
                var getLayersParameters = new GetLayersParameters();
                getLayersParameters.LayerUris.Add(new Uri(OutputPathFileName));
                var layers = GisEditor.LayerManager.GetLayers<ShapeFileFeatureLayer>(getLayersParameters);
                if (layers != null)
                {
                    GisEditor.ActiveMap.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        GisEditor.ActiveMap.AddLayersBySettings(layers);
                        GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.LoadToMapCoreDescription));
                    }));
                }
            }
        }

        private bool CheckIsAreaBasedFeatureLayer(FeatureLayer featureLayer)
        {
            var featureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
            if (featureLayerPlugin != null)
            {
                return featureLayerPlugin.GetFeatureSimpleShapeType(featureLayer) == SimpleShapeType.Area;
            }
            else return false;
        }

        private void SaveFeaturesToTempFile()
        {
            string tempDir = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, TempPath);
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            tempFilePath = Path.Combine(tempDir, "BlendTemp.shp");

            List<Feature> featuresToBlend = null;

            if (BlendSelectedFeaturesOnly)
            {
                featuresToBlend = FilterSelectedFeatures();
            }

            //rename the IDs, becaue features from different layers may have the same ID.
            //and the exporter can not export features that have the same IDs
            featuresToBlend = RenameFeatureIds(featuresToBlend);
            string projectionInWKT = Proj4Projection.ConvertProj4ToPrj(GisEditor.ActiveMap.DisplayProjectionParameters);

            var columns = ColumnsToInclude.Select(c => c.ToFeatureSourceColumn()).ToList();
            if (IsIntersect)
            {
                tempFilesForIntersect = new List<string>();

                var featureGroups = featuresToBlend.GroupBy(feature => feature.Tag).ToList();
                foreach (var group in featureGroups)
                {
                    string path = Path.Combine(tempDir, string.Format("BlendTemp{0}.shp", ((Layer)group.Key).Name));
                    tempFilesForIntersect.Add(path);
                    FileExportInfo info = new FileExportInfo(group, columns, path, projectionInWKT);
                    ShapeFileExporter exporter = new ShapeFileExporter();
                    exporter.ExportToFile(info);
                }
            }
            else
            {
                FileExportInfo info = new FileExportInfo(featuresToBlend, columns, tempFilePath, projectionInWKT);
                ShapeFileExporter exporter = new ShapeFileExporter();
                exporter.ExportToFile(info);
            }
        }

        private List<Feature> RenameFeatureIds(IEnumerable<Feature> features)
        {
            var results = new List<Feature>();

            foreach (var feature in features)
            {
                if (results.Where(resultFeature => resultFeature.Id == feature.Id).Count() == 0)
                {
                    results.Add(feature);
                }
                else
                {
                    Feature copy = new Feature(feature.GetWellKnownBinary(), Guid.NewGuid().ToString(), feature.ColumnValues)
                    {
                        Tag = feature.Tag
                    };
                    results.Add(copy);
                }
            }

            return results;
        }

        private List<Feature> FilterSelectedFeatures()
        {
            var filteredSelectedFeatures = overlay.HighlightFeatureLayer.InternalFeatures
                                      .Where(f => SelectedAreaLayers.Any(l => l == f.Tag))
                                      .ToList();
            return filteredSelectedFeatures;
        }
    }
}
