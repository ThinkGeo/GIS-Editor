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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ScaleLineElementViewModel : ViewModelBase
    {
        private MapPrinterLayer mapPrinterLayer;

        private AreaStyle backgroundStyle;

        private PrinterDragMode dragMode;

        private ScaleLineUnitSystem selectedUnitSystem;

        [NonSerialized]
        private BitmapImage preview;

        private ObservedCommand configureCommand;

        public ScaleLineElementViewModel(MapPrinterLayer mapPrinterLayer)
        {
            PropertyChanged += ScaleLineElementEntity_PropertyChanged;
            this.mapPrinterLayer = mapPrinterLayer;
            dragMode = PrinterDragMode.Draggable;
            selectedUnitSystem = ScaleLineUnitSystem.ImperialAndMetric;
            BackgroundStyle = new AreaStyle();
            BackgroundStyle.CustomAreaStyles.Add(new AreaStyle(new GeoSolidBrush(GeoColor.StandardColors.Transparent)));
        }

        public AreaStyle BackgroundStyle
        {
            get { return backgroundStyle; }
            set
            {
                backgroundStyle = value;
                RaisePropertyChanged(()=>BackgroundStyle);
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

        public MapPrinterLayer MapPrinterLayer
        {
            get { return mapPrinterLayer; }
        }

        public ScaleLineUnitSystem SelectedUnitSystem
        {
            get { return selectedUnitSystem; }
            set
            {
                selectedUnitSystem = value;
                RaisePropertyChanged(()=>SelectedUnitSystem);
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
                        if (BackgroundStyle == null)
                        {
                            BackgroundStyle = new AreaStyle();
                            BackgroundStyle.CustomAreaStyles.Add(new AreaStyle(new GeoSolidBrush(GeoColor.StandardColors.Transparent)));
                        }

                        AreaStyle editingAreaStyle = (AreaStyle)BackgroundStyle.CloneDeep();
                        editingAreaStyle.Name = GisEditor.StyleManager.GetStylePluginByStyle(editingAreaStyle).Name;
                        if (editingAreaStyle.CustomAreaStyles.Count > 0)
                        {
                            editingAreaStyle.CustomAreaStyles.ForEach(c => c.Name = GisEditor.StyleManager.GetStylePluginByStyle(c).Name);
                        }
                        StyleBuilderArguments args = new StyleBuilderArguments();
                        args.AvailableStyleCategories = StyleCategories.Area;
                        args.AvailableUIElements = StyleBuilderUIElements.StyleList;

                        args.AppliedCallback = (editResult) =>
                        {
                            var areaStyle = new AreaStyle();
                            foreach (var item in editResult.CompositeStyle.Styles.OfType<AreaStyle>())
                            {
                                areaStyle.CustomAreaStyles.Add(item);
                            }
                            ApplyScaleLineStyle(areaStyle);
                        };

                        var resultStyle = GisEditor.StyleManager.EditStyles(args, editingAreaStyle);
                        if (resultStyle != null)
                        {
                            ApplyScaleLineStyle(resultStyle);
                        }
                    }, () => true);
                }
                return configureCommand;
            }
        }

        private void ApplyScaleLineStyle(AreaStyle style)
        {
            style.DrawingLevel = DrawingLevel.LabelLevel;
            foreach (var item in style.CustomAreaStyles)
            {
                item.DrawingLevel = DrawingLevel.LabelLevel;
            }
            BackgroundStyle.Name = style.Name;
            BackgroundStyle = style;
        }

        internal static bool IsValid(ScaleLinePrinterLayer scaleLinePrinterLayer)
        {
            bool scaleLineValid = true;
            if (!scaleLinePrinterLayer.MapPrinterLayer.IsOpen) scaleLinePrinterLayer.MapPrinterLayer.Open();
            double resolution = scaleLinePrinterLayer.MapPrinterLayer.MapExtent.Width / scaleLinePrinterLayer.MapPrinterLayer.GetBoundingBox().Width;
            if (!scaleLinePrinterLayer.IsOpen)
            {
                scaleLinePrinterLayer.Open();
            }

            // use to campare use with unit to display.
            int maxBarLength = (int)scaleLinePrinterLayer.GetBoundingBox().Width;
            long maxSizeData = Convert.ToInt64(maxBarLength * resolution * GetInchesPreUnit(scaleLinePrinterLayer.MapPrinterLayer.MapUnit));

            DistanceUnit bottomUnits = DistanceUnit.Feet;
            DistanceUnit topUnits = DistanceUnit.Meter;
            if (maxSizeData > 100000)
            {
                topUnits = DistanceUnit.Kilometer;
                bottomUnits = DistanceUnit.Mile;
            }

            float topMax = Convert.ToInt32(maxSizeData / GetInchesPreUnit(topUnits));
            float bottomMax = Convert.ToInt32(maxSizeData / GetInchesPreUnit(bottomUnits));

            // now trim this down to useful block length
            int topRounded = GetBarLength((int)topMax);
            int bottomRounded = GetBarLength((int)bottomMax);

            if (topRounded < 2 && topUnits == DistanceUnit.Meter) scaleLineValid = false;
            return scaleLineValid;
        }

        private static double GetInchesPreUnit(DistanceUnit targetUnit)
        {
            double returnValue = 0;

            switch (targetUnit)
            {
                case DistanceUnit.Meter:
                    returnValue = 39.3701;
                    break;

                case DistanceUnit.Feet:
                    returnValue = 12;
                    break;

                case DistanceUnit.Kilometer:
                    returnValue = 39370.1;
                    break;

                case DistanceUnit.Mile:
                    returnValue = 63360;
                    break;

                case DistanceUnit.UsSurveyFeet:
                    break;

                case DistanceUnit.Yard:
                    returnValue = 36;
                    break;
                default:
                    break;
            }

            return returnValue;
        }

        private static double GetInchesPreUnit(GeographyUnit targetUnit)
        {
            double returnValue = 0;

            switch (targetUnit)
            {
                case GeographyUnit.Unknown:
                    break;

                case GeographyUnit.DecimalDegree:
                    returnValue = 4374754;
                    break;

                case GeographyUnit.Feet:
                    returnValue = 12;
                    break;

                case GeographyUnit.Meter:
                    returnValue = 39.3701;
                    break;
                default:
                    break;
            }

            return returnValue;
        }

        private static int GetBarLength(int maxLength)
        {
            string maxLengthString = maxLength.ToString(CultureInfo.InvariantCulture);

            int firstTwoChars;
            if (maxLengthString.Length > 1)
            {
                firstTwoChars = Convert.ToInt32(maxLengthString.Substring(0, 2), CultureInfo.InvariantCulture);
            }
            else
            {
                firstTwoChars = Convert.ToInt32(maxLengthString, CultureInfo.InvariantCulture);
                firstTwoChars *= 10;
            }

            // put it into the correct bracket
            string returnValueString = string.Empty;
            if (firstTwoChars > 45)
            {
                returnValueString = "5";
            }
            else if (firstTwoChars > 15)
            {
                returnValueString = "2";
            }
            else
            {
                returnValueString = "1";
            }

            for (int i = 0; i < maxLengthString.Length - 1; i++)
            {
                returnValueString += "0";
            }

            // scale it up the correct power of 10
            return Convert.ToInt32(returnValueString, CultureInfo.InvariantCulture);
        }

        private void ScaleLineElementEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Preview" || (MapPrinterLayer.MapUnit == GeographyUnit.DecimalDegree && !PrinterLayerHelper.CheckDecimalDegreeIsInRange(MapPrinterLayer.MapExtent))) return;
            if (!MapPrinterLayer.IsOpen) MapPrinterLayer.Open();
            try
            {
                ScaleLinePrinterLayer scaleLinePrinterLayer = new ScaleLinePrinterLayer(MapPrinterLayer);
                scaleLinePrinterLayer.BackgroundMask = BackgroundStyle as AreaStyle;
                scaleLinePrinterLayer.MapUnit = MapPrinterLayer.MapUnit;
                scaleLinePrinterLayer.UnitSystem = SelectedUnitSystem;
                var boundingBox = MapPrinterLayer.GetBoundingBox();
                var pageCenter = boundingBox.GetCenterPoint();
                scaleLinePrinterLayer.SetPosition(1.25, .25, -3.25, -2.25, PrintingUnit.Inch);
                using (Bitmap bitmap = new Bitmap(220, 60))
                {
                    PlatformGeoCanvas canvas = new PlatformGeoCanvas();
                    scaleLinePrinterLayer.SafeProcess(() =>
                    {
                        var scaleLineBoundingBox = scaleLinePrinterLayer.GetBoundingBox();
                        canvas.BeginDrawing(bitmap, scaleLineBoundingBox, GeographyUnit.Meter);
                        scaleLinePrinterLayer.Draw(canvas, new Collection<SimpleCandidate>());
                    });

                    //scaleLinePrinterLayer.Open();
                    //var scaleLineBoundingBox = scaleLinePrinterLayer.GetBoundingBox();
                    //canvas.BeginDrawing(bitmap, scaleLineBoundingBox, GeographyUnit.Meter);
                    //scaleLinePrinterLayer.Draw(canvas, new Collection<SimpleCandidate>());
                    //scaleLinePrinterLayer.Close();
                    canvas.EndDrawing();
                    MemoryStream ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Png);

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();
                    Preview = bitmapImage;
                }
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
        }
    }
}