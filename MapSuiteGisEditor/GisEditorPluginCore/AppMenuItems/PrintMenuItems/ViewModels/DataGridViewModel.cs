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
using System.Data;
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
    public class DataGridViewModel : ViewModelBase
    {
        [NonSerialized]
        private BitmapImage fontPreview;
        [NonSerialized]
        private System.Windows.Media.FontFamily fontName;

        private string addingColumnName;
        private DataTable currentDataTable;
        private float fontSize;
        private GeoBrush fontColor;
        private bool isBold;
        private bool isItalic;
        private bool isStrikeout;
        private bool isUnderline;
        private PrinterDragMode dragMode;
        private PrinterResizeMode resizeMode;
        private string removingColumnName;

        public DataGridViewModel()
        {
            addingColumnName = string.Empty;
            fontName = new System.Windows.Media.FontFamily("Arial");
            fontColor = new GeoSolidBrush(GeoColor.StandardColors.Black);
            resizeMode = PrinterResizeMode.Resizable;
            dragMode = PrinterDragMode.Draggable;
            currentDataTable = new DataTable();
            currentDataTable.Columns.CollectionChanged += Columns_CollectionChanged;
            PropertyChanged += DataGridViewModelPropertyChanged;
            FontSize = 9;
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

        public BitmapImage FontPreview
        {
            get { return fontPreview; }
            set
            {
                fontPreview = value;
                RaisePropertyChanged(()=>FontPreview);
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

        public DataTable CurrentDataTable
        {
            get { return currentDataTable; }
            set
            {
                currentDataTable = value;
                RaisePropertyChanged(()=>CurrentDataTable);
            }
        }

        public string AddingColumnName
        {
            get { return addingColumnName; }
            set
            {
                addingColumnName = value;
                RaisePropertyChanged(()=>AddingColumnName);
            }
        }

        public string RemovingColumnName
        {
            get { return removingColumnName; }
            set
            {
                removingColumnName = value;
                RaisePropertyChanged(()=>RemovingColumnName);
            }
        }

        public Collection<string> Columns
        {
            get
            {
                Collection<string> columns = new Collection<string>();
                foreach (DataColumn item in currentDataTable.Columns)
                {
                    columns.Add(item.ColumnName);
                }
                return columns;
            }
        }

        public bool IsDropDownAndRomoveEnabled
        {
            get { return Columns.Count > 0; }
        }

        private void Columns_CollectionChanged(object sender, CollectionChangeEventArgs e)
        {
            RaisePropertyChanged(()=>Columns);
            RaisePropertyChanged(()=>IsDropDownAndRomoveEnabled);
        }

        private LabelPrinterLayer GetLabelPrinterLayer()
        {
            DrawingFontStyles drawingFontStyles = DrawingFontStyles.Regular;
            if (IsBold)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Bold;
            if (IsItalic)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Italic;
            if (IsStrikeout)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Strikeout;
            if (IsUnderline)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Underline;

            GeoFont font = new GeoFont(FontName.Source, FontSize, drawingFontStyles);
            LabelPrinterLayer labelPrinterLayer = new LabelPrinterLayer("Abc", font, FontColor);
            return labelPrinterLayer;
        }

        private void DataGridViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FontPreview" || e.PropertyName == "AddingColumnName" || e.PropertyName == "RemovingColumnName") return;
            LabelPrinterLayer labelPrinterLayer = GetLabelPrinterLayer();
            using (Bitmap bitmap = new Bitmap(311, 60))
            {
                PlatformGeoCanvas canvas = new PlatformGeoCanvas();
                canvas.BeginDrawing(bitmap, new RectangleShape(-180, 90, 180, -90), GeographyUnit.Meter);

                labelPrinterLayer.SafeProcess(() =>
                {
                    labelPrinterLayer.Draw(canvas, new System.Collections.ObjectModel.Collection<SimpleCandidate>());
                });

                canvas.EndDrawing();
                MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                FontPreview = bitmapImage;
            }
        }
    }
}