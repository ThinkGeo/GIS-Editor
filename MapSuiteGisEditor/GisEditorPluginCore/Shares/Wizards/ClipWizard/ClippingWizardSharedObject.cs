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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ClippingWizardSharedObject : WizardShareObject
    {
        private bool isAddToMap;
        private bool isOutToFile;
        private bool isUseSelectedFeatures;
        private bool isUseSelectedFeaturesEnable;
        private string clippingLayerTempPath;
        private FeatureLayer masterLayer;
        private InMemoryFeatureLayer resultLayer;
        private ClippingTypeModel currentClippingType;
        private ObservableCollection<ClippingTypeModel> clippingTypes;
        private ObservableCollection<ClippingLayerViewModel> clippingLayers;

        public ClippingWizardSharedObject()
            : base()
        {
            IsAddToMap = true;
            clippingTypes = new ObservableCollection<ClippingTypeModel>();
            clippingTypes.Add(new ClippingTypeModel(GisEditor.LanguageManager.GetStringResource("ClippingWizardShareObjectStep3StandardClipTitle"), ClippingType.Standard, GisEditor.LanguageManager.GetStringResource("ClippingWizardShareObjectStep3StandardClipDescription"), "/GisEditorPluginCore;component/Images/standard.png"));
            clippingTypes.Add(new ClippingTypeModel(GisEditor.LanguageManager.GetStringResource("ClippingWizardShareObjectStep3InverseClipTitle"), ClippingType.Inverse, GisEditor.LanguageManager.GetStringResource("ClippingWizardShareObjectStep3InverseClipDescription"), "/GisEditorPluginCore;component/Images/inverse.png"));
            CurrentClippingType = clippingTypes.FirstOrDefault();
        }

        public string WizardName
        {
            get { return "Clip"; }
        }

        public FeatureLayer MasterLayer
        {
            get { return masterLayer; }
            set
            {
                masterLayer = value;
                RaisePropertyChanged("MasterLayer");

                ClippingLayers = new ObservableCollection<ClippingLayerViewModel>();
                foreach (var layer in AllLayers)
                {
                    var featureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(layer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
                    if (featureLayerPlugin != null
                        && layer != MasterLayer
                        && featureLayerPlugin.GetFeatureSimpleShapeType(layer) == SimpleShapeType.Area)
                    {
                        ClippingLayers.Add(new ClippingLayerViewModel { FeatureLayer = layer });
                    }
                }
            }
        }

        public ObservableCollection<FeatureLayer> AllLayers
        {
            get
            {
                return new ObservableCollection<FeatureLayer>(GisEditor.ActiveMap.GetFeatureLayers(true));
            }
        }

        public ObservableCollection<ClippingLayerViewModel> ClippingLayers
        {
            get { return clippingLayers; }
            set
            {
                clippingLayers = value;
                RaisePropertyChanged("ClippingLayers");
            }
        }

        public bool IsUseSelectedFeatures
        {
            get { return isUseSelectedFeatures; }
            set
            {
                isUseSelectedFeatures = value;
                RaisePropertyChanged("IsUseSelectedFeatures");
            }
        }

        public InMemoryFeatureLayer ResultLayer
        {
            get { return resultLayer; }
            set
            {
                resultLayer = value;
                RaisePropertyChanged("ResultLayer");
            }
        }

        public bool IsAddToMap
        {
            get { return isAddToMap; }
            set
            {
                isAddToMap = value;
                RaisePropertyChanged("IsAddToMap");
            }
        }

        public bool IsOutToFile
        {
            get { return isOutToFile; }
            set
            {
                isOutToFile = value;
                RaisePropertyChanged("IsOutToFile");
            }
        }

        public bool IsUseSelectedFeaturesEnable
        {
            get { return isUseSelectedFeaturesEnable; }
            set
            {
                isUseSelectedFeaturesEnable = value;
                RaisePropertyChanged("IsUseSelectedFeaturesEnable");
            }
        }

        public ObservableCollection<ClippingTypeModel> ClippingTypes
        {
            get { return clippingTypes; }
        }

        public ClippingTypeModel CurrentClippingType
        {
            get { return currentClippingType; }
            set
            {
                currentClippingType = value;
                RaisePropertyChanged("CurrentClippingType");
            }
        }

        protected override void LoadToMapCore()
        {
            if (File.Exists(OutputPathFileName))
            {
                var getLayersParameters = new GetLayersParameters();
                getLayersParameters.LayerUris.Add(new Uri(OutputPathFileName));
                var layers = GisEditor.LayerManager.GetLayers<ShapeFileFeatureLayer>(getLayersParameters);

                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        GisEditor.ActiveMap.AddLayersBySettings(layers);
                        GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.LoadToMapCoreDescription));
                    }));
                }
                else
                {
                    GisEditor.ActiveMap.AddLayersBySettings(layers);
                    GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.LoadToMapCoreDescription));
                }
            }

            ShapeFileFeatureLayerExtension.RemoveShapeFiles(clippingLayerTempPath);
        }

        protected override TaskPlugin GetTaskPluginCore()
        {
            if (MasterLayer.IsOpen)
            {
                lock (MasterLayer) MasterLayer.Close();
            }

            List<FeatureLayer> clippingLayerFeatureLayers = null;
            if (IsUseSelectedFeatures)
            {
                SaveClippingLayer();
                clippingLayerFeatureLayers = new List<FeatureLayer> { new ShapeFileFeatureLayer(clippingLayerTempPath) };
            }
            else
            {
                clippingLayerFeatureLayers = ClippingLayers.Where(l => l.IsSelected).Select(l => l.FeatureLayer).ToList();
            }
            if (MasterLayer.FeatureSource.IsOpen)
            {
                MasterLayer.FeatureSource.Close();
            }
            if (MasterLayer.FeatureSource.Projection != null && MasterLayer.FeatureSource.Projection.IsOpen)
            {
                MasterLayer.FeatureSource.Projection.Close();
            }
            foreach (var featureLayer in clippingLayerFeatureLayers)
            {
                if (featureLayer.IsOpen)
                {
                    featureLayer.Close();
                }
                if (featureLayer.FeatureSource.Projection != null && featureLayer.FeatureSource.Projection.IsOpen)
                {
                    featureLayer.FeatureSource.Projection.Close();
                }
            }

            var plugin = GisEditor.TaskManager.GetActiveTaskPlugins<ClipTaskPlugin>().FirstOrDefault();
            if (plugin != null)
            {
                InitializePlugin(plugin, clippingLayerFeatureLayers);
            }

            return plugin;
        }

        private void InitializePlugin(ClipTaskPlugin plugin, List<FeatureLayer> clippingLayerFeatureLayers)
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
            plugin.MasterLayerFeatureLayer = MasterLayer;
            plugin.ClippingLayerFeatureLayers = clippingLayerFeatureLayers;
            plugin.Wkt = Proj4Projection.ConvertProj4ToPrj(GisEditor.ActiveMap.DisplayProjectionParameters);
            plugin.ClippingType = CurrentClippingType.ClippingType;
        }

        private void SaveClippingLayer()
        {
            string tempDir = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, TempPath);
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            clippingLayerTempPath = Path.Combine(tempDir, "ClippingTemp.shp");

            Collection<Feature> clippingFeatures = null;
            var clipLayers = this.ClippingLayers.Where(l => l.IsSelected).Select(l => l.FeatureLayer);

            if (this.IsUseSelectedFeatures)
            {
                Collection<Feature> selectedFeatures = new Collection<Feature>();

                var selectionOverlay = GisEditor.SelectionManager.GetSelectionOverlay();

                if (selectionOverlay != null)
                {
                    var selectedFeaturesInThisLayer = selectionOverlay.HighlightFeatureLayer.InternalFeatures.Where(tmpFeature => clipLayers.Contains(tmpFeature.Tag));
                    foreach (var feature in selectedFeaturesInThisLayer)
                    {
                        selectedFeatures.Add(feature);
                    }
                }
                if (selectedFeatures.Count > 0)
                {
                    clippingFeatures = selectedFeatures;
                }
            }

            //we don't need columns from the clipping layers
            string projectionInWKT = Proj4Projection.ConvertProj4ToPrj(GisEditor.ActiveMap.DisplayProjectionParameters);
            FileExportInfo info = new FileExportInfo(clippingFeatures, new FeatureSourceColumn[] { new FeatureSourceColumn("None", "String", 10) }, clippingLayerTempPath, projectionInWKT);

            ShapeFileExporter exporter = new ShapeFileExporter();
            exporter.ExportToFile(info);
        }
    }
}