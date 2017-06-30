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
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BuildIndexViewModel : ViewModelBase
    {
        [NonSerialized]
        private RelayCommand browseCommand;

        [NonSerialized]
        private RelayCommand<BuildShapeFileModel> removeCommand;

        private ObservableCollection<BuildShapeFileModel> sourceFiles;
        private bool isBusy;

        public BuildIndexViewModel()
        {
            sourceFiles = new ObservableCollection<BuildShapeFileModel>();
        }

        public void Execute()
        {
            IsBusy = true;

            GisEditor.GetMaps()
                .SelectMany(m => m.Overlays)
                .OfType<LayerOverlay>()
                .Where(l => l.Layers.OfType<ShapeFileFeatureLayer>().Any(tmpLayer => sourceFiles.Any(tmpSourceFile => tmpSourceFile.FilePath.Equals(tmpLayer.ShapePathFilename, StringComparison.OrdinalIgnoreCase))))
                .ForEach(l => l.Close());

            Task task = Task.Factory.StartNew(() =>
            {
                foreach (var item in sourceFiles)
                {
                    Task.Factory.StartNew((tmpItem) =>
                    {
                        ((BuildShapeFileModel)tmpItem).BuildIndex();
                    }, item, TaskCreationOptions.AttachedToParent);
                }
            });
            task.ContinueWith((t) => IsBusy = false);
        }

        public void Cancel()
        {
            foreach (var file in sourceFiles)
            {
                file.IsCancel = true;
            }
        }

        public RelayCommand BrowseCommand
        {
            get
            {
                if (browseCommand == null)
                {
                    browseCommand = new RelayCommand(() =>
                    {
                        NotificationMessageAction<IEnumerable<string>> openFileDialogMessage = new NotificationMessageAction<IEnumerable<string>>("", (files) =>
                        {
                            foreach (var item in files)
                            {
                                sourceFiles.Add(new BuildShapeFileModel(item));
                            }
                        });
                        Messenger.Default.Send(openFileDialogMessage, this);
                    });
                }
                return browseCommand;
            }
        }

        public RelayCommand<BuildShapeFileModel> RemoveCommand
        {
            get
            {
                if (removeCommand == null)
                {
                    removeCommand = new RelayCommand<BuildShapeFileModel>((file) =>
                    {
                        sourceFiles.Remove(file);
                    });
                }
                return removeCommand;
            }
        }

        public ObservableCollection<BuildShapeFileModel> SourceFiles
        {
            get { return sourceFiles; }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                RaisePropertyChanged(()=>IsBusy);
            }
        }
    }
}