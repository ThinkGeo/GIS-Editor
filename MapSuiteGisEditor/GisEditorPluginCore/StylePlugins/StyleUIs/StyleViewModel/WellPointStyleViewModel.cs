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
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class WellPointStyleViewModel : StyleViewModel
    {
        private int previewWidth;
        private int previewHeight;
        private WellPointStyle actualPointStyle;
        private Collection<BitmapImage> wellSymbolTypeList;

        public WellPointStyleViewModel(WellPointStyle style)
            : base(style)
        {
            HelpKey = "PointStyleHelp";

            ActualObject = style;
            actualPointStyle = style;

            if (wellSymbolTypeList == null)
            {
                wellSymbolTypeList = GetWellSymbolTypeListItems();
            }
        }

        public Collection<BitmapImage> WellSymbolTypeList
        {
            get { return wellSymbolTypeList; }
        }

        public int PreviewHeight
        {
            get { return previewHeight; }
            set { previewHeight = value; RaisePropertyChanged("PreviewHeight"); }
        }

        public int PreviewWidth
        {
            get { return previewWidth; }
            set { previewWidth = value; RaisePropertyChanged("PreviewWidth"); }
        }

        public int WellSymbolTypeIndex
        {
            get
            {
                return actualPointStyle.WellPointIndex;
            }
            set
            {
                actualPointStyle.WellPointIndex = value;
                RaisePropertyChanged("WellSymbolTypeIndex");
            }
        }

        public float SymbolSize
        {
            get
            {
                return actualPointStyle.SymbolSize;
            }
            set
            {
                actualPointStyle.SymbolSize = value;
                RaisePropertyChanged("SymbloSize");
            }
        }

        public GeoBrush FillColor
        {
            get
            {
                return actualPointStyle.Advanced.CustomBrush != null ? actualPointStyle.Advanced.CustomBrush : actualPointStyle.SymbolSolidBrush;
            }
            set
            {
                if (value is GeoSolidBrush)
                {
                    actualPointStyle.SymbolSolidBrush = (GeoSolidBrush)value;
                    actualPointStyle.Advanced.CustomBrush = null;
                }
                else
                {
                    actualPointStyle.Advanced.CustomBrush = value;
                }
                RaisePropertyChanged("FillColor");
            }
        }

        public GeoBrush OutlineColor
        {
            get
            {
                return actualPointStyle.SymbolPen.Brush;
            }
            set
            {
                actualPointStyle.SymbolPen.Brush = value;
                RaisePropertyChanged("OutlineColor");
            }
        }

        public float OutlineThickness
        {
            get
            {
                return actualPointStyle.SymbolPen.Width;
            }
            set
            {
                actualPointStyle.SymbolPen.Width = value;
                RaisePropertyChanged("OutlineThickness");
            }
        }

        public DrawingLevel DrawingLevel
        {
            get { return actualPointStyle.DrawingLevel; }
            set
            {
                actualPointStyle.DrawingLevel = value;
            }
        }

        public float XOffsetInPixel
        {
            get { return actualPointStyle.XOffsetInPixel; }
            set
            {
                actualPointStyle.XOffsetInPixel = value;
                RaisePropertyChanged("XOffsetInPixel");
            }
        }

        public float YOffsetInPixel
        {
            get { return actualPointStyle.YOffsetInPixel; }
            set
            {
                actualPointStyle.YOffsetInPixel = value;
                RaisePropertyChanged("YOffsetInPixel");
            }
        }

        private Collection<BitmapImage> GetWellSymbolTypeListItems()
        {
            Collection<BitmapImage> images = new Collection<BitmapImage>();

            WellPointStyle pointStyle = new WellPointStyle();
            pointStyle.SymbolSize = 16;
            pointStyle.SymbolSolidBrush = new GeoSolidBrush(GeoColor.StandardColors.Black);
            pointStyle.SymbolPen = new GeoPen(GeoColor.SimpleColors.Black);

            var bufferUnitNames = Enum.GetValues(typeof(WellPointSymbolType));
            foreach (var item in bufferUnitNames)
            {
                pointStyle.WellPointIndex = (int)item;
                pointStyle.XOffsetInPixel = -2;
                BitmapImage bitmapImage = pointStyle.GetPreviewImage(34, 18);
                images.Add(bitmapImage);
            }

            return images;
        }
    }
}
