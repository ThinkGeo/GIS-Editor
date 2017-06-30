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
using System.Linq;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ScaleBarElementViewModel : ViewModelBase
    {
        private MapPrinterLayer mapPrinterLayer;

        private AreaStyle background;

        private GeoBrush color;

        private GeoBrush alternatingColor;

        private UnitSystem selectedUnitSystem;

        private PrinterResizeMode resizeMode;

        private PrinterDragMode dragMode;

        [NonSerialized]
        private BitmapImage preview;

        private ObservedCommand configureCommand;

        private ScaleNumericFormatType selectedNumericFormatType;

        public ScaleBarElementViewModel(MapPrinterLayer mapPrinterLayer)
        {
            this.mapPrinterLayer = mapPrinterLayer;
            background = new AreaStyle();
            background.CustomAreaStyles.Add(new AreaStyle(new GeoSolidBrush(GeoColor.StandardColors.Transparent)));
            selectedUnitSystem = UnitSystem.Metric;
            color = new GeoSolidBrush(GeoColor.StandardColors.Black);
            alternatingColor = new GeoSolidBrush(GeoColor.StandardColors.White);
            resizeMode = PrinterResizeMode.Resizable;
            dragMode = PrinterDragMode.Draggable;
            PropertyChanged += ScaleBarViewModelPropertyChanged;
            SelectedNumericFormatType = ScaleNumericFormatType.None;
        }

        public MapPrinterLayer MapPrinterLayer
        {
            get { return mapPrinterLayer; }
        }

        public AreaStyle Background
        {
            get { return background; }
            set
            {
                background = value;
                RaisePropertyChanged(()=>Background);
            }
        }

        public GeoBrush Color
        {
            get { return color; }
            set
            {
                color = value;
                RaisePropertyChanged(()=>Color);
            }
        }

        public GeoBrush AlternatingColor
        {
            get { return alternatingColor; }
            set
            {
                alternatingColor = value;
                RaisePropertyChanged(()=>AlternatingColor);
            }
        }

        public UnitSystem SelectedUnitSystem
        {
            get { return selectedUnitSystem; }
            set
            {
                selectedUnitSystem = value;
                RaisePropertyChanged(()=>SelectedUnitSystem);
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

        public BitmapImage Preview
        {
            get { return preview; }
            set
            {
                preview = value;
                RaisePropertyChanged(()=>Preview);
            }
        }

        public ObservedCommand ConfigureCommand
        {
            get
            {
                if (configureCommand == null)
                {
                    configureCommand = new ObservedCommand(() =>
                    {
                        if (Background == null)
                        {
                            Background = new AreaStyle();
                            Background.CustomAreaStyles.Add(new AreaStyle(new GeoSolidBrush(GeoColor.StandardColors.Transparent)));
                        }

                        AreaStyle editingStyle = (AreaStyle)background.CloneDeep();
                        StyleBuilderArguments args = new StyleBuilderArguments();
                        editingStyle.Name = GisEditor.StyleManager.GetStylePluginByStyle(editingStyle).Name;
                        args.AvailableUIElements = StyleBuilderUIElements.StyleList;
                        args.AvailableStyleCategories = StyleCategories.Area;
                        if (editingStyle.CustomAreaStyles.Count > 0)
                        {
                            editingStyle.CustomAreaStyles.ForEach(c => c.Name = GisEditor.StyleManager.GetStylePluginByStyle(c).Name);
                        }

                        args.AppliedCallback = (argsResult) =>
                        {
                            var areaStyle = new AreaStyle();
                            foreach (var item in argsResult.CompositeStyle.Styles.OfType<AreaStyle>())
                            {
                                areaStyle.CustomAreaStyles.Add(item);
                            }
                            ApplyScaleBarStyle(areaStyle);
                        };

                        AreaStyle resultStyle = GisEditor.StyleManager.EditStyles(args, editingStyle);
                        if (resultStyle != null)
                        {
                            ApplyScaleBarStyle(resultStyle);
                        }
                    }, () => true);
                }
                return configureCommand;
            }
        }

        public ScaleNumericFormatType SelectedNumericFormatType
        {
            get { return selectedNumericFormatType; }
            set
            {
                selectedNumericFormatType = value;
                RaisePropertyChanged(()=>SelectedNumericFormatType);
            }
        }

        public string NumericFormatString
        {
            get
            {
                switch (SelectedNumericFormatType)
                {
                    case ScaleNumericFormatType.Currency:
                        return "C";
                    case ScaleNumericFormatType.Decimal:
                        return "D";
                    case ScaleNumericFormatType.Scientific:
                        return "E";
                    case ScaleNumericFormatType.FixedPoint:
                        return "F";
                    case ScaleNumericFormatType.General:
                        return "G";
                    case ScaleNumericFormatType.Number:
                        return "N";
                    case ScaleNumericFormatType.Percent:
                        return "P";
                    case ScaleNumericFormatType.RoundTrip:
                        return "R";
                    case ScaleNumericFormatType.Hexadecimal:
                        return "X";
                    case ScaleNumericFormatType.None:
                    default:
                        return "";
                }
            }
        }

        private void ApplyScaleBarStyle(AreaStyle style)
        {
            style.DrawingLevel = DrawingLevel.LabelLevel;
            foreach (var item in style.CustomAreaStyles)
            {
                item.DrawingLevel = DrawingLevel.LabelLevel;
            }
            Background.Name = style.Name;
            Background = style;
        }

        private void ScaleBarViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Preview")
            {
                ScaleBarPrinterLayer scaleBarPrinterLayer = new ScaleBarPrinterLayer(MapPrinterLayer);
                scaleBarPrinterLayer.BackgroundMask = Background as AreaStyle;
                scaleBarPrinterLayer.BarBrush = Color;
                scaleBarPrinterLayer.AlternateBarBrush = AlternatingColor;
                scaleBarPrinterLayer.TextStyle.NumericFormat = NumericFormatString;
                scaleBarPrinterLayer.UnitFamily = SelectedUnitSystem;
                scaleBarPrinterLayer.DragMode = DragMode;
                scaleBarPrinterLayer.ResizeMode = ResizeMode;
                scaleBarPrinterLayer.Open();
                scaleBarPrinterLayer.MapUnit = MapPrinterLayer.MapUnit;
                scaleBarPrinterLayer.SetPosition(1.25, .25, -3.25, -2.25, PrintingUnit.Inch);
                var boundingBox = scaleBarPrinterLayer.GetBoundingBox();
                //using (Bitmap bitmap = new Bitmap((int)boundingBox.Width, (int)boundingBox.Height))
                using (Bitmap bitmap = new Bitmap(210, 56))
                {
                    var ms = new MemoryStream();
                    var gdiPlusGeoCanvas = new PlatformGeoCanvas();
                    gdiPlusGeoCanvas.BeginDrawing(bitmap, boundingBox, GeographyUnit.Feet);
                    scaleBarPrinterLayer.Draw(gdiPlusGeoCanvas, new Collection<SimpleCandidate>());
                    gdiPlusGeoCanvas.EndDrawing();
                    bitmap.Save(ms, ImageFormat.Png);
                    BitmapImage previewImage = new BitmapImage();
                    previewImage.BeginInit();
                    previewImage.StreamSource = ms;
                    previewImage.EndInit();
                    Preview = previewImage;
                }
            }
        }
    }
}