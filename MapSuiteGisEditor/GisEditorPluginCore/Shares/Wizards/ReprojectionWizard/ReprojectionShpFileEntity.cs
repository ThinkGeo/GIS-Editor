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
using System.IO;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ReprojectionShpFileEntity : ViewModelBase
    {
        private bool isInternalProjectionDetermined;
        private ShapeFileFeatureLayer shpLayer;
        [NonSerialized]
        private RelayCommand<ReprojectionShpFileEntity> editCommand;
        [NonSerialized]
        private RelayCommand<ReprojectionShpFileEntity> deleteCommand;
        private Action<ReprojectionShpFileEntity> editAction;
        private Action<ReprojectionShpFileEntity> deleteAction;

        public ReprojectionShpFileEntity(string filePath, Action<ReprojectionShpFileEntity> edit, Action<ReprojectionShpFileEntity> delete)
        {
            shpLayer = new ShapeFileFeatureLayer(filePath);
            SetInternalProjection();
            editAction = edit;
            deleteAction = delete;
        }

        public ShapeFileFeatureLayer ShpLayer
        {
            get { return shpLayer; }
        }

        public string ShortName
        {
            get
            {
                return Path.GetFileName(shpLayer.ShapePathFilename);
            }
        }

        public bool IsInternalProjectionDetermined
        {
            get { return isInternalProjectionDetermined; }
            set
            {
                isInternalProjectionDetermined = value;
                RaisePropertyChanged(() => IsInternalProjectionDetermined);
            }
        }

        public string InternalProjection
        {
            get { return ((Proj4Projection)shpLayer.FeatureSource.Projection).InternalProjectionParametersString; }
            set
            {
                Proj4Projection projection = (Proj4Projection)shpLayer.FeatureSource.Projection;
                projection.InternalProjectionParametersString = value;
                projection.SyncProjectionParametersString();
                RaisePropertyChanged(() => InternalProjection);
            }
        }

        public string ExternalProjection
        {
            get { return ((Proj4Projection)shpLayer.FeatureSource.Projection).ExternalProjectionParametersString; }
            set
            {
                Proj4Projection projection = (Proj4Projection)shpLayer.FeatureSource.Projection;
                projection.ExternalProjectionParametersString = value;
                projection.SyncProjectionParametersString();
                RaisePropertyChanged(() => ExternalProjection);
            }
        }

        public RelayCommand<ReprojectionShpFileEntity> EditCommand
        {
            get
            {
                if (editCommand == null)
                {
                    editCommand = new RelayCommand<ReprojectionShpFileEntity>((entity) =>
                    {
                        if (editAction != null)
                        {
                            editAction(entity);
                        }
                    });
                }
                return editCommand;
            }
        }

        public RelayCommand<ReprojectionShpFileEntity> DeleteCommand
        {
            get
            {
                if (deleteCommand == null)
                {
                    deleteCommand = new RelayCommand<ReprojectionShpFileEntity>(entity =>
                    {
                        deleteAction?.Invoke(entity);
                    });
                }
                return deleteCommand;
            }
        }

        private void SetInternalProjection()
        {
            string prjPath = Path.ChangeExtension(shpLayer.ShapePathFilename, ".prj");
            var requireIndex = shpLayer.RequireIndex;
            shpLayer.RequireIndex = false;
            bool isDecimalDegree = LayerPluginHelper.IsDecimalDegree(shpLayer);
            shpLayer.RequireIndex = requireIndex;
            shpLayer.FeatureSource.Projection = new Proj4Projection();
            if (File.Exists(prjPath))
            {
                string wkt = File.ReadAllText(prjPath);
                try
                {
                    string proj4 = Proj4Projection.ConvertPrjToProj4(wkt);
                    Proj4Projection projection = (Proj4Projection)ShpLayer.FeatureSource.Projection;
                    projection.InternalProjectionParametersString = proj4;
                    projection.SyncProjectionParametersString();
                    IsInternalProjectionDetermined = true;
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    IsInternalProjectionDetermined = false;
                }
            }
            else if (isDecimalDegree)
            {
                Proj4Projection projection = (Proj4Projection)ShpLayer.FeatureSource.Projection;
                projection.InternalProjectionParametersString = Proj4Projection.GetEpsgParametersString(4326);
                projection.SyncProjectionParametersString();
                IsInternalProjectionDetermined = true;
            }
            else
            {
                IsInternalProjectionDetermined = false;
            }
        }
    }
}