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
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class AreaStyleViewModel : StyleViewModel
    {
        private GeoPenViewModel outlinePen;
        private AreaStyle actualAreaStyle;

        public AreaStyleViewModel(AreaStyle style)
            : base(style)
        {
            actualAreaStyle = style;
            HelpKey = "AreaStyleHelp";
            outlinePen = new GeoPenViewModel(style.OutlinePen);
            OutlinePen.PropertyChanged += OutlinePen_PropertyChanged;
            //LoadSwitchableStylePlugins(StyleCategories.Area);
            //LoadSwitchableStylePlugins(StyleCategories.Text);
            //SetDefaultSelectedStyleType();
        }

        public GeoBrush OutLineColor
        {
            get
            {
                return actualAreaStyle.OutlinePen.Brush;
            }
            set
            {
                actualAreaStyle.OutlinePen.Brush = value;
                RaisePropertyChanged("OutLineColor");
            }
        }

        public float OutlineThickness
        {
            get
            {
                return actualAreaStyle.OutlinePen.Width;
            }
            set
            {
                actualAreaStyle.OutlinePen.Width = value;
                RaisePropertyChanged("OutlineThickness");
            }
        }

        public GeoBrush FillColor
        {
            get
            {
                return actualAreaStyle.Advanced.FillCustomBrush != null ? actualAreaStyle.Advanced.FillCustomBrush : actualAreaStyle.FillSolidBrush;
            }
            set
            {
                GeoSolidBrush solidBrush = value as GeoSolidBrush;
                if (solidBrush != null)
                {
                    actualAreaStyle.FillSolidBrush = solidBrush;
                    actualAreaStyle.Advanced.FillCustomBrush = null;
                }
                else
                {
                    actualAreaStyle.Advanced.FillCustomBrush = value;
                }

                RaisePropertyChanged("FillColor");
            }
        }

        public GeoPenViewModel OutlinePen
        {
            get { return outlinePen; }
        }

        public DrawingLevel DrawingLevel
        {
            get { return actualAreaStyle.DrawingLevel; }
            set { actualAreaStyle.DrawingLevel = value; }
        }

        public float XOffsetInPixel
        {
            get { return actualAreaStyle.XOffsetInPixel; }
            set
            {
                actualAreaStyle.XOffsetInPixel = value;
                RaisePropertyChanged("XOffsetInPixel");
            }
        }

        public float YOffsetInPixel
        {
            get { return actualAreaStyle.YOffsetInPixel; }
            set
            {
                actualAreaStyle.YOffsetInPixel = value;
                RaisePropertyChanged("YOffsetInPixel");
            }
        }

        private void OutlinePen_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(e.PropertyName);
        }
    }
}