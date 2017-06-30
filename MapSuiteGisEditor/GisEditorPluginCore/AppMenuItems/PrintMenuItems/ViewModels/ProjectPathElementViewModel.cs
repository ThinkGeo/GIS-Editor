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


using GalaSoft.MvvmLight;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ProjectPathElementViewModel : ViewModelBase
    {
        [NonSerialized]
        private System.Windows.Media.FontFamily fontName;
        [NonSerialized]
        private BitmapImage preview;
        [NonSerialized]
        private GeoBrush fontColor;
        [NonSerialized]
        private float fontSize;
        [NonSerialized]
        private bool isBold;
        [NonSerialized]
        private bool isItalic;
        [NonSerialized]
        private bool isStrikeout;
        [NonSerialized]
        private bool isUnderline;
        [NonSerialized]
        private PrinterDragMode dragMode;
        [NonSerialized]
        private PrinterResizeMode resizeMode;
        [NonSerialized]
        private string projectPath;

        public ProjectPathElementViewModel(string projectPath)
        {
            fontName = new System.Windows.Media.FontFamily("Arial");
            fontSize = 12;
            fontColor = new GeoSolidBrush(GeoColor.StandardColors.Black);
            dragMode = PrinterDragMode.Draggable;
            resizeMode = PrinterResizeMode.Resizable;
            this.projectPath = projectPath;
            PropertyChanged += ProjectPathElementEntity_PropertyChanged;
            RefreshPreview("");
        }

        public System.Windows.Media.FontFamily FontName
        {
            get { return fontName; }
            set
            {
                fontName = value;
                RaisePropertyChanged(() => FontName);
            }
        }

        public string ProjectPath
        {
            get { return projectPath; }
            set
            {
                projectPath = value;
                RaisePropertyChanged(() => ProjectPath);
            }
        }

        public float FontSize
        {
            get { return fontSize; }
            set
            {
                fontSize = value;
                RaisePropertyChanged(() => FontSize);
            }
        }

        public GeoBrush FontColor
        {
            get { return fontColor; }
            set
            {
                fontColor = value;
                RaisePropertyChanged(() => FontColor);
            }
        }

        public bool IsBold
        {
            get { return isBold; }
            set
            {
                isBold = value;
                RaisePropertyChanged(() => IsBold);
            }
        }

        public bool IsItalic
        {
            get { return isItalic; }
            set
            {
                isItalic = value;
                RaisePropertyChanged(() => IsItalic);
            }
        }

        public bool IsStrikeout
        {
            get { return isStrikeout; }
            set
            {
                isStrikeout = value;
                RaisePropertyChanged(() => IsStrikeout);
            }
        }

        public bool IsUnderline
        {
            get { return isUnderline; }
            set
            {
                isUnderline = value;
                RaisePropertyChanged(() => IsUnderline);
            }
        }

        public BitmapImage Preview
        {
            get { return preview; }
        }

        public bool IsEnabled
        {
            get
            {
                return !string.IsNullOrEmpty(ProjectPath.Trim());
            }
        }

        public PrinterDragMode DragMode
        {
            get { return dragMode; }
            set
            {
                dragMode = value;
                RaisePropertyChanged(() => DragMode);
            }
        }

        public PrinterResizeMode ResizeMode
        {
            get { return resizeMode; }
            set
            {
                resizeMode = value;
                RaisePropertyChanged(() => ResizeMode);
            }
        }

        private void ProjectPathElementEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RefreshPreview(e.PropertyName);
        }

        private void RefreshPreview(string propertyName)
        {
            if (propertyName != "Preview" && !string.IsNullOrEmpty(ProjectPath.Trim()))
            {
                ProjectPathPrinterLayer projectPathPrinterLayer = new ProjectPathPrinterLayer();
                projectPathPrinterLayer.LoadFromViewModel(this);
                using (Bitmap bitmap = new Bitmap(460, 50))
                {
                    PlatformGeoCanvas canvas = new PlatformGeoCanvas();
                    canvas.BeginDrawing(bitmap, new RectangleShape(-180, 90, 180, -90), GeographyUnit.Meter);

                    projectPathPrinterLayer.SafeProcess(() =>
                    {
                        projectPathPrinterLayer.Draw(canvas, new Collection<SimpleCandidate>());
                    });

                    canvas.EndDrawing();
                    MemoryStream ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Png);
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();
                    preview = bitmapImage;
                    RaisePropertyChanged(() => Preview);
                }
            }
        }
    }
}
