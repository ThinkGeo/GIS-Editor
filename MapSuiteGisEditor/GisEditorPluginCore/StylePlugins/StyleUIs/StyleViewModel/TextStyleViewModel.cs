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


using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class TextStyleViewModel : StyleViewModel
    {
        private string imagePath;
        private string functionText;
        private bool isCustomText;
        private bool contextMenuIsOpen;
        private bool basicLabelIsChecked;
        private bool dateFormatIsEnabled;
        private bool numericFormatIsEnabled;
        private Collection<int> gridSizes;
        private Collection<BoundItem> dataFormats;
        private Collection<BoundItem> numericFormats;
        private Visibility basicLabelGridVisibility;
        private Visibility customLabelGridVisibility;
        private Visibility labelFunctionGridVisibility;
        private Visibility forceHorizontalLabelForLineVisibility;
        private Visibility splineTypeVisibility;
        private Visibility labelPolygonVisibility;
        private Visibility fittingPolygonVisibility;
        private Visibility fittingFactorVisibility;
        private Visibility fittingLineInScreenVisibility;
        private Visibility segmentRatioVisibility;
        private Visibility bestPlacementVisibility;
        private Visibility labelPlacementVisibility;
        private Visibility customLabelCheckButtonVisibility;
        private Visibility iconTabVisibility;
        private Visibility duplicationTabVisibility;
        private Visibility viewDataButtonVisibility;
        private Visibility placementTabVisibility;
        private Collection<FeatureSourceColumn> featureSourceColumns;
        private StyleBuilderArguments requiredValues;
        private IconTextStyle actualTextStyle;
        private RelayCommand insertCommand;
        private RelayCommand labelFunctionsCommand;

        public TextStyleViewModel(IconTextStyle style, StyleBuilderArguments requiredValues)
            : base(style)
        {
            gridSizes = new Collection<int> { 500, 400, 300, 200, 150, 120, 100, 80, 40, 20, 10 };
            dataFormats = new Collection<BoundItem>
            {
                new BoundItem("None", string.Empty),
                new BoundItem("mm/dd/yyyy","{0:MM/dd/yyyy}"),
                new BoundItem("dd/mm/yyyy","{0:dd/MM/yyyy}"),
                new BoundItem("mmmm,yyyy","{0:mmmm,yyyy}"),
                new BoundItem("yyyy,mmmm","{0:yyyy,mmmm}"),
                new BoundItem("mmmm,dd","{0:mmmm,dd}"),
                new BoundItem("d mmmm","{0:d mmmm}"),
                new BoundItem("mm/dd/yyyy hh:mm:ss","{0:MM/dd/yyyy hh:mm:ss}"),
                new BoundItem("dd/mm/yyyy hh:mm:ss","{0:dd/MM/yyyy hh:mm:ss}"),
                new BoundItem("hh:mm:ss","{0:hh:mm:ss}"),
                new BoundItem("hh:mm","{0:hh:mm}"),
                new BoundItem("hh","{0:hh:mm}"),
                new BoundItem("mm","{0:mm}"),
                new BoundItem("ss","{0:ss}")
            };
            numericFormats = new Collection<BoundItem>
            {
                new BoundItem("None", string.Empty),
                new BoundItem("Currency","{0:C}"),
                new BoundItem("Decimal","{0:0.0}"),
                new BoundItem("Scientific (exponential)","{0:E}"),
                new BoundItem("Fixed-point","{0:F}"),
                new BoundItem("General","{0:G}"),
                new BoundItem("Number","{0:N0}"),
                new BoundItem("Percent","{0:0%}"),
                new BoundItem("Round-trip","{0:R}"),
                new BoundItem("Custom","{0:N4}")
            };
            ActualObject = style;
            actualTextStyle = style;
            RequiredValues = requiredValues;

            var shpLayer = RequiredValues.FeatureLayer as ShapeFileFeatureLayer;
            if (shpLayer != null)
            {
                shpLayer.Open();
                featureSourceColumns = shpLayer.FeatureSource.GetColumns();
            }

            if (actualTextStyle.IsLabelFunctionEnabled)
            {
                IsLabelFunctions = true;
                string text = actualTextStyle.LabelFunctionsScript;
                foreach (var item in actualTextStyle.LabelFunctionColumnNames)
                {
                    if (text.Contains(item.Key))
                    {
                        text = text.Replace(item.Key, "[" + item.Value + "]");
                    }
                }
                functionText = text;
            }
            else if (TextColumnName.Contains("[") && TextColumnName.Contains("]"))
            {
                IsCustomText = true;
            }
            else
            {
                BasicLabelIsChecked = true;
            }

            try
            {
                if (string.IsNullOrEmpty(TextColumnName))
                {
                    string columnContainsName = RequiredValues.ColumnNames.Where(columnName => columnName.IndexOf("name", StringComparison.InvariantCultureIgnoreCase) != -1).FirstOrDefault();
                    if (!string.IsNullOrEmpty(columnContainsName))
                    {
                        string alias = RequiredValues.FeatureLayer.FeatureSource.GetColumnAlias(columnContainsName);
                        SelectedTextColumnName = new KeyValuePair<string, string>(columnContainsName, alias);
                    }
                    else
                    {
                        string firstColumn = RequiredValues.ColumnNames.FirstOrDefault();
                        if (!string.IsNullOrEmpty(firstColumn))
                        {
                            string alias = RequiredValues.FeatureLayer.FeatureSource.GetColumnAlias(firstColumn);
                            SelectedTextColumnName = new KeyValuePair<string, string>(firstColumn, alias);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                if (!String.IsNullOrEmpty(style.TextColumnName))
                {
                    SelectedTextColumnName = ColumnNames.FirstOrDefault();

                    CustomLabelCheckButtonVisibility = Visibility.Collapsed;
                    ViewDataButtonVisibility = Visibility.Collapsed;
                    PlacementTabVisibility = Visibility.Collapsed;
                    DuplicationTabVisibility = Visibility.Collapsed;
                    IconTabVisibility = Visibility.Collapsed;
                }
            }

            if (GisEditor.ActiveMap != null && GisEditor.ActiveMap.GetFeatureLayers(true).Count == 0)
            {
                ViewDataButtonVisibility = Visibility.Collapsed;
            }

            ChangeInnerControlsVisibility();
            //LoadSwitchableStylePlugins(StylePluginHelper.GetStyleProviderTypesAccordingToShapeType(requiredValues.ShapeType));
            //LoadSwitchableStylePlugins(StyleCategories.Text);
            //SetDefaultSelectedStyleType();
            HelpKey = "LabelStyleHelp";
        }

        public Visibility CustomLabelCheckButtonVisibility
        {
            get { return customLabelCheckButtonVisibility; }
            set
            {
                customLabelCheckButtonVisibility = value;
                RaisePropertyChanged("CustomLabelCheckButtonVisibility");
            }
        }

        public Visibility IconTabVisibility
        {
            get { return iconTabVisibility; }
            set
            {
                iconTabVisibility = value;
                RaisePropertyChanged("IconTabVisibility");
            }
        }

        public Visibility DuplicationTabVisibility
        {
            get { return duplicationTabVisibility; }
            set
            {
                duplicationTabVisibility = value;
                RaisePropertyChanged("DuplicationTabVisibility");
            }
        }

        public Visibility PlacementTabVisibility
        {
            get { return placementTabVisibility; }
            set
            {
                placementTabVisibility = value;
                RaisePropertyChanged("PlacementTabVisibility");
            }
        }

        public Visibility ViewDataButtonVisibility
        {
            get { return viewDataButtonVisibility; }
            set
            {
                viewDataButtonVisibility = value;
                RaisePropertyChanged("ViewDataButtonVisibility");
            }
        }

        public StyleBuilderArguments RequiredValues
        {
            get { return requiredValues; }
            set { requiredValues = value; }
        }

        public Dictionary<string, string> ColumnNames
        {
            get
            {
                Dictionary<string, string> newColumnNames = new Dictionary<string, string>();

                foreach (var cloumnName in requiredValues.ColumnNames)
                {
                    if (!string.IsNullOrEmpty(cloumnName) && !newColumnNames.ContainsKey(cloumnName))
                    {
                        newColumnNames.Add(cloumnName, requiredValues.FeatureLayer.FeatureSource.GetColumnAlias(cloumnName));
                    }
                }

                return newColumnNames;
            }
        }

        public Collection<BoundItem> DataFormats
        {
            get { return dataFormats; }
        }

        public Collection<BoundItem> NumericFormats
        {
            get { return numericFormats; }
        }

        public Visibility LabelingLocationVisibility
        {
            get
            {
                bool isArea = false;
                WellKnownType wkt = WellKnownType.Invalid;
                RequiredValues.FeatureLayer.SafeProcess(() =>
                {
                    wkt = RequiredValues.FeatureLayer.FeatureSource.GetFirstFeaturesWellKnownType();
                });
                if (wkt == WellKnownType.Polygon || wkt == WellKnownType.Multipolygon)
                {
                    isArea = true;
                }

                return isArea ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public bool IsBoundingBoxCenter
        {
            get { return actualTextStyle.PolygonLabelingLocationMode == PolygonLabelingLocationMode.BoundingBoxCenter; }
            set
            {
                actualTextStyle.PolygonLabelingLocationMode = value ? PolygonLabelingLocationMode.BoundingBoxCenter : PolygonLabelingLocationMode.Centroid;
                RaisePropertyChanged("IsBoundingBoxCenter");
            }
        }

        public bool IsCentroid
        {
            get { return actualTextStyle.PolygonLabelingLocationMode == PolygonLabelingLocationMode.Centroid; }
            set
            {
                actualTextStyle.PolygonLabelingLocationMode = value ? PolygonLabelingLocationMode.Centroid : PolygonLabelingLocationMode.BoundingBoxCenter;
                RaisePropertyChanged("IsCntroid");
            }
        }

        public string TextColumnName
        {
            get
            {
                return actualTextStyle.TextColumnName;
            }
            set
            {
                actualTextStyle.TextColumnName = value;
                RaisePropertyChanged("TextColumnName");
            }
        }

        public KeyValuePair<string, string> SelectedTextColumnName
        {
            get
            {
                return ColumnNames.FirstOrDefault(c => c.Key == actualTextStyle.TextColumnName.Replace("[", string.Empty).Replace("]", string.Empty));
            }
            set
            {
                actualTextStyle.TextColumnName = value.Key;
                RaisePropertyChanged("SelectedTextColumnName");
            }
        }

        public bool IsCustomText
        {
            get
            {
                return isCustomText;
            }
            set
            {
                isCustomText = value;
                if (value)
                {
                    BasicLabelGridVisibility = Visibility.Collapsed;
                    LabelFunctionGridVisibility = Visibility.Collapsed;
                    CustomLabelGridVisibility = Visibility.Visible;

                    DateFormat = DataFormats.FirstOrDefault();
                    DateFormatIsEnabled = false;

                    NumericFormat = NumericFormats.FirstOrDefault();
                    NumericFormatIsEnabled = false;

                    if (TextColumnName != null
                        && RequiredValues.ColumnNames.Contains<string>(TextColumnName))
                    {
                        TextColumnName = string.Format("[{0}]", TextColumnName);
                    }
                }

                RaisePropertyChanged("IsCustomText");
                RaisePropertyChanged("BasicLabelIsChecked");
            }
        }

        public bool IsLabelFunctions
        {
            get { return actualTextStyle.IsLabelFunctionEnabled; }
            set
            {
                actualTextStyle.IsLabelFunctionEnabled = value;
                if (value)
                {
                    BasicLabelGridVisibility = Visibility.Collapsed;
                    CustomLabelGridVisibility = Visibility.Collapsed;
                    LabelFunctionGridVisibility = Visibility.Visible;
                }
                RaisePropertyChanged("IsCustomText");
            }
        }

        public string CustomLabel
        {
            get
            {
                return "";
            }
            set
            {
                RaisePropertyChanged("CustomLabel");
            }
        }

        public FontFamily FontName
        {
            get
            {
                return Fonts.SystemFontFamilies.FirstOrDefault(ff => ff.Source.Equals(actualTextStyle.Font.FontName));
            }
            set
            {
                actualTextStyle.Font = new GeoFont(value.Source, actualTextStyle.Font.Size, actualTextStyle.Font.Style);
                RaisePropertyChanged("FontName");
            }
        }

        public float FontSize
        {
            get
            {
                return actualTextStyle.Font.Size;
            }
            set
            {
                if (value > 0 && value <= 100)
                {
                    actualTextStyle.Font = new GeoFont(actualTextStyle.Font.FontName, value, actualTextStyle.Font.Style);
                    RaisePropertyChanged("FontSize");
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("TextStyleViewModelMakeSureMessage"), "Warning");
                }
            }
        }

        public bool IsBold
        {
            get
            {
                return actualTextStyle.Font.IsBold;
            }
            set
            {
                if (value)
                {
                    actualTextStyle.Font = new GeoFont(actualTextStyle.Font.FontName, actualTextStyle.Font.Size, actualTextStyle.Font.Style | DrawingFontStyles.Bold);
                }
                else
                {
                    actualTextStyle.Font = new GeoFont(actualTextStyle.Font.FontName, actualTextStyle.Font.Size, actualTextStyle.Font.Style ^ DrawingFontStyles.Bold);
                }
                RaisePropertyChanged("IsBold");
            }
        }

        public bool IsItalic
        {
            get
            {
                return actualTextStyle.Font.IsItalic;
            }
            set
            {
                if (value)
                {
                    actualTextStyle.Font = new GeoFont(actualTextStyle.Font.FontName, actualTextStyle.Font.Size, actualTextStyle.Font.Style | DrawingFontStyles.Italic);
                }
                else
                {
                    actualTextStyle.Font = new GeoFont(actualTextStyle.Font.FontName, actualTextStyle.Font.Size, actualTextStyle.Font.Style ^ DrawingFontStyles.Italic);
                }
                RaisePropertyChanged("IsItalic");
            }
        }

        public bool IsStrikeout
        {
            get
            {
                return actualTextStyle.Font.IsStrikeout;
            }
            set
            {
                if (value)
                {
                    actualTextStyle.Font = new GeoFont(actualTextStyle.Font.FontName, actualTextStyle.Font.Size, actualTextStyle.Font.Style | DrawingFontStyles.Strikeout);
                }
                else
                {
                    actualTextStyle.Font = new GeoFont(actualTextStyle.Font.FontName, actualTextStyle.Font.Size, actualTextStyle.Font.Style ^ DrawingFontStyles.Strikeout);
                }
                RaisePropertyChanged("IsStrikeout");
            }
        }

        public bool IsUnderline
        {
            get
            {
                return actualTextStyle.Font.IsUnderline;
            }
            set
            {
                if (value)
                {
                    actualTextStyle.Font = new GeoFont(actualTextStyle.Font.FontName, actualTextStyle.Font.Size, actualTextStyle.Font.Style | DrawingFontStyles.Underline);
                }
                else
                {
                    actualTextStyle.Font = new GeoFont(actualTextStyle.Font.FontName, actualTextStyle.Font.Size, actualTextStyle.Font.Style ^ DrawingFontStyles.Underline);
                }
                RaisePropertyChanged("IsUnderline");
            }
        }

        public GeoBrush FontColor
        {
            get
            {
                return actualTextStyle.Advanced.TextCustomBrush != null ? actualTextStyle.Advanced.TextCustomBrush : actualTextStyle.TextSolidBrush;
            }
            set
            {
                if (value is GeoSolidBrush)
                {
                    actualTextStyle.TextSolidBrush = (GeoSolidBrush)value;
                    actualTextStyle.Advanced.TextCustomBrush = null;
                }
                else
                {
                    actualTextStyle.Advanced.TextCustomBrush = value;
                }
                RaisePropertyChanged("FontColor");
            }
        }

        public float OffsetX
        {
            get
            {
                return actualTextStyle.XOffsetInPixel;
            }
            set
            {
                actualTextStyle.XOffsetInPixel = value;
                RaisePropertyChanged("OffsetX");
            }
        }

        public float OffsetY
        {
            get
            {
                return actualTextStyle.YOffsetInPixel;
            }
            set
            {
                actualTextStyle.YOffsetInPixel = value;
                RaisePropertyChanged("OffsetY");
            }
        }

        public double RotationAngle
        {
            get
            {
                return actualTextStyle.RotationAngle;
            }
            set
            {
                actualTextStyle.RotationAngle = value;
                RaisePropertyChanged("RotationAngle");
            }
        }

        public bool FittingPolygon
        {
            get
            {
                return actualTextStyle.FittingPolygon;
            }
            set
            {
                actualTextStyle.FittingPolygon = value;
                RaisePropertyChanged("FittingPolygon");
            }
        }

        public bool EnableHalo
        {
            get
            {
                return actualTextStyle.IsHaloEnabled;
            }
            set
            {
                actualTextStyle.IsHaloEnabled = value;
                //float haloWidth = 3;
                //if (actualTextStyle.HaloPen != null)
                //{
                //    haloWidth = actualTextStyle.HaloPen.Width;
                //}

                //if (value)
                //{
                //    actualTextStyle.HaloPen = new GeoPen(GeoColor.SimpleColors.White, haloWidth);
                //}
                //else
                //{
                //    actualTextStyle.HaloPen = new GeoPen();
                //    actualTextStyle.HaloPen.Width = haloWidth;
                //}
                RaisePropertyChanged("EnableHalo");
                //RaisePropertyChanged("HaloColor");
                //RaisePropertyChanged("HaloWidth");
            }
        }

        public float HaloWidth
        {
            get
            {
                return actualTextStyle.HaloPen.Width;
            }
            set
            {
                actualTextStyle.HaloPen.Width = value;
                RaisePropertyChanged("HaloWidth");
            }
        }

        public GeoBrush HaloColor
        {
            get
            {
                return actualTextStyle.HaloPen.Brush;
            }
            set
            {
                actualTextStyle.HaloPen.Brush = value;
                RaisePropertyChanged("HaloColor");
            }
        }

        public bool EnableMask
        {
            get
            {
                return actualTextStyle.IsHaloEnabled;
            }
            set
            {
                actualTextStyle.IsHaloEnabled = value;
                if (value && actualTextStyle.Mask == null)
                {
                    actualTextStyle.Mask = AreaStyles.CreateSimpleAreaStyle(GeoColor.SimpleColors.White, GeoColor.FromHtml("#808080"));
                    actualTextStyle.Mask.DrawingLevel = DrawingLevel.LabelLevel;
                }
                //if (value)
                //{
                //    actualTextStyle.Mask = AreaStyles.CreateSimpleAreaStyle(GeoColor.SimpleColors.White, GeoColor.FromHtml("#808080"));
                //    actualTextStyle.Mask.DrawingLevel = DrawingLevel.LabelLevel;
                //}
                //else
                //{
                //    actualTextStyle.Mask = null;
                //}
                RaisePropertyChanged("EnableMask");
                //RaisePropertyChanged("OutlineColor");
                //RaisePropertyChanged("OutlineThickness");
                //RaisePropertyChanged("FillColor");
                //RaisePropertyChanged("Margin");
            }
        }

        public GeoBrush OutlineColor
        {
            get
            {
                GeoBrush brush = new GeoSolidBrush();
                if (actualTextStyle.Mask != null)
                {
                    brush = actualTextStyle.Mask.OutlinePen.Brush;
                }
                return brush;
            }
            set
            {
                if (actualTextStyle.Mask != null)
                {
                    actualTextStyle.Mask.OutlinePen.Brush = value;
                }
                RaisePropertyChanged("OutlineColor");
            }
        }

        public float OutlineThickness
        {
            get
            {
                float width = 0;
                if (actualTextStyle.Mask != null)
                {
                    width = actualTextStyle.Mask.OutlinePen.Width;
                }
                return width;
            }
            set
            {
                if (actualTextStyle.Mask != null)
                {
                    actualTextStyle.Mask.OutlinePen.Width = value;
                }
                RaisePropertyChanged("OutlineThickness");
            }
        }

        public GeoBrush FillColor
        {
            get
            {
                GeoBrush brush = new GeoSolidBrush();
                if (actualTextStyle.Mask != null)
                {
                    brush = actualTextStyle.Mask.Advanced.FillCustomBrush != null ? actualTextStyle.Mask.Advanced.FillCustomBrush : actualTextStyle.Mask.FillSolidBrush;
                }
                return brush;
            }
            set
            {
                if (actualTextStyle.Mask != null)
                {
                    if (value is GeoSolidBrush)
                    {
                        actualTextStyle.Mask.FillSolidBrush = (GeoSolidBrush)value;
                        actualTextStyle.Mask.Advanced.FillCustomBrush = null;
                    }
                    else
                    {
                        actualTextStyle.Mask.Advanced.FillCustomBrush = value;
                    }
                }
                RaisePropertyChanged("FillColor");
            }
        }

        public int Margin
        {
            get
            {
                return actualTextStyle.MaskMargin;
            }
            set
            {
                actualTextStyle.MaskMargin = value;
                RaisePropertyChanged("Margin");
            }
        }

        public bool AllowOverlapping
        {
            get
            {
                return actualTextStyle.OverlappingRule == LabelOverlappingRule.AllowOverlapping;
            }
            set
            {
                actualTextStyle.OverlappingRule = value ? LabelOverlappingRule.AllowOverlapping : LabelOverlappingRule.NoOverlapping;
                RaisePropertyChanged("AllowOverlapping");
            }
        }

        public int GridSize
        {
            get
            {
                return actualTextStyle.GridSize;
            }
            set
            {
                actualTextStyle.GridSize = value;
                RaisePropertyChanged("GridSize");
            }
        }

        public Collection<int> GridSizes
        {
            get
            {
                return gridSizes;
            }
        }

        public BoundItem DateFormat
        {
            get
            {
                if (actualTextStyle.DateFormat == null)
                {
                    return dataFormats.FirstOrDefault(df => df.Text.Equals("None"));
                }
                else
                {
                    return dataFormats.FirstOrDefault(df => df.Value.Equals(actualTextStyle.DateFormat));
                }
            }
            set
            {
                if (featureSourceColumns == null)
                {
                    actualTextStyle.DateFormat = null;
                }
                else
                {
                    var featureSourceColumn = featureSourceColumns.FirstOrDefault(column => column.ColumnName.Equals(TextColumnName));
                    if (featureSourceColumn != null)
                    {
                        if (featureSourceColumn.TypeName.Equals("Date"))
                        {
                            actualTextStyle.DateFormat = value.Value;
                        }
                        else
                        {
                            actualTextStyle.DateFormat = null;
                        }
                    }
                    else
                    {
                        actualTextStyle.DateFormat = null;
                    }
                }
                RaisePropertyChanged("DateFormat");
            }
        }

        public string CustomFormat
        {
            get { return actualTextStyle.NumericFormat; }
            set
            {
                actualTextStyle.NumericFormat = value;
                RaisePropertyChanged("CustomFormat");
            }
        }

        public bool CustomFormatIsEnabled
        {
            get { return NumericFormat != null && NumericFormat.Text == "Custom"; }
        }

        public BoundItem NumericFormat
        {
            get
            {
                BoundItem boundItem = null;
                if (actualTextStyle.NumericFormat == null)
                {
                    boundItem = numericFormats.FirstOrDefault(nf => nf.Text.Equals("None"));
                }
                else
                {
                    boundItem = numericFormats.FirstOrDefault(nf => nf.Value.Equals(actualTextStyle.NumericFormat));
                }
                if (boundItem == null)
                {
                    boundItem = numericFormats.FirstOrDefault(nf => nf.Text.Equals("Custom"));
                }
                return boundItem;
            }
            set
            {
                if (featureSourceColumns == null)
                {
                    actualTextStyle.NumericFormat = null;
                }
                else
                {
                    var featureSourceColumn = featureSourceColumns.FirstOrDefault(column => column.ColumnName.Equals(TextColumnName));
                    if (featureSourceColumn != null)
                    {
                        if (featureSourceColumn.TypeName.Equals("Double")
                            || featureSourceColumn.TypeName.Equals("Float")
                            || featureSourceColumn.TypeName.Equals("Integer")
                            || featureSourceColumn.TypeName.Equals("Numeric")
                            || featureSourceColumn.TypeName.Equals("String")
                            || featureSourceColumn.TypeName.Equals("Character"))
                        {
                            actualTextStyle.NumericFormat = value.Value;
                        }
                        else
                        {
                            actualTextStyle.NumericFormat = null;
                        }
                    }
                    else
                    {
                        actualTextStyle.NumericFormat = null;
                    }
                    RaisePropertyChanged("NumericFormat");
                }
                RaisePropertyChanged("CustomFormat");
                RaisePropertyChanged("CustomFormatIsEnabled");
            }
        }

        public bool LabelAllPolygonParts
        {
            get
            {
                return actualTextStyle.LabelAllPolygonParts;
            }
            set
            {
                actualTextStyle.LabelAllPolygonParts = value;
                RaisePropertyChanged("LabelAllPolygonParts");
            }
        }

        public LabelDuplicateRule CurrentDuplicateRule
        {
            get
            {
                return actualTextStyle.DuplicateRule;
            }
            set
            {
                actualTextStyle.DuplicateRule = value;
                RaisePropertyChanged("CurrentDuplicateRule");
            }
        }

        public bool SuppressPartialLabels
        {
            get
            {
                return actualTextStyle.SuppressPartialLabels;
            }
            set
            {
                actualTextStyle.SuppressPartialLabels = value;
                RaisePropertyChanged("SuppressPartialLabels");
            }
        }

        public double FittingPolygonFactor
        {
            get
            {
                return actualTextStyle.FittingPolygonFactor;
            }
            set
            {
                actualTextStyle.FittingPolygonFactor = value;
                RaisePropertyChanged("FittingPolygonFactor");
            }
        }

        public bool ForceHorizontalLabelForLine
        {
            get
            {
                return actualTextStyle.ForceHorizontalLabelForLine;
            }
            set
            {
                actualTextStyle.ForceHorizontalLabelForLine = value;
                RaisePropertyChanged("ForceHorizontalLabelForLine");
            }
        }

        public SplineType CurrentSplineType
        {
            get
            {
                return actualTextStyle.SplineType;
            }
            set
            {
                actualTextStyle.SplineType = value;
                RaisePropertyChanged("CurrentSplineType");
            }
        }

        public bool BestPlacement
        {
            get
            {
                return actualTextStyle.BestPlacement;
            }
            set
            {
                actualTextStyle.BestPlacement = value;
                RaisePropertyChanged("BestPlacement");
                RaisePropertyChanged("LabelPlacementIsEnabled");
            }
        }

        public PointPlacement CurrentPointPlacement
        {
            get
            {
                return actualTextStyle.PointPlacement;
            }
            set
            {
                actualTextStyle.PointPlacement = value;
                RaisePropertyChanged("CurrentPointPlacement");
            }
        }

        public bool FittingLineInScreen
        {
            get
            {
                return actualTextStyle.FittingLineInScreen;
            }
            set
            {
                actualTextStyle.FittingLineInScreen = value;
                RaisePropertyChanged("FittingLineInScreen");
            }
        }

        public double TextLineSegmentRatio
        {
            get
            {
                return actualTextStyle.TextLineSegmentRatio;
            }
            set
            {
                actualTextStyle.TextLineSegmentRatio = value;
                RaisePropertyChanged("TextLineSegmentRatio");
            }
        }

        public string IconFilePathName
        {
            get
            {
                return String.IsNullOrEmpty(imagePath) ? ((IconStyle)actualTextStyle).IconFilePathName : imagePath;
            }
            set
            {
                imagePath = value;
                if (!String.IsNullOrEmpty(value) && StyleHelper.IsImageValid(value))
                {
                    //((IconStyle)actualTextStyle).IconFilePathName = value;

                    var stream = new System.IO.MemoryStream(System.IO.File.ReadAllBytes(value));
                    ((IconStyle)actualTextStyle).IconImage = new GeoImage(stream);
                    RaisePropertyChanged("IconFilePathName");
                }
                else
                {
                    ((IconStyle)actualTextStyle).IconFilePathName = null;
                    ((IconStyle)actualTextStyle).IconImage = null;
                    RaisePropertyChanged("IconFilePathName");
                }
            }
        }

        public double IconImageScale
        {
            get
            {
                return ((IconStyle)actualTextStyle).IconImageScale;
            }
            set
            {
                if (value > 0 && value < 10000)
                    ((IconStyle)actualTextStyle).IconImageScale = value;
                else
                {
                    System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("TextStyleViewModelMustBiggerMessage"), "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                }
                RaisePropertyChanged("IconImageScale");
            }
        }

        public Visibility ForceHorizontalLabelForLineVisibility
        {
            get { return forceHorizontalLabelForLineVisibility; }
            set
            {
                forceHorizontalLabelForLineVisibility = value;
                RaisePropertyChanged("ForceHorizontalLabelForLineVisibility");
            }
        }

        public Visibility SplineTypeVisibility
        {
            get { return splineTypeVisibility; }
            set
            {
                splineTypeVisibility = value;
                RaisePropertyChanged("SplineTypeVisibility");
            }
        }

        public Visibility LabelPolygonVisibility
        {
            get { return labelPolygonVisibility; }
            set
            {
                labelPolygonVisibility = value;
                RaisePropertyChanged("LabelPolygonVisibility");
            }
        }

        public Visibility FittingPolygonVisibility
        {
            get { return fittingPolygonVisibility; }
            set
            {
                fittingPolygonVisibility = value;
                RaisePropertyChanged("FittingPolygonVisibility");
            }
        }

        public Visibility FittingFactorVisibility
        {
            get { return fittingFactorVisibility; }
            set
            {
                fittingFactorVisibility = value;
                RaisePropertyChanged("FittingFactorVisibility");
            }
        }

        public Visibility FittingLineInScreenVisibility
        {
            get { return fittingLineInScreenVisibility; }
            set
            {
                fittingLineInScreenVisibility = value;
                RaisePropertyChanged("FittingLineInScreenVisibility");
            }
        }

        public Visibility SegmentRatioVisibility
        {
            get { return segmentRatioVisibility; }
            set
            {
                segmentRatioVisibility = value;
                RaisePropertyChanged("SegmentRatioVisibility");
            }
        }

        public Visibility BestPlacementVisibility
        {
            get { return bestPlacementVisibility; }
            set
            {
                bestPlacementVisibility = value;
                RaisePropertyChanged("BestPlacementVisibility");
            }
        }

        public Visibility LabelPlacementVisibility
        {
            get { return labelPlacementVisibility; }
            set
            {
                labelPlacementVisibility = value;
                RaisePropertyChanged("LabelPlacementVisibility");
            }
        }

        public bool LabelPlacementIsEnabled
        {
            get { return !BestPlacement; }
        }

        public bool DateFormatIsEnabled
        {
            get { return dateFormatIsEnabled; }
            set
            {
                dateFormatIsEnabled = value;
                RaisePropertyChanged("DateFormatIsEnabled");
            }
        }

        public bool NumericFormatIsEnabled
        {
            get { return numericFormatIsEnabled; }
            set
            {
                numericFormatIsEnabled = value;
                RaisePropertyChanged("NumericFormatIsEnabled");
            }
        }

        public Visibility BasicLabelGridVisibility
        {
            get { return basicLabelGridVisibility; }
            set
            {
                basicLabelGridVisibility = value;
                RaisePropertyChanged("BasicLabelGridVisibility");
            }
        }

        public Visibility LabelFunctionGridVisibility
        {
            get { return labelFunctionGridVisibility; }
            set
            {
                labelFunctionGridVisibility = value;
                RaisePropertyChanged("LabelFunctionGridVisibility");
            }
        }

        public Visibility CustomLabelGridVisibility
        {
            get { return customLabelGridVisibility; }
            set
            {
                customLabelGridVisibility = value;
                RaisePropertyChanged("CustomLabelGridVisibility");
            }
        }

        public bool ContextMenuIsOpen
        {
            get { return contextMenuIsOpen; }
            set
            {
                contextMenuIsOpen = value;
                RaisePropertyChanged("ContextMenuIsOpen");
            }
        }

        public bool BasicLabelIsChecked
        {
            get
            {
                return basicLabelIsChecked;
            }
            set
            {
                basicLabelIsChecked = value;
                if (value)
                {
                    BasicLabelGridVisibility = Visibility.Visible;
                    CustomLabelGridVisibility = Visibility.Collapsed;
                    LabelFunctionGridVisibility = Visibility.Collapsed;

                    DateFormatIsEnabled = true;
                    NumericFormatIsEnabled = true;
                }

                RaisePropertyChanged("IsCustomText");
                RaisePropertyChanged("BasicLabelIsChecked");
            }
        }

        public RelayCommand InsertCommand
        {
            get
            {
                if (insertCommand == null)
                {
                    insertCommand = new RelayCommand(() =>
                    {
                        ContextMenuIsOpen = true;
                    });
                }
                return insertCommand;
            }
        }

        public RelayCommand LabelFunctionsCommand
        {
            get
            {
                if (labelFunctionsCommand == null)
                {
                    labelFunctionsCommand = new RelayCommand(() =>
                    {
                        LabelFunctionsWindow window = new LabelFunctionsWindow(ColumnNames, actualTextStyle.LabelFunctionsScript, LabelFunctionColumnNames);
                        window.Owner = Application.Current.MainWindow;
                        if (window.ShowDialog().GetValueOrDefault())
                        {
                            actualTextStyle.LabelFunctionColumnNames.Clear();
                            string text = window.CodeText;
                            actualTextStyle.LabelFunctionsScript = text;
                            foreach (var item in window.LabelFunctionColumnNames)
                            {
                                actualTextStyle.TextColumnName = item.Value;
                                actualTextStyle.LabelFunctionColumnNames.Add(item.Key, item.Value);
                                if (text.Contains(item.Key))
                                {
                                    text = text.Replace(item.Key, "[" + item.Value + "]");
                                }
                            }
                            FunctionText = text;
                        }
                    });
                }
                return labelFunctionsCommand;
            }
        }

        public Dictionary<string, string> LabelFunctionColumnNames
        {
            get { return actualTextStyle.LabelFunctionColumnNames; }
        }

        public string FunctionText
        {
            get { return functionText; }
            set
            {
                functionText = value;
                RaisePropertyChanged("FunctionText");
            }
        }

        private void ChangeInnerControlsVisibility()
        {
            var shapeType = StylePluginHelper.GetWellKnownType(RequiredValues.FeatureLayer);
            switch (shapeType)
            {
                case SimpleShapeType.Point:

                    // Placement Visibility
                    ForceHorizontalLabelForLineVisibility = Visibility.Collapsed;
                    SplineTypeVisibility = Visibility.Collapsed;

                    // Duplication Visiibility
                    LabelPolygonVisibility = Visibility.Collapsed;

                    // Clipping
                    FittingPolygonVisibility = Visibility.Collapsed;
                    FittingFactorVisibility = Visibility.Collapsed;
                    FittingLineInScreenVisibility = Visibility.Collapsed;
                    SegmentRatioVisibility = Visibility.Collapsed;
                    break;

                case SimpleShapeType.Line:

                    // Placement Visibility
                    BestPlacementVisibility = Visibility.Collapsed;
                    LabelPlacementVisibility = Visibility.Collapsed;

                    // Duplication Visibility
                    LabelPolygonVisibility = Visibility.Collapsed;

                    // Clipping
                    FittingPolygonVisibility = Visibility.Collapsed;
                    FittingFactorVisibility = Visibility.Collapsed;
                    break;

                case SimpleShapeType.Area:

                    // Placement Visibility
                    ForceHorizontalLabelForLineVisibility = Visibility.Collapsed;
                    SplineTypeVisibility = Visibility.Collapsed;
                    BestPlacementVisibility = Visibility.Collapsed;
                    LabelPlacementVisibility = Visibility.Collapsed;

                    // Clipping
                    FittingLineInScreenVisibility = Visibility.Collapsed;
                    SegmentRatioVisibility = Visibility.Collapsed;
                    break;

                default:
                    break;
            }
        }

        public DrawingLevel DrawingLevel
        {
            get { return actualTextStyle.DrawingLevel; }
            set { actualTextStyle.DrawingLevel = value; }
        }

        protected override void StyleViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.Equals("DateFormat")
                && !e.PropertyName.Equals("NumericFormat")
                && !e.PropertyName.Equals("BasicLabelGridVisibility")
                && !e.PropertyName.Equals("BasicLabelIsChecked")
                && !e.PropertyName.Equals("CustomLabelGridVisibility")
                && !e.PropertyName.Equals("IsCustomText")
                && !e.PropertyName.Equals("LabelFunctionGridVisibility")
                && !e.PropertyName.Equals("IsLabelFunctions")
                && CanRefreshPreviewSource(e.PropertyName))
            {
                //if actualTextStyle.NumericFormat or actualTextStyle.DateFormat has an
                //available value, it would throw an exception while getting preview
                string numericFormatBackup = actualTextStyle.NumericFormat;
                string dateFormatBackup = actualTextStyle.DateFormat;
                actualTextStyle.NumericFormat = null;
                actualTextStyle.DateFormat = null;

                Stream stream = null;

                if (actualTextStyle.IconImage != null)
                    stream = actualTextStyle.IconImage.GetImageStream();

                if (!string.IsNullOrEmpty(IconFilePathName))
                {
                    stream = new MemoryStream(File.ReadAllBytes(IconFilePathName));
                }

                PreviewSource = null;

                if (stream != null)
                {
                    var bitmap = new System.Drawing.Bitmap(System.Drawing.Image.FromStream(stream));
                    MemoryStream ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Png);
                    ms.Seek(0, SeekOrigin.Begin);
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();

                    PreviewSource = bitmapImage;
                }

                actualTextStyle.NumericFormat = numericFormatBackup;
                actualTextStyle.DateFormat = dateFormatBackup;
            }
        }
    }
}