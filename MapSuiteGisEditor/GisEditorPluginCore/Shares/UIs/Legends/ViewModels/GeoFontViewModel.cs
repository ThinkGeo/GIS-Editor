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
using System.Reflection;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Drawing;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class GeoFontViewModel : ViewModelBase
    {
        [Obfuscation(Exclude = true)]
        private string fontName;

        [Obfuscation(Exclude = true)]
        private int fontSize;

        [Obfuscation(Exclude = true)]
        private DrawingFontStyles fontStyles;

        [Obfuscation(Exclude = true)]
        private bool isBold;

        [Obfuscation(Exclude = true)]
        private bool isItalic;

        [Obfuscation(Exclude = true)]
        private bool isStrike;

        [Obfuscation(Exclude = true)]
        private bool isUnderline;

        public GeoFontViewModel()
        {
            FontName = "Arial";
            FontStyles = DrawingFontStyles.Regular;
        }

        public string FontName
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

        public DrawingFontStyles FontStyles
        {
            get { return fontStyles; }
            set
            {
                fontStyles = value;
                RaisePropertyChanged(()=>FontStyles);
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

        public bool IsStrike
        {
            get { return isStrike; }
            set
            {
                isStrike = value;
                RaisePropertyChanged(()=>IsStrike);
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

        public bool IsBold
        {
            get { return isBold; }
            set
            {
                isBold = value;
                RaisePropertyChanged(()=>IsBold);
            }
        }

        public GeoFontViewModel Clone()
        {
            GeoFontViewModel newFont = new GeoFontViewModel
            {
                FontName = FontName,
                FontSize = FontSize,
                FontStyles = FontStyles,
                IsBold = IsBold,
                IsItalic = IsItalic,
                IsStrike = IsStrike,
                IsUnderline = IsUnderline
            };
            return newFont;
        }

        public GeoFont ToGeoFont()
        {
            DrawingFontStyles fontStyle = DrawingFontStyles.Regular;
            if (IsBold) fontStyle |= DrawingFontStyles.Bold;
            if (IsItalic) fontStyle |= DrawingFontStyles.Italic;
            if (IsStrike) fontStyle |= DrawingFontStyles.Strikeout;
            if (IsUnderline) fontStyle |= DrawingFontStyles.Underline;

            return new GeoFont(fontName, fontSize, fontStyle);
        }

        public void FromGeoFont(GeoFont geoFont)
        {
            IsBold = geoFont.IsBold;
            IsItalic = geoFont.IsItalic;
            IsStrike = geoFont.IsStrikeout;
            IsUnderline = geoFont.IsUnderline;
            FontName = geoFont.FontName;
            FontSize = (int)geoFont.Size;
        }
    }
}