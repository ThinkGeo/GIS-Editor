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
using System.Globalization;
using System.IO;
using System.Windows;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BuildShapeFileModel : ViewModelBase
    {
        private string status;
        private string filePath;
        private static readonly string statusFormat = "{0}   {1}";

        public BuildShapeFileModel(string filePath)
        {
            this.filePath = filePath;
            Status = string.Format(CultureInfo.InvariantCulture, statusFormat, filePath, "Ready");
        }

        public string FilePath
        {
            get { return filePath; }
        }

        public string FileName
        {
            get { return Path.GetFileName(FilePath); }
        }

        public string Status
        {
            get { return status; }
            set
            {
                status = value;
                RaisePropertyChanged(()=>Status);
            }
        }

        public bool IsCancel
        { get; set; }

        public void BuildIndex()
        {
            ShapeFileFeatureSource.BuildingIndex += ShapeFileFeatureSource_BuildingIndex;
            ShapeFileFeatureLayer.BuildIndexFile(FilePath, BuildIndexMode.Rebuild);
            ShapeFileFeatureSource.BuildingIndex -= ShapeFileFeatureSource_BuildingIndex;

            string tempTargetIdxPath = Path.ChangeExtension(FilePath, "_tmp.idx");
            string tempTargetIdsPath = Path.ChangeExtension(FilePath, "_tmp.ids");
            if (File.Exists(tempTargetIdxPath)) { File.Delete(tempTargetIdxPath); }
            if (File.Exists(tempTargetIdsPath)) { File.Delete(tempTargetIdsPath); }
        }

        private void ShapeFileFeatureSource_BuildingIndex(object sender, BuildingIndexShapeFileFeatureSourceEventArgs e)
        {
            if (Application.Current != null && e.ShapePathFilename.Equals(filePath))
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!e.Cancel)
                    {
                        Status = string.Format(CultureInfo.InvariantCulture, statusFormat, filePath, e.CurrentRecordIndex * 100 / e.RecordCount + "%");
                    }
                }));
                if (IsCancel && !e.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}