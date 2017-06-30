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
using System.Windows.Media;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class TitleViewModel : ViewModelBase
    {
        private string id;
        private string title;
        private int fontSize;
        private GeoBrush fontColor;
        private bool isBold;
        private bool isItalic;
        private bool isStrikeout;
        private bool isUnderline;
        private float angle;
        private AdornmentLocation titleLocation;
        private float left;
        private float top;
        private bool doesAddHalo;
        private GeoBrush haloColor;
        private float haloThickness;
        private bool isEnableMask;
        private GeoBrush maskFillColor;
        private GeoBrush maskOutlineColor;
        private float maskOutlineThickness;
        private int maskMarginValue;
        
        [NonSerialized]
        private FontFamily fontName;

        public TitleViewModel()
        {
            Title = GisEditor.LanguageManager.GetStringResource("TitleEntityNewTitle");
            TitleLocation = AdornmentLocation.UpperCenter;
            FontName = new FontFamily("Arial");
            FontSize = 18;
            HaloThickness = 3;
        }

        public string ID
        {
            get
            {
                if (String.IsNullOrEmpty(id))
                    id = Guid.NewGuid().ToString();
                return id;
            }
            set { id = value; }
        }

        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                if (!String.IsNullOrEmpty(title))
                    RaisePropertyChanged(()=>Title);
            }
        }

        public FontFamily FontName
        {
            get { return fontName; }
            set
            {
                fontName = value;
                RaisePropertyChanged(()=>FontName);
            }
        }

        public int FontSize
        {
            get { return fontSize; }
            set
            {
                if (value > 0)
                {
                    fontSize = value;
                    RaisePropertyChanged(()=>FontSize);
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("TitleEntityValueGreaterThanText"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"));
                }
            }
        }

        public GeoBrush FontColor
        {
            get
            {
                if (fontColor == null)
                    fontColor = new GeoSolidBrush(GeoColor.SimpleColors.Black);
                return fontColor;
            }
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

        public float Angle
        {
            get { return (float)Math.Round(angle, 2); }
            set
            {
                angle = value;
                if (angle > 360)
                    angle = 360;
                else if (angle < 0)
                    angle = 0;
                RaisePropertyChanged(()=>Angle);
            }
        }

        public AdornmentLocation TitleLocation
        {
            get { return titleLocation; }
            set
            {
                titleLocation = value;
                RaisePropertyChanged(()=>TitleLocation);
            }
        }

        public float Left
        {
            get { return left; }
            set
            {
                left = value;
                RaisePropertyChanged(()=>Left);
            }
        }

        public float Top
        {
            get { return top; }
            set
            {
                top = value;
                RaisePropertyChanged(()=>Top);
            }
        }

        public bool DoesAddHalo
        {
            get { return doesAddHalo; }
            set
            {
                doesAddHalo = value;
                RaisePropertyChanged(()=>DoesAddHalo);
            }
        }

        public GeoBrush HaloColor
        {
            get
            {
                if (haloColor == null)
                    haloColor = new GeoSolidBrush(GeoColor.StandardColors.White);
                return haloColor;
            }
            set
            {
                haloColor = value;
                RaisePropertyChanged(()=>HaloColor);
            }
        }

        public float HaloThickness
        {
            get { return haloThickness; }
            set
            {
                haloThickness = value;
                RaisePropertyChanged(()=>HaloThickness);
            }
        }

        public bool IsEnableMask
        {
            get { return isEnableMask; }
            set
            {
                isEnableMask = value;
                RaisePropertyChanged(()=>IsEnableMask);
            }
        }

        public GeoBrush MaskFillColor
        {
            get
            {
                if (maskFillColor == null)
                    maskFillColor = new GeoSolidBrush(GeoColor.StandardColors.White);
                return maskFillColor;
            }
            set
            {
                maskFillColor = value;
                RaisePropertyChanged(()=>MaskFillColor);
            }
        }

        public GeoBrush MaskOutlineColor
        {
            get
            {
                if (maskOutlineColor == null)
                    maskOutlineColor = new GeoSolidBrush(GeoColor.SimpleColors.Black);
                return maskOutlineColor;
            }
            set
            {
                maskOutlineColor = value;
                RaisePropertyChanged(()=>MaskOutlineColor);
            }
        }

        public float MaskOutlineThickness
        {
            get { return maskOutlineThickness; }
            set
            {
                maskOutlineThickness = value;
                RaisePropertyChanged(()=>MaskOutlineThickness);
            }
        }

        public int MaskMarginValue
        {
            get { return maskMarginValue; }
            set
            {
                maskMarginValue = value;
                RaisePropertyChanged(()=>MaskMarginValue);
            }
        }

        public void Load(TitleAdornmentLayer layer)
        {
            Title = layer.Title;
            FontName = new FontFamily(layer.TitleFont.FontName);
            FontSize = (int)layer.TitleFont.Size;
            FontColor = layer.FontColor;
            IsBold = layer.TitleFont.IsBold;
            IsItalic = layer.TitleFont.IsItalic;
            IsStrikeout = layer.TitleFont.IsStrikeout;
            IsUnderline = layer.TitleFont.IsUnderline;
            Angle = layer.Rotation;
            TitleLocation = layer.Location;
            Left = layer.XOffsetInPixel;
            Top = layer.YOffsetInPixel;
            DoesAddHalo = layer.HaloPen != null;
            if (DoesAddHalo)
            {
                HaloColor = layer.HaloPen.Brush;
                HaloThickness = layer.HaloPen.Width;
            }

            IsEnableMask = layer.MaskFillColor != null;
            if (IsEnableMask)
            {
                MaskFillColor = layer.MaskFillColor;
                MaskOutlineColor = layer.MaskOutlineColor;
                MaskOutlineThickness = layer.MaskOutlineThickness;
                MaskMarginValue = layer.MaskMargin;
            }
        }
    }
}