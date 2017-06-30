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
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class TexturePickerViewModel : INotifyPropertyChanged
    {
        [NonSerialized]
        private Brush selectedBrush;
        private ObservableCollection<ImageBrush> imageBrushes;

        public event PropertyChangedEventHandler PropertyChanged;

        public TexturePickerViewModel()
        {
            imageBrushes = new ObservableCollection<ImageBrush>();
            InitializePredefinedBrushes();
        }

        public Brush SelectedBrush
        {
            get { return selectedBrush; }
            set
            {
                if (selectedBrush != value)
                {
                    selectedBrush = value;
                    RaisePropertyChanged("SelectedBrush");
                }
            }
        }

        public ObservableCollection<ImageBrush> ImageBrushes { get { return imageBrushes; } }

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void InitializePredefinedBrushes()
        {
            string directoryPath = GetTextureImagesFolder();
            if (Directory.Exists(directoryPath))
            {
                string[] fileNames = Directory.GetFiles(directoryPath, "*.png");
                foreach (string fileName in fileNames)
                {
                    string fullFileName = new FileInfo(fileName).FullName;

                    BitmapImage imageSource = new BitmapImage();
                    imageSource.BeginInit();
                    imageSource.UriSource = new Uri("file://" + fullFileName);
                    imageSource.EndInit();

                    ImageBrush brush = new ImageBrush(imageSource);
                    brush.SetValue(Canvas.TagProperty, fullFileName);
                    imageBrushes.Add(brush);
                }
            }
        }

        public static string GetTextureImagesFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Map Suite Gis Editor", "Images");
        }
    }
}