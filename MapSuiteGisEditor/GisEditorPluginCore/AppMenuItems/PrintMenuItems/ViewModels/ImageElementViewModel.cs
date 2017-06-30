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
using System.Globalization;
using System.IO;
using System.Windows;
using GalaSoft.MvvmLight;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ImageElementViewModel : ViewModelBase
    {
        private static string previewImagePath = "pack://application:,,,/GisEditorPluginCore;component/Images/ImagePrinterLayer/arrow_type{0:D2}_preview.png";

        private ObservableCollection<byte[]> images;

        private byte[] selectedImage;

        private PrinterResizeMode resizeMode;

        private PrinterDragMode dragMode;

        private AreaStyle backgroundStyle;

        public ImageElementViewModel()
        {
            images = new ObservableCollection<byte[]>();

            for (int i = 1; i <= 15; i++)
            {
                var path = string.Format(CultureInfo.InvariantCulture, previewImagePath, i);

                if (previewImagePath.StartsWith("pack:"))
                    images.Add(StreamToBytes(Application.GetResourceStream(new Uri(path)).Stream));
                else
                    images.Add(File.ReadAllBytes(path));
            }

            SelectedImage = images[0];
            resizeMode = PrinterResizeMode.MaintainAspectRatio;
            dragMode = PrinterDragMode.Draggable;
            backgroundStyle = new AreaStyle();
            backgroundStyle.DrawingLevel = DrawingLevel.LabelLevel;
            AreaStyle areaStyle = new AreaStyle(new GeoPen(GeoColor.StandardColors.Black, 1));
            areaStyle.Name = "Area Style";
            areaStyle.DrawingLevel = DrawingLevel.LabelLevel;
            backgroundStyle.CustomAreaStyles.Add(areaStyle);
        }

        public ObservableCollection<byte[]> Images
        {
            get { return images; }
        }

        public byte[] SelectedImage
        {
            get { return selectedImage; }
            set
            {
                selectedImage = value;
                RaisePropertyChanged(()=>SelectedImage);
            }
        }

        public PrinterResizeMode ResizeMode
        {
            get { return resizeMode; }
            set
            {
                resizeMode = value;
                RaisePropertyChanged(()=>ResizeMode);
            }
        }

        public PrinterDragMode DragMode
        {
            get { return dragMode; }
            set
            {
                dragMode = value;
                RaisePropertyChanged(()=>DragMode);
            }
        }

        public AreaStyle BackgroundStyle
        {
            get { return backgroundStyle; }
            set
            {
                backgroundStyle = value;
                RaisePropertyChanged(()=>BackgroundPreview);
            }
        }

        public BitmapImage BackgroundPreview
        {
            get
            {
                return backgroundStyle.GetPreviewImage(156, 156);
            }
        }

        private byte[] StreamToBytes(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            byte[] bytes = new byte[stream.Length];

            stream.Read(bytes, 0, bytes.Length);

            return bytes;
        }
    }
}