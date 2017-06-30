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
    public class DissolveWizardShareObject : WizardShareObject
    {
        private Collection<string> matchColumns;
        private Collection<string> extraColumns;
        private Collection<OperatorPair> operatorPairs;
        private FeatureLayer selectedFeatureLayer;
        private string tempFilePath;
        private bool dissolveSelectedFeaturesOnly;
        private bool needAddToMap;

        public DissolveWizardShareObject()
        {
            matchColumns = new Collection<string>();
            extraColumns = new Collection<string>();
            operatorPairs = new Collection<OperatorPair>();
            needAddToMap = true;
        }

        public string WizardName
        {
            get { return "Dissolve"; }
        }

        public Collection<string> MatchColumns
        {
            get { return matchColumns; }
        }

        public Collection<string> ExtraColumns
        {
            get { return extraColumns; }
        }

        public Collection<OperatorPair> OperatorPairs
        {
            get { return operatorPairs; }
        }

        public FeatureLayer SelectedFeatureLayer
        {
            get { return selectedFeatureLayer; }
            set { selectedFeatureLayer = value; }
        }

        public bool DissolveSelectedFeaturesOnly
        {
            get { return dissolveSelectedFeaturesOnly; }
            set { dissolveSelectedFeaturesOnly = value; }
        }

        public bool NeedAddToMap
        {
            get { return needAddToMap; }
            set
            {
                needAddToMap = value;
                RaisePropertyChanged("NeedAddToMap");
            }
        }

        protected override TaskPlugin GetTaskPluginCore()
        {
            FeatureSource featureSource = null;
            if (DissolveSelectedFeaturesOnly)
            {
                SaveSelectedFeaturesToTempFile();
                featureSource = new ShapeFileFeatureSource(tempFilePath);
            }
            else
            {
                if (SelectedFeatureLayer.FeatureSource.IsOpen)
                {
                    SelectedFeatureLayer.FeatureSource.Close();
                }
                featureSource = SelectedFeatureLayer.FeatureSource;
            }

            var plugin = GisEditor.TaskManager.GetActiveTaskPlugins<DissolveTaskPlugin>().FirstOrDefault();
            if (plugin != null)
            {
                InitializePlugin(plugin, featureSource);
            }

            return plugin;
        }

        private void InitializePlugin(DissolveTaskPlugin plugin, FeatureSource featureSource)
        {
            plugin.Wkt = Proj4Projection.ConvertProj4ToPrj(GisEditor.ActiveMap.DisplayProjectionParameters);
            plugin.MatchColumns = MatchColumns;
            plugin.OperatorPairs = OperatorPairs;
            plugin.FeatureSource = featureSource;
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
        }

        protected override void LoadToMapCore()
        {
            ShapeFileFeatureLayerExtension.RemoveShapeFiles(tempFilePath);
            AddToMap();
        }

        private Collection<Feature> GetFeaturesToDissolve()
        {
            Collection<Feature> featuresToDissolve = new Collection<Feature>();

            var selectionOverlay = GisEditor.SelectionManager.GetSelectionOverlay();
            if (selectionOverlay != null)
            {
                var selectedFeaturesInThisLayer = selectionOverlay.HighlightFeatureLayer.InternalFeatures
                    .Where(tmpFeature => tmpFeature.Tag == this.SelectedFeatureLayer);
                foreach (var feature in selectedFeaturesInThisLayer)
                {
                    featuresToDissolve.Add(feature);
                }
            }

            return featuresToDissolve;
        }

        private void SaveSelectedFeaturesToTempFile()
        {
            string tempDir = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, TempPath);
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            tempFilePath = Path.Combine(tempDir, "DissolveTemp.shp");

            Collection<Feature> featuresToDissolve = GetFeaturesToDissolve();

            string projectionInWKT = Proj4Projection.ConvertProj4ToPrj(GisEditor.ActiveMap.DisplayProjectionParameters);
            FileExportInfo info = new FileExportInfo(featuresToDissolve, GetColumnsOfSelectedLayer(), tempFilePath, projectionInWKT);

            ShapeFileExporter exporter = new ShapeFileExporter();
            exporter.ExportToFile(info);
        }

        private IEnumerable<FeatureSourceColumn> GetColumnsOfSelectedLayer()
        {
            Collection<FeatureSourceColumn> columns = new Collection<FeatureSourceColumn>();
            SelectedFeatureLayer.SafeProcess(() =>
            {
                columns = SelectedFeatureLayer.FeatureSource.GetColumns();
            });
            //SelectedFeatureLayer.Open();
            //var columns = SelectedFeatureLayer.FeatureSource.GetColumns();
            //SelectedFeatureLayer.Close();

            return columns;
        }

        private void AddToMap()
        {
            if (NeedAddToMap)
            {
                var getLayersParameters = new GetLayersParameters();
                getLayersParameters.LayerUris.Add(new Uri(OutputPathFileName));
                var newLayers = GisEditor.LayerManager.GetLayers<ShapeFileFeatureLayer>(getLayersParameters);
                GisEditor.ActiveMap.Dispatcher.BeginInvoke(new Action(() =>
                {
                    GisEditor.ActiveMap.AddLayersBySettings(newLayers);
                    GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.AddToMapDescription));
                }));
            }
        }
    }
}