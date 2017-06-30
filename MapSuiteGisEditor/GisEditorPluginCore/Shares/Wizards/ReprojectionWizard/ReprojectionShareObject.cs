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
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ReprojectionShareObject : WizardShareObject
    {
        private ObservableCollection<ReprojectionShpFileEntity> sourceFiles;
        private string outputFolder;
        private bool isExternalProjectionDetermined;
        [NonSerialized]
        private RelayCommand chooseTargetCommand;

        public ReprojectionShareObject()
        {
            sourceFiles = new ObservableCollection<ReprojectionShpFileEntity>();
        }

        public ObservableCollection<ReprojectionShpFileEntity> SourceFiles
        {
            get { return sourceFiles; }
        }

        public string OutputFolder
        {
            get { return outputFolder; }
            set
            {
                outputFolder = value;
                RaisePropertyChanged("OutputFolder");
            }
        }

        public string ExternalProjection
        {
            get
            {
                return sourceFiles.Count == 0 ? string.Empty : sourceFiles.FirstOrDefault().ExternalProjection;
            }
            set
            {
                foreach (var item in sourceFiles)
                {
                    item.ExternalProjection = value;
                }
                RaisePropertyChanged("ExternalProjection");
            }
        }

        public bool IsExternalProjectionDetermined
        {
            get { return isExternalProjectionDetermined; }
            set { isExternalProjectionDetermined = value; }
        }

        public RelayCommand ChooseTargetCommand
        {
            get
            {
                if (chooseTargetCommand == null)
                {
                    chooseTargetCommand = new RelayCommand(() =>
                    {
                        //var projectionWindow = new SelectProjectionWindow();

                        var projectionWindow = new ProjectionWindow("", "Please Select a Projection", "");
                        if (projectionWindow.ShowDialog().GetValueOrDefault())
                        {
                            ExternalProjection = projectionWindow.Proj4ProjectionParameters;
                            IsExternalProjectionDetermined = true;
                        }
                    });
                }
                return
                    chooseTargetCommand;
            }
        }

        public void LoadSourceFiles(IEnumerable<string> files)
        {
            foreach (var item in files)
            {
                sourceFiles.Add(new ReprojectionShpFileEntity(item, ChooseInternalProjection, DeleteItem));
            }
        }

        protected override void LoadToMapCore()
        {
            var layerPathFileNames = SourceFiles.Select(s => Path.Combine(OutputFolder, s.ShortName));
            //var shapeFileLayerPlugin = GisEditor.LayerManager.GetSortedPlugins<ShapeFileFeatureLayerPlugin>().FirstOrDefault();
            //if (shapeFileLayerPlugin != null)
            {
                var getLayersParameters = new GetLayersParameters();
                foreach (var item in layerPathFileNames.Where(f => File.Exists(f)))
                {
                    getLayersParameters.LayerUris.Add(new Uri(item));
                }
               // var layers = shapeFileLayerPlugin.GetLayers(getLayersParameters);
                var layers = GisEditor.LayerManager.GetLayers<ShapeFileFeatureLayer>(getLayersParameters);
                GisEditor.ActiveMap.Dispatcher.BeginInvoke(new Action(() =>
                {
                    GisEditor.ActiveMap.AddLayersBySettings(layers);
                    GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.LoadToMapCoreDescription));
                }));
            }
        }

        protected override TaskPlugin GetTaskPluginCore()
        {
            ReprojectTaskPlugin taskPlugin = new ReprojectTaskPlugin();
            foreach (var entity in SourceFiles)
            {
                taskPlugin.ShapePathFileNames.Add(entity.ShpLayer.ShapePathFilename, entity.InternalProjection);
            }

            taskPlugin.OutputPathFileName = OutputFolder;
            taskPlugin.TargetProjectionParameter = ExternalProjection;
            return taskPlugin;
        }

        private void DeleteItem(ReprojectionShpFileEntity entity)
        {
            sourceFiles.Remove(entity);
        }

        private void ChooseInternalProjection(ReprojectionShpFileEntity entity)
        {
            var projectionWindow = new ProjectionWindow("", "", "Apply For All");

            if (projectionWindow.ShowDialog().GetValueOrDefault())
            {
                if (projectionWindow.SyncProj4ProjectionForAll)
                {
                    foreach (var shpEntity in SourceFiles)
                    {
                        shpEntity.InternalProjection = projectionWindow.Proj4ProjectionParameters;
                        shpEntity.IsInternalProjectionDetermined = true;
                    }
                }
                else
                {
                    entity.InternalProjection = projectionWindow.Proj4ProjectionParameters;
                    entity.IsInternalProjectionDetermined = true;
                }
            }
        }
    }
}