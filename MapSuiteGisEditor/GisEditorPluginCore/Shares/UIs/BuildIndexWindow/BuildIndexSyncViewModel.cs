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
using System.Threading;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BuildIndexSyncViewModel : ViewModelBase
    {
        private int progress;
        private string name;
        private string fullName;
        private FeatureLayer featureLayer;
        private Exception buildingIndexError;
        private BuildIndexAdapter buildIndexAdapter;
        private CancellationTokenSource cancellationTokenSource;

        public BuildIndexSyncViewModel(FeatureLayer featureLayer, BuildIndexAdapter buildIndexAdapter)
        {
            this.featureLayer = featureLayer;
            this.buildIndexAdapter = buildIndexAdapter;
            var uri = buildIndexAdapter.LayerPlugin.GetUri(featureLayer);
            if (uri != null) this.FullName = uri.LocalPath;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public CancellationTokenSource CancellationTokenSource { get { return cancellationTokenSource; } }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string FullName
        {
            get { return fullName; }
            set
            {
                fullName = value;
                Name = value.Substring(value.LastIndexOf('\\') + 1);
            }
        }

        public int Progress
        {
            get { return progress; }
            set
            {
                progress = value;
                RaisePropertyChanged(()=>Progress);
            }
        }

        public Exception BuildingIndexError
        {
            get { return buildingIndexError; }
            set { buildingIndexError = value; }
        }

        public bool BuildIndex()
        {
            bool isFinished = false;
            buildIndexAdapter.BuildingIndex += BuildingIndex;
            try
            {
                buildIndexAdapter.BuildIndex(featureLayer);
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                BuildingIndexError = ex;
            }
            finally
            {
                buildIndexAdapter.BuildingIndex -= BuildingIndex;
                isFinished = true;
            }
            return isFinished;
        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }

        private void BuildingIndex(object sender, BuildingIndexEventArgs e)
        {
            Progress = e.CurrentRecordIndex * 100 / e.RecordCount;
        }
    }
}