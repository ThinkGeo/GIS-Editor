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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class TextElementViewModel : ViewModelBase
    {
        [NonSerialized]
        private System.Windows.Media.FontFamily fontName;
        [NonSerialized]
        private BitmapImage preview;
        private GeoBrush fontColor;
        private string text;
        private float fontSize;
        private bool withPreview;
        private bool wrapText;
        private bool isBold;
        private bool isItalic;
        private bool isStrikeout;
        private bool isUnderline;
        private PrinterDragMode dragMode;
        private PrinterResizeMode resizeMode;

        public TextElementViewModel(bool withPreview = true)
        {
            text = string.Empty;
            fontName = new System.Windows.Media.FontFamily("Arial");
            fontSize = 24;
            fontColor = new GeoSolidBrush(GeoColor.StandardColors.Black);
            dragMode = PrinterDragMode.Draggable;
            resizeMode = PrinterResizeMode.Resizable;
            this.withPreview = withPreview;
            PropertyChanged += TextElementEntity_PropertyChanged;
        }

        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                RaisePropertyChanged(()=>Text);
                RaisePropertyChanged(()=>IsEnabled);
            }
        }

        public System.Windows.Media.FontFamily FontName
        {
            get { return fontName; }
            set
            {
                fontName = value;
                RaisePropertyChanged(()=>FontName);
            }
        }

        public float FontSize
        {
            get { return fontSize; }
            set
            {
                fontSize = value;
                RaisePropertyChanged(()=>FontSize);
            }
        }

        public GeoBrush FontColor
        {
            get { return fontColor; }
            set
            {
                fontColor = value;
                RaisePropertyChanged(()=>FontColor);
            }
        }

        public bool IsBold
        {
            get { return isBold; }
            set
            {
                isBold = value;
                RaisePropertyChanged(()=>IsBold);
            }
        }

        public bool IsItalic
        {
            get { return isItalic; }
            set
            {
                isItalic = value;
                RaisePropertyChanged(()=>IsItalic);
            }
        }

        public bool IsStrikeout
        {
            get { return isStrikeout; }
            set
            {
                isStrikeout = value;
                RaisePropertyChanged(()=>IsStrikeout);
            }
        }

        public bool IsUnderline
        {
            get { return isUnderline; }
            set
            {
                isUnderline = value;
                RaisePropertyChanged(()=>IsUnderline);
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
                return !string.IsNullOrEmpty(Text.Trim());
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

        public PrinterResizeMode ResizeMode
        {
            get { return resizeMode; }
            set
            {
                resizeMode = value;
                RaisePropertyChanged(()=>ResizeMode);
            }
        }

        public bool WrapText
        {
            get { return wrapText; }
            set
            {
                wrapText = value;
                RaisePropertyChanged(()=>WrapText);
            }
        }

        private void TextElementEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (withPreview && e.PropertyName != "Preview" && !string.IsNullOrEmpty(text.Trim()))
            {
                LabelPrinterLayer labelPrinterLayer = new LabelPrinterLayer();
                labelPrinterLayer.LoadFromViewModel(this);
                using (Bitmap bitmap = new Bitmap(250, 100))
                {
                    PlatformGeoCanvas canvas = new PlatformGeoCanvas();
                    canvas.BeginDrawing(bitmap, new RectangleShape(-180, 90, 180, -90), GeographyUnit.Meter);

                    labelPrinterLayer.SafeProcess(() =>
                    {
                        labelPrinterLayer.Draw(canvas, new Collection<SimpleCandidate>());
                    });

                    //labelPrinterLayer.Open();
                    //labelPrinterLayer.Draw(canvas, new System.Collections.ObjectModel.Collection<SimpleCandidate>());
                    //labelPrinterLayer.Close();
                    canvas.EndDrawing();
                    MemoryStream ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Png);
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();
                    preview = bitmapImage;
                    RaisePropertyChanged(()=>Preview);
                }
            }
        }
    }
}