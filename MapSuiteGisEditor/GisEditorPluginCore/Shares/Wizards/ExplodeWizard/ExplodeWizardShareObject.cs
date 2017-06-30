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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ExplodeWizardShareObject : WizardShareObject
    {
        private ObservableCollection<FeatureLayer> featureLayers;
        private FeatureLayer selectedFeatureLayer;

        public ExplodeWizardShareObject()
        {
            ExtensionFilter = "Shape files(*.shp)|*.shp";
            InitSelectableFeatureLayers();
        }

        public string WizardName
        {
            get { return "Uncombine"; }
        }

        public ObservableCollection<FeatureLayer> FeatureLayers { get { return featureLayers; } }

        public FeatureLayer SelectedFeatureLayer
        {
            get { return selectedFeatureLayer; }
            set
            {
                selectedFeatureLayer = value;

                var selectionOverlay = GisEditor.SelectionManager.GetSelectionOverlay();
                RaisePropertyChanged("NeedToShowEndCap");
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
                        //we need to use the dispatcher here, because the "Explode" method is called by another thread.
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
            ExplodeTaskPlugin result = GisEditor.TaskManager.GetActiveTaskPlugins<ExplodeTaskPlugin>().FirstOrDefault();
            if (result != null)
            {
                PrepareTaskParameters(result);
            }
            return result;
        }

        private void PrepareTaskParameters(ExplodeTaskPlugin plugin)
        {
            FeatureSource featureSource = SelectedFeatureLayer.FeatureSource;

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
            plugin.DisplayProjectionParameters = GisEditor.ActiveMap.DisplayProjectionParameters;
        }

        private void InitSelectableFeatureLayers()
        {
            featureLayers = new ObservableCollection<FeatureLayer>();
            if (GisEditor.ActiveMap != null)
            {
                GisEditor.ActiveMap.GetFeatureLayers(true).ForEach(l => featureLayers.Add(l));
            }
        }
    }
}
