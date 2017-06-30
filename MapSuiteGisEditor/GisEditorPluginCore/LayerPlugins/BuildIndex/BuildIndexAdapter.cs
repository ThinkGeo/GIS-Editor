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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public abstract class BuildIndexAdapter
    {
        private const int maxSizeForSyncBuilding = 31457280;
        private const int maxCountForSyncBuildingPoints = 50000;

        private FeatureLayerPlugin layerPlugin;
        private Dictionary<string, Action> backgroundBuildingCallbacks;

        public event EventHandler<BuildingIndexEventArgs> BuildingIndex;

        protected BuildIndexAdapter(FeatureLayerPlugin layerPlugin)
        {
            this.layerPlugin = layerPlugin;
            this.backgroundBuildingCallbacks = new Dictionary<string, Action>();
        }

        public FeatureLayerPlugin LayerPlugin
        {
            get { return layerPlugin; }
        }

        public void BuildIndex(FeatureLayer featureLayer)
        {
            BuildIndexCore(featureLayer);
        }

        protected abstract void BuildIndexCore(FeatureLayer featureLayer);

        protected bool HasIndex(FeatureLayer featureLayer)
        {
            return HasIndexCore(featureLayer);
        }

        protected virtual bool HasIndexCore(FeatureLayer featureLayer)
        {
            bool hasIndex = false;
            if (!string.IsNullOrEmpty(layerPlugin.ExtensionFilter))
            {
                var uri = layerPlugin.GetUri(featureLayer);
                if (uri != null && uri.Scheme.Equals("file") && File.Exists(uri.LocalPath))
                {
                    hasIndex = new string[] { ".idx", ".ids" }.All(ex =>
                    {
                        string indexPathFileName = Path.ChangeExtension(uri.LocalPath, ex);
                        return File.Exists(indexPathFileName);
                    });
                }
                return hasIndex;
            }
            else return false;
        }

        public void SetRequireIndex(FeatureLayer featureLayer, bool requireIndex)
        {
            SetRequireIndexCore(featureLayer, requireIndex);
        }

        protected abstract void SetRequireIndexCore(FeatureLayer featureLayer, bool requireIndex);

        protected virtual void OnBuildingIndex(BuildingIndexEventArgs args)
        {
            EventHandler<BuildingIndexEventArgs> handler = BuildingIndex;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        public void BuildIndex(IEnumerable<FeatureLayer> featureLayers)
        {
            Collection<FeatureLayer> pendingLayersToBuildIndex = new Collection<FeatureLayer>();
            Collection<FeatureLayer> pendingLayersToBuildInBackground = new Collection<FeatureLayer>();
            BuildLargeIndexMode buildLargeIndexMode = BuildLargeIndexMode.Unknown;

            DisableNoIndexFeatureLayers(featureLayers, pendingLayersToBuildIndex);
            BuildIndexSync(pendingLayersToBuildIndex, pendingLayersToBuildInBackground, ref buildLargeIndexMode);
            BuildIndexAsync(pendingLayersToBuildInBackground);
        }

        private void BuildIndexAsync(Collection<FeatureLayer> pendingLayersToBuildInBackground)
        {
            Collection<BuildIndexTaskPlugin> taskPlugins = new Collection<BuildIndexTaskPlugin>();
            foreach (var featureLayer in pendingLayersToBuildInBackground.Distinct())
            {
                BuildIndexTaskPlugin taskPlugin = new BuildIndexTaskPlugin();
                taskPlugin.FeatureLayer = featureLayer;
                taskPlugin.BuildIndexAdapter = this;
                taskPlugins.Add(taskPlugin);
            }

            foreach (var task in taskPlugins)
            {
                GisEditor.TaskManager.UpdatingProgress -= new EventHandler<UpdatingTaskProgressEventArgs>(TaskManager_UpdatingProgress);
                GisEditor.TaskManager.UpdatingProgress += new EventHandler<UpdatingTaskProgressEventArgs>(TaskManager_UpdatingProgress);
                string taskToken = GisEditor.TaskManager.RunTask(task);
                backgroundBuildingCallbacks.Add(taskToken, task.LoadToMap);
            }
        }

        private void BuildIndexSync(Collection<FeatureLayer> pendingLayersToBuildIndex, Collection<FeatureLayer> pendingLayersToBuildInBackground, ref BuildLargeIndexMode buildLargeIndexMode)
        {
            bool applyForAll = false;
            bool canceled = false;

            Collection<FeatureLayer> notAddLayers = new Collection<FeatureLayer>();
            if (pendingLayersToBuildIndex.Count > 0)
            {
                foreach (var featureLayer in pendingLayersToBuildIndex)
                {
                    if (!applyForAll)
                    {
                        BuildIndexFileDialog buildIndexDialog = new BuildIndexFileDialog();
                        buildIndexDialog.HasMultipleFiles = pendingLayersToBuildIndex.Count > 1;
                        if (buildIndexDialog.ShowDialog().GetValueOrDefault())
                        {
                            canceled = false;
                            if (buildIndexDialog.BuildIndexFileMode == BuildIndexFileMode.BuildAll)
                            {
                                applyForAll = true;
                            }
                        }
                        else
                        {
                            canceled = true;
                            if (buildIndexDialog.BuildIndexFileMode == BuildIndexFileMode.DoNotBuildAll)
                            {
                                applyForAll = true;
                            }
                        }
                    }

                    if (applyForAll && canceled) break;
                    else if (canceled) continue;

                    if (CheckIsBuildSync(featureLayer))
                    {
                        BuildIndexSync(featureLayer);
                    }
                    else
                    {
                        if (!applyForAll || buildLargeIndexMode == BuildLargeIndexMode.Unknown)
                        {
                            BuildLargeIndexSyncWindow buildLargeIndexWindow = new BuildLargeIndexSyncWindow();
                            buildLargeIndexWindow.FileName = featureLayer.Name;
                            if (buildLargeIndexWindow.ShowDialog().GetValueOrDefault())
                            {
                                buildLargeIndexMode = buildLargeIndexWindow.BuildLargeIndexMode;
                            }
                        }

                        switch (buildLargeIndexMode)
                        {
                            case BuildLargeIndexMode.NormalBuild:
                                BuildIndexSync(featureLayer);
                                break;

                            case BuildLargeIndexMode.DoNotAdd:
                                featureLayer.IsVisible = false;
                                notAddLayers.Add(featureLayer);
                                break;

                            case BuildLargeIndexMode.BackgroundBuild:
                                featureLayer.IsVisible = false;
                                pendingLayersToBuildInBackground.Add(featureLayer);
                                break;

                            case BuildLargeIndexMode.DoNotBuild:
                            default:
                                break;
                        }
                    }
                }
            }

            if (notAddLayers.Count > 0) notAddLayers.ForEach(l => pendingLayersToBuildIndex.Remove(l));
        }

        private void DisableNoIndexFeatureLayers(IEnumerable<FeatureLayer> featureLayers, Collection<FeatureLayer> pendingLayersToBuildIndex)
        {
            foreach (var featureLayer in featureLayers)
            {
                if (!HasIndex(featureLayer))
                {
                    SetRequireIndex(featureLayer, false);
                    pendingLayersToBuildIndex.Add(featureLayer);
                }
                else
                {
                    SetRequireIndex(featureLayer, true);
                }
            }
        }

        private bool CheckIsBuildSync(FeatureLayer featureLayer)
        {
            var uri = layerPlugin.GetUri(featureLayer);
            if (uri != null)
            {
                string pathFileName = uri.LocalPath;
                SimpleShapeType shapeType = layerPlugin.GetFeatureSimpleShapeType(featureLayer);
                if (shapeType != SimpleShapeType.Unknown)
                {
                    if (shapeType == SimpleShapeType.Point)
                    {
                        bool isSync = true;
                        featureLayer.SafeProcess(() =>
                        {
                            isSync = featureLayer.QueryTools.GetCount() < maxCountForSyncBuildingPoints;
                        });
                        return isSync;
                    }
                    else return FileSizeLessOrEqualThanSyncLimit(pathFileName);
                }
                else
                {
                    return FileSizeLessOrEqualThanSyncLimit(pathFileName);
                }
            }
            else return false;
        }

        private static bool FileSizeLessOrEqualThanSyncLimit(string pathFileName)
        {
            FileInfo shapeFileInfo = new FileInfo(pathFileName);
            return shapeFileInfo.Length <= maxSizeForSyncBuilding;
        }

        private void BuildIndexSync(FeatureLayer featureLayer)
        {
            BuildIndexSyncWindow buildIndexWindow = new BuildIndexSyncWindow(featureLayer, this);
            buildIndexWindow.ShowDialog();
            if (buildIndexWindow.BuildingIndexError == null)
            {
                featureLayer.SafeProcess(() => SetRequireIndex(featureLayer, true));
            }
        }

        private void TaskManager_UpdatingProgress(object sender, UpdatingTaskProgressEventArgs e)
        {
            if (e.TaskState == TaskState.Completed
                && backgroundBuildingCallbacks.ContainsKey(e.TaskToken)
                && backgroundBuildingCallbacks[e.TaskToken] != null)
            {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        backgroundBuildingCallbacks[e.TaskToken]();
                        backgroundBuildingCallbacks.Remove(e.TaskToken);
                    });
                }
                else
                {
                    backgroundBuildingCallbacks[e.TaskToken]();
                    backgroundBuildingCallbacks.Remove(e.TaskToken);
                }
            }
            else if (e.TaskState == TaskState.Canceled
                && backgroundBuildingCallbacks.ContainsKey(e.TaskToken))
            {
                backgroundBuildingCallbacks.Remove(e.TaskToken);
            }
        }
    }
}