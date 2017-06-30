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
using System.Windows;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class LineStyleViewModel : StyleViewModel
    {
        private GeoPenViewModel outerPen;
        private GeoPenViewModel innerPen;
        private GeoPenViewModel centerPen;
        private LineStyle actualLineStyle;
        private DrawingLevel selectedOuterPenDrawingLevel;

        public LineStyleViewModel(LineStyle style)
            : base(style)
        {
            HelpKey = "LineStyleHelp";
            ActualObject = style;
            actualLineStyle = style;
            outerPen = new GeoPenViewModel(style.OuterPen);
            innerPen = new GeoPenViewModel(style.InnerPen);
            centerPen = new GeoPenViewModel(style.CenterPen);

            OuterPen.PropertyChanged += new PropertyChangedEventHandler(GeoPen_PropertyChanged);
            InnerPen.PropertyChanged += new PropertyChangedEventHandler(GeoPen_PropertyChanged);
            CenterPen.PropertyChanged += new PropertyChangedEventHandler(GeoPen_PropertyChanged);

            //LoadSwitchableStylePlugins(StyleCategories.Line);
            //LoadSwitchableStylePlugins(StyleCategories.Text);
            //SetDefaultSelectedStyleType();
            OuterPen.ColorAndThicknessVisibility = Visibility.Collapsed;
        }

        public GeoBrush OuterPenColor
        {
            get
            {
                return actualLineStyle.OuterPen.Brush;
            }
            set
            {
                actualLineStyle.OuterPen.Brush = value;
                RaisePropertyChanged("OuterPenColor");
            }
        }

        public DrawingLevel DrawingLevel
        {
            get { return actualLineStyle.OuterPenDrawingLevel; }
            set
            {
                selectedOuterPenDrawingLevel = value;
                actualLineStyle.OuterPenDrawingLevel = selectedOuterPenDrawingLevel;
                RaisePropertyChanged("DrawingLevel");
            }
        }

        public float OuterPenWidth
        {
            get
            {
                return actualLineStyle.OuterPen.Width;
            }
            set
            {
                actualLineStyle.OuterPen.Width = value;
                RaisePropertyChanged("OuterPenWidth");
            }
        }

        public GeoBrush InnerPenColor
        {
            get
            {
                return actualLineStyle.InnerPen.Brush;
            }
            set
            {
                actualLineStyle.InnerPen.Brush = value;
                RaisePropertyChanged("InnerPenColor");
            }
        }

        public float InnerPenWidth
        {
            get
            {
                return actualLineStyle.InnerPen.Width;
            }
            set
            {
                actualLineStyle.InnerPen.Width = value;
                RaisePropertyChanged("InnerPenWidth");
            }
        }

        public GeoBrush CenterPenColor
        {
            get
            {
                return actualLineStyle.CenterPen.Brush;
            }
            set
            {
                actualLineStyle.CenterPen.Brush = value;
                RaisePropertyChanged("CenterPenColor");
            }
        }

        public float CenterPenWidth
        {
            get
            {
                return actualLineStyle.CenterPen.Width;
            }
            set
            {
                actualLineStyle.CenterPen.Width = value;
                RaisePropertyChanged("CenterPenWidth");
            }
        }

        public GeoPenViewModel OuterPen
        {
            get { return outerPen; }
        }

        public GeoPenViewModel InnerPen
        {
            get { return innerPen; }
        }

        public GeoPenViewModel CenterPen
        {
            get { return centerPen; }
        }

        public float XOffsetInPixel
        {
            get { return actualLineStyle.XOffsetInPixel; }
            set
            {
                actualLineStyle.XOffsetInPixel = value;
                RaisePropertyChanged("XOffsetInPixel");
            }
        }

        public float YOffsetInPixel
        {
            get { return actualLineStyle.YOffsetInPixel; }
            set
            {
                actualLineStyle.YOffsetInPixel = value;
                RaisePropertyChanged("YOffsetInPixel");
            }
        }

        private void GeoPen_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(e.PropertyName);
            PreviewSource = ActualObject.GetPreviewImage();
        }
    }
}