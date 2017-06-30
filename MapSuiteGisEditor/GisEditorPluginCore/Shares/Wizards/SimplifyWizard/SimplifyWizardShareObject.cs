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
    public class SimplifyWizardShareObject : WizardShareObject
    {
        private string tempFilePath;
        private string selectedDistanceUnit;
        private double simplificationTolerance;
        private bool currentLayerHasSelectedFeatures;
        private bool onlySimplifySelectedFeatures;
        private bool preserveTopology;
        private bool addToMap;
        private bool isDecimalDegrees;
        private FeatureLayer selectedFeatureLayer;
        private DistanceUnitToStringConverter converter;
        private ObservableCollection<string> distanceUnits;
        private ObservableCollection<FeatureLayer> featureLayers;

        public SimplifyWizardShareObject()
        {
            converter = new DistanceUnitToStringConverter();
            InitSelectableFeatureLayers();

            InitDistance();

            OnlySimplifySelectedFeatures = true;
            AddToMap = true;
        }

        public string WizardName
        {
            get { return "Simplify"; }
        }

        private void InitDistance()
        {
            distanceUnits = new ObservableCollection<string>(Enum.GetValues(typeof(DistanceUnit)).Cast<object>().Select(value => (string)converter.Convert(value, null, null, null)));

            if (GisEditor.ActiveMap.MapUnit == GeographyUnit.DecimalDegree)
            {
                DistanceUnits.Insert(0, "Decimal Degrees");

                SelectedDistanceUnit = DistanceUnits[0];
                SimplificationTolerance = 0.01;

                IsDecimalDegrees = true;
            }
            else
            {
                SelectedDistanceUnit = converter.Convert(DistanceUnit.Kilometer, null, null, null).ToString();
                SimplificationTolerance = 5;
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

                var selectionOverlay = GisEditor.SelectionManager.GetSelectionOverlay();
                if (selectionOverlay != null)
                {
                    CurrentLayerHasSelectedFeatures = selectionOverlay.HighlightFeatureLayer.InternalFeatures.Any(f => f.Tag == value);
                }
            }
        }

        public bool OnlySimplifySelectedFeatures
        {
            get { return onlySimplifySelectedFeatures; }
            set { onlySimplifySelectedFeatures = value; }
        }

        public bool PreserveTopology
        {
            get { return preserveTopology; }
            set { preserveTopology = value; }
        }

        public bool CurrentLayerHasSelectedFeatures
        {
            get { return currentLayerHasSelectedFeatures; }
            set
            {
                currentLayerHasSelectedFeatures = value;
                RaisePropertyChanged("CurrentLayerHasSelectedFeatures");

                if (!value)
                {
                    OnlySimplifySelectedFeatures = false;
                }
            }
        }

        public double SimplificationTolerance
        {
            get { return simplificationTolerance; }
            set
            {
                simplificationTolerance = value;
                RaisePropertyChanged("SimplificationTolerance");
            }
        }

        public ObservableCollection<string> DistanceUnits
        {
            get { return distanceUnits; }
        }

        public string SelectedDistanceUnit
        {
            get { return selectedDistanceUnit; }
            set { selectedDistanceUnit = value; }
        }

        public bool AddToMap
        {
            get { return addToMap; }
            set { addToMap = value; }
        }

        public bool IsDecimalDegrees
        {
            get { return isDecimalDegrees; }
            set { isDecimalDegrees = value; }
        }

        private void InitSelectableFeatureLayers()
        {
            featureLayers = new ObservableCollection<FeatureLayer>();

            if (GisEditor.ActiveMap != null)
            {
                var allFeatureLayers = GisEditor.ActiveMap.GetFeatureLayers(true).Where(layer =>
                {
                    if (layer is ShapeFileFeatureLayer)
                    {
                        ShapeFileFeatureLayerPlugin layerPlugin = new ShapeFileFeatureLayerPlugin();
                        var shapeType = layerPlugin.GetFeatureSimpleShapeType(layer);
                        if (shapeType == SimpleShapeType.Point)
                        {
                            return false;
                        }
                    }

                    return true;
                });

                foreach (var featureLayer in allFeatureLayers)
                {
                    FeatureLayers.Add(featureLayer);
                }
            }
        }

        protected override TaskPlugin GetTaskPluginCore()
        {
            string tempDir = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, TempPath);
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            tempFilePath = Path.Combine(tempDir, "SimplifyTemp.shp");

            FeatureSource featureSource = new ShapeFileFeatureSource(tempFilePath);
            if (CurrentLayerHasSelectedFeatures && OnlySimplifySelectedFeatures)
            {
                SaveFeaturesToTempFile(tempFilePath);
                featureSource = new ShapeFileFeatureSource(tempFilePath);
            }
            else
            {
                featureSource = SelectedFeatureLayer.FeatureSource;
            }

            if (featureSource.IsOpen) featureSource.Close();
            if (featureSource.Projection != null) featureSource.Projection.Close();

            var plugin = GisEditor.TaskManager.GetActiveTaskPlugins<SimplifyTaskPlugin>().FirstOrDefault();
            if (plugin != null)
            {
                InitializePlugin(plugin, featureSource);
            }

            return plugin;
        }

        private void InitializePlugin(SimplifyTaskPlugin plugin, FeatureSource featureSource)
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
            plugin.FeatureSource = featureSource;
            plugin.PreserveTopology = PreserveTopology;
            plugin.SelectedDistanceUnit = SelectedDistanceUnit;
            plugin.SimplificationTolerance = SimplificationTolerance;
            plugin.MapUnit = GisEditor.ActiveMap.MapUnit;
            plugin.DisplayProjectionParameters = Proj4Projection.ConvertProj4ToPrj(GisEditor.ActiveMap.DisplayProjectionParameters);
        }

        protected override void LoadToMapCore()
        {
            if (File.Exists(OutputPathFileName))
            {
                AddSimplifiedFileToMap();
            }

            ShapeFileFeatureLayerExtension.RemoveShapeFiles(tempFilePath);
        }

        private void AddSimplifiedFileToMap()
        {
            var getLayersParameters = new GetLayersParameters();
            getLayersParameters.LayerUris.Add(new Uri(OutputPathFileName));
            var layers = GisEditor.LayerManager.GetLayers<ShapeFileFeatureLayer>(getLayersParameters);
            if (layers != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    GisEditor.ActiveMap.AddLayersBySettings(layers);
                    GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.AddSimplifiedFileToMapDescription));
                }));
            }
        }

        private void SaveFeaturesToTempFile(string tempFilePath)
        {
            var featuresToSimplify = GetFeaturesToSimplify();

            string projectionInWKT = Proj4Projection.ConvertProj4ToPrj(GisEditor.ActiveMap.DisplayProjectionParameters);
            FileExportInfo info = new FileExportInfo(featuresToSimplify, GetColumns(), tempFilePath, projectionInWKT);

            ShapeFileExporter exporter = new ShapeFileExporter();
            exporter.ExportToFile(info);
        }

        private IEnumerable<FeatureSourceColumn> GetColumns()
        {
            Collection<FeatureSourceColumn> results = new Collection<FeatureSourceColumn>();
            SelectedFeatureLayer.SafeProcess(() =>
            {
                results = SelectedFeatureLayer.FeatureSource.GetColumns();
            });
            //SelectedFeatureLayer.Open();
            //var results = SelectedFeatureLayer.FeatureSource.GetColumns();
            //SelectedFeatureLayer.Close();

            return results;
        }

        private IEnumerable<Feature> GetFeaturesToSimplify()
        {
            IEnumerable<Feature> results = GisEditor.SelectionManager.GetSelectedFeatures().Where(f => f.Tag == SelectedFeatureLayer);

            results = results.Where(f =>
            {
                var shape = f.GetShape();
                return shape is AreaBaseShape || shape is LineBaseShape;
            });
            return results;
        }
    }
}