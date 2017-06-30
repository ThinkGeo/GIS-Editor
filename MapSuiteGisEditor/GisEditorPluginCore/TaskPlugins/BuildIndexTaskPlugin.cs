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
using System.IO;
using System.Linq;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BuildIndexTaskPlugin : TaskPlugin
    {
        private FeatureLayer featureLayer;
        private BuildIndexAdapter buildIndexAdapter;

        public BuildIndexTaskPlugin()
        { }

        public BuildIndexAdapter BuildIndexAdapter
        {
            get { return buildIndexAdapter; }
            set { buildIndexAdapter = value; }
        }

        public FeatureLayer FeatureLayer
        {
            get { return featureLayer; }
            set { featureLayer = value; }
        }

        protected override void LoadCore()
        {
            var uri = buildIndexAdapter.LayerPlugin.GetUri(featureLayer);
            if (uri != null)
            {
                Name = Description = GisEditor.LanguageManager.GetStringResource("BuildIndexTaskPluginName") + " " + Path.GetFileName(uri.LocalPath);
            }
        }

        protected override void RunCore()
        {
            if (buildIndexAdapter != null && FeatureLayer != null)
            {
                buildIndexAdapter.BuildingIndex += new EventHandler<BuildingIndexEventArgs>(IndexAdapter_BuildingIndex);
                try
                {
                    OnUpdatingProgress(new UpdatingTaskProgressEventArgs(TaskState.Updating) { Message = GisEditor.LanguageManager.GetStringResource("BuildIndexTaskPluginMessage") });
                    buildIndexAdapter.BuildIndex(FeatureLayer);
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
                finally
                {
                    buildIndexAdapter.BuildingIndex -= IndexAdapter_BuildingIndex;
                }
            }
        }

        public void LoadToMap()
        {
            LoadToMapCore();
        }

        protected virtual void LoadToMapCore()
        {
            bool needRefresh = false;
            foreach (var overlayToRefresh in GetOverlaysToRefresh())
            {
                var layersToRefresh = overlayToRefresh.Layers.OfType<FeatureLayer>();
                foreach (var layerToRefresh in layersToRefresh)
                {
                    if (layerToRefresh == FeatureLayer)
                    {
                        layerToRefresh.SafeProcess(() =>
                        {
                            buildIndexAdapter.SetRequireIndex(layerToRefresh, true);
                            layerToRefresh.IsVisible = true;
                            needRefresh = true;
                        });
                    }
                }

                if (needRefresh)
                {
                    overlayToRefresh.Invalidate();
                }
            }

            if (needRefresh)
            {
                GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, "LoadToMapCore"));
            }
        }

        private IEnumerable<LayerOverlay> GetOverlaysToRefresh()
        {
            var overlaysToRefresh = GisEditor.DockWindowManager.DocumentWindows.Select(item => item.Content).OfType<GisEditorWpfMap>()
                                                            .SelectMany(tmpMap => tmpMap.Overlays.OfType<LayerOverlay>()
                                                            .Where(tmpOverlay => tmpOverlay.Layers.OfType<FeatureLayer>()
                                                            .Any(tmpLayer =>
                                                            {
                                                                var tmpIndexAdapter = GisEditor.LayerManager.GetLayerPlugins(tmpLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
                                                                if (tmpIndexAdapter != null)
                                                                {
                                                                    var tmpUri = tmpIndexAdapter.GetUri(tmpLayer);
                                                                    var uri = buildIndexAdapter.LayerPlugin.GetUri(featureLayer);
                                                                    if (tmpUri != null && uri != null)
                                                                    {
                                                                        return tmpUri.LocalPath.Equals(uri.LocalPath
                                                                         , StringComparison.OrdinalIgnoreCase);
                                                                    }
                                                                    else return false;
                                                                }
                                                                else return false;
                                                            })));
            return overlaysToRefresh;
        }

        private void IndexAdapter_BuildingIndex(object sender, BuildingIndexEventArgs e)
        {
            int progressPercentage = e.CurrentRecordIndex * 100 / e.RecordCount;
            var updatingArgs = new UpdatingTaskProgressEventArgs(TaskState.Updating, progressPercentage);
            updatingArgs.Current = e.CurrentRecordIndex;
            updatingArgs.UpperBound = e.RecordCount;
            OnUpdatingProgress(updatingArgs);
            if (updatingArgs.TaskState == TaskState.Canceled)
            {
                e.Cancel = true;
            }
        }
    }
}