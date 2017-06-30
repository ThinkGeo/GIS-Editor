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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BufferWizardShareObject : WizardShareObject
    {
        private bool addToMap;
        private bool onlyBufferSelectedFeatures;
        private bool currentLayerHasSelectedFeatures;
        private double distance;
        private string busyContent = "Buffering......";
        private string tempShpFilePath;
        private int smoothness;
        private int[] smoothnessValues;
        private FeatureLayer selectedFeatureLayer;
        private ObservableCollection<FeatureLayer> featureLayers;
        private DistanceUnit selectedDistanceUnit;
        private ObservableCollection<string> distanceUnits;
        private BufferCapType capStyle;
        private bool needDissolve;

        public BufferWizardShareObject()
            : base()
        {
            tempShpFilePath = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "BufferTaskTemp.shp");
            InitSelectableFeatureLayers();

            smoothnessValues = new int[] { 3, 4, 5, 6, 7, 8, 9, 10 };

            var converter = new DistanceUnitToStringConverter();
            distanceUnits = new ObservableCollection<string>(Enum.GetValues(typeof(DistanceUnit)).Cast<object>().Select(value => (string)converter.Convert(value, null, null, null)));

            OnlyBufferSelectedFeatures = true;
            AddToMap = true;

            Distance = 20;
            Smoothness = 8;

            CapStyle = BufferCapType.Round;
            SelectedDistanceUnit = DistanceUnit.Meter;
        }

        public string WizardName
        {
            get { return "Buffer"; }
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
                    CurrentLayerHasSelectedFeatures = selectionOverlay.HighlightFeatureLayer
                        .InternalFeatures.Any(f => f.Tag == value);
                }

                RaisePropertyChanged("NeedToShowEndCap");
            }
        }

        public bool OnlyBufferSelectedFeatures
        {
            get { return onlyBufferSelectedFeatures; }
            set
            {
                onlyBufferSelectedFeatures = value;
                RaisePropertyChanged("OnlyBufferSelectedFeatures");
            }
        }

        public bool NeedToShowEndCap
        {
            get
            {
                var featureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(SelectedFeatureLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
                if (featureLayerPlugin != null)
                {
                    return featureLayerPlugin.GetFeatureSimpleShapeType(SelectedFeatureLayer) == SimpleShapeType.Line;
                }
                else
                {
                    return false;
                }
            }
        }

        public double Distance
        {
            get { return distance; }
            set { distance = value; }
        }

        public ObservableCollection<string> DistanceUnits
        {
            get { return distanceUnits; }
        }

        public DistanceUnit SelectedDistanceUnit
        {
            get { return selectedDistanceUnit; }
            set { selectedDistanceUnit = value; }
        }

        public bool NeedDissolve
        {
            get { return needDissolve; }
            set { needDissolve = value; RaisePropertyChanged("NeedDissolve"); }
        }

        public int Smoothness
        {
            get { return smoothness; }
            set
            {
                smoothness = value;
                RaisePropertyChanged("Smoothness");
            }
        }

        public BufferCapType CapStyle
        {
            get { return capStyle; }
            set { capStyle = value; }
        }

        public ObservableCollection<FeatureLayer> FeatureLayers { get { return featureLayers; } }



        public bool AddToMap
        {
            get { return addToMap; }
            set { addToMap = value; }
        }

        public int[] SmoothnessValues { get { return smoothnessValues; } }

        public bool CurrentLayerHasSelectedFeatures
        {
            get { return currentLayerHasSelectedFeatures; }
            set
            {
                currentLayerHasSelectedFeatures = value;
                RaisePropertyChanged("CurrentLayerHasSelectedFeatures");

                if (!value)
                {
                    OnlyBufferSelectedFeatures = false;
                }
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

        protected override void LoadToMapCore()
        {
            if (File.Exists(OutputPathFileName))
            {
                var getLayersParameters = new GetLayersParameters();
                getLayersParameters.LayerUris.Add(new Uri(OutputPathFileName));
                var layers = GisEditor.LayerManager.GetLayers<ShapeFileFeatureLayer>(getLayersParameters);
                if (layers != null)
                {
                    if (Application.Current != null)
                    {
                        //we need to use the dispatcher here, because the "Buffer" method is called by another thread.
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
            }
        }

        protected override TaskPlugin GetTaskPluginCore()
        {
            BufferTaskPlugin result = GisEditor.TaskManager.GetActiveTaskPlugins<BufferTaskPlugin>().FirstOrDefault();
            if (result != null)
            {
                PrepareTaskParameters(result);
            }
            return result;
        }

        private void InitSelectableFeatureLayers()
        {
            featureLayers = new ObservableCollection<FeatureLayer>();
            if (GisEditor.ActiveMap != null)
            {
                GisEditor.ActiveMap.GetFeatureLayers(true).ForEach(l => featureLayers.Add(l));
            }
        }

        private void PrepareTaskParameters(BufferTaskPlugin plugin)
        {
            FeatureSource featureSource = null;
            if (CurrentLayerHasSelectedFeatures && OnlyBufferSelectedFeatures)
            {
                ShapeFileExporter shpExporter = new ShapeFileExporter();
                string projectionInWKT = Proj4Projection.ConvertProj4ToPrj(GisEditor.ActiveMap.DisplayProjectionParameters);

                if (GisEditor.SelectionManager.GetSelectionOverlay() != null)
                {
                    var features = GisEditor.SelectionManager.GetSelectionOverlay().HighlightFeatureLayer
                            .InternalFeatures.Where(f => f.Tag == SelectedFeatureLayer);
                    FileExportInfo info = new FileExportInfo(features, GetColumns(), tempShpFilePath, projectionInWKT);
                    shpExporter.ExportToFile(info);
                }
                featureSource = new ShapeFileFeatureSource(tempShpFilePath);
            }
            else
            {
                featureSource = SelectedFeatureLayer.FeatureSource;
            }

            if (featureSource.IsOpen)
            {
                featureSource.Close();
            }

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
            plugin.Distance = Distance;
            plugin.Smoothness = Smoothness;
            plugin.Capstyle = CapStyle;
            plugin.MapUnit = GisEditor.ActiveMap.MapUnit;
            plugin.DistanceUnit = SelectedDistanceUnit;
            plugin.DisplayProjectionParameters = GisEditor.ActiveMap.DisplayProjectionParameters;
            plugin.Dissolve = NeedDissolve;
        }

        private IEnumerable<FeatureSourceColumn> GetColumns()
        {
            Collection<FeatureSourceColumn> results = new Collection<FeatureSourceColumn>();
            SelectedFeatureLayer.SafeProcess(() =>
            {
                SelectedFeatureLayer.FeatureSource.GetColumns().ForEach(c => results.Add(c));
            });

            return results;
        }
    }
}
