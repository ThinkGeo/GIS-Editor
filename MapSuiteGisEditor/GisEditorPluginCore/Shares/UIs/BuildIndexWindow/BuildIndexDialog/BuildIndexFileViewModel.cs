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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BuildIndexFileViewModel : ViewModelBase
    {
        private static bool isCancelled;
        private bool hasMultipleFiles;
        private string contentText;
        private BuildIndexFileMode buildIndexFileMode;
        [NonSerialized]
        private RelayCommand yesCommand;
        [NonSerialized]
        private RelayCommand noCommand;
        [NonSerialized]
        private RelayCommand yesForAllCommand;
        [NonSerialized]
        private RelayCommand notForAllCommand;

        public BuildIndexFileViewModel()
        {
            isCancelled = false;

            contentText = GisEditor.LanguageManager.GetStringResource("BuildIndexFileViewModelLayerNotHaveIndexFileContent");
            yesCommand = new RelayCommand(() =>
            {
                BuildIndexFileMode = BuildIndexFileMode.Build;
                SendMessage();
            });

            noCommand = new RelayCommand(() =>
            {
                BuildIndexFileMode = BuildIndexFileMode.DoNotBuild;
                SendMessage(false);
            });

            yesForAllCommand = new RelayCommand(() =>
            {
                BuildIndexFileMode = BuildIndexFileMode.BuildAll;
                SendMessage();
            });

            notForAllCommand = new RelayCommand(() =>
            {
                BuildIndexFileMode = BuildIndexFileMode.DoNotBuildAll;
                SendMessage(false);
            });
        }

        public BuildIndexFileMode BuildIndexFileMode
        {
            get { return buildIndexFileMode; }
            set
            {
                buildIndexFileMode = value;
                RaisePropertyChanged(()=>BuildIndexFileMode);
            }
        }

        public string ContentText
        {
            get { return contentText; }
            set
            {
                contentText = value;
                RaisePropertyChanged(()=>ContentText);
            }
        }

        public bool HasMultipleFiles
        {
            get { return hasMultipleFiles; }
            set
            {
                hasMultipleFiles = value;
                RaisePropertyChanged(()=>HasMultipleFiles);
            }
        }

        public RelayCommand YesCommand { get { return yesCommand; } }

        public RelayCommand NoCommand { get { return noCommand; } }

        public RelayCommand YesForAllCommand { get { return yesForAllCommand; } }

        public RelayCommand NotForAllCommand { get { return notForAllCommand; } }

        public static void BuildIndex(ShapeFileFeatureLayer layer)
        {
            string sourceIdsPath = Path.ChangeExtension(layer.IndexPathFilename, ".ids");

            if (layer.RequireIndex && File.Exists(layer.IndexPathFilename) && File.Exists(sourceIdsPath))
            {
                lock (layer)
                {
                    layer.Close();
                    layer.RequireIndex = false;
                    layer.Open();
                }
            }

            StatusBar.GetInstance().Cancelled += new EventHandler<CancelEventArgs>(BuildIndexFileDialog_Cancelled);
            ShapeFileFeatureSource.BuildingIndex += new EventHandler<BuildingIndexShapeFileFeatureSourceEventArgs>(ShapeFileFeatureSource_BuildingIndex);

            try
            {
                if (GisEditor.ActiveMap != null)
                {
                    var closingOverlays = GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>()
                        .Where(tmpOverlay => tmpOverlay.Layers.OfType<ShapeFileFeatureLayer>()
                            .Select(tmpLayer => tmpLayer.ShapePathFilename)
                            .Contains(layer.ShapePathFilename));

                    Action action = new Action(() =>
                    {
                        foreach (var closingOverlay in closingOverlays)
                        {
                            closingOverlay.Close();
                        }
                    });

                    if (Application.Current == null)
                        action();
                    else
                        Application.Current.Dispatcher.BeginInvoke(action);
                }

                ShapeFileFeatureLayer.BuildIndexFile(layer.ShapePathFilename, BuildIndexMode.Rebuild);
            }
            finally
            {
                StatusBar.GetInstance().Cancelled -= new EventHandler<CancelEventArgs>(BuildIndexFileDialog_Cancelled);
                ShapeFileFeatureSource.BuildingIndex -= new EventHandler<BuildingIndexShapeFileFeatureSourceEventArgs>(ShapeFileFeatureSource_BuildingIndex);

                lock (layer)
                {
                    layer.Close();
                    layer.RequireIndex = true;
                    layer.Open();
                }
            }
        }

        private static void BuildIndexFileDialog_Cancelled(object sender, CancelEventArgs e)
        {
            isCancelled = true;
        }

        private static void ShapeFileFeatureSource_BuildingIndex(object sender, BuildingIndexShapeFileFeatureSourceEventArgs e)
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action<StatusBar>(s =>
                {
                    s.CurrentProgressBar.IsIndeterminate = false;
                    s.CurrentProgressBar.Value = e.CurrentRecordIndex * 100 / e.RecordCount;
                }), StatusBar.GetInstance());
            }

            if (isCancelled) e.Cancel = true;
        }

        private void SendMessage(bool? result = true)
        {
            Messenger.Default.Send(result, this);
        }
    }
}