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


using System.Linq;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class Exstension
    {
        internal static void SetDrawingLevel(this LineStyle lineStyle)
        {
            lineStyle.CenterPenDrawingLevel = DrawingLevel.LabelLevel;
            lineStyle.InnerPenDrawingLevel = DrawingLevel.LabelLevel;
            lineStyle.OuterPenDrawingLevel = DrawingLevel.LabelLevel;
            foreach (var customLineStyle in lineStyle.CustomLineStyles)
            {
                customLineStyle.SetDrawingLevel();
            }
        }

        internal static void SetDrawingLevel(this PointStyle pointStyle)
        {
            pointStyle.DrawingLevel = DrawingLevel.LabelLevel;
            foreach (var customPointStyle in pointStyle.CustomPointStyles)
            {
                customPointStyle.SetDrawingLevel();
            }
        }

        internal static void SetDrawingLevel(this PointBaseStyle pointBaseStyle)
        {
            pointBaseStyle.DrawingLevel = DrawingLevel.LabelLevel;
        }

        internal static void SetDrawingLevel(this AreaStyle areaStyle)
        {
            areaStyle.DrawingLevel = DrawingLevel.LabelLevel;
            foreach (var customAreaStyle in areaStyle.CustomAreaStyles)
            {
                customAreaStyle.SetDrawingLevel();
            }
        }

        internal static void SetDrawingLevel(this LegendItem legendItem)
        {
            if (legendItem.BackgroundMask != null)
                legendItem.BackgroundMask.DrawingLevel = DrawingLevel.LabelLevel;
            SetDrawingLevelForStyle(legendItem.ImageStyle);
            if (legendItem.ImageMask != null)
                legendItem.ImageMask.DrawingLevel = DrawingLevel.LabelLevel;
            if (legendItem.TextMask != null)
                legendItem.TextMask.DrawingLevel = DrawingLevel.LabelLevel;
        }

        private static void SetDrawingLevelForStyle(Style style)
        {
            AreaStyle areaStyle = style as AreaStyle;
            LineStyle lineStyle = style as LineStyle;
            PointStyle pointStyle = style as PointStyle;
            CompositeStyle compositeStyle = style as CompositeStyle;
            FilterStyle filterStyle = style as FilterStyle;
            ValueStyle valueStyle = style as ValueStyle;
            ClassBreakStyle classBreakStyle = style as ClassBreakStyle;

            if (areaStyle != null)
            {
                areaStyle.SetDrawingLevel();
            }
            else if (lineStyle != null)
            {
                lineStyle.SetDrawingLevel();
            }
            else if (pointStyle != null)
            {
                pointStyle.SetDrawingLevel();
            }
            else if (filterStyle != null)
            {
                foreach (var item in filterStyle.Styles)
                {
                    SetDrawingLevelForStyle(item);
                }
            }
            else if (classBreakStyle != null)
            {
                foreach (var item in classBreakStyle.ClassBreaks.SelectMany(v => v.CustomStyles))
                {
                    SetDrawingLevelForStyle(item);
                }
            }
            else if (valueStyle != null)
            {
                foreach (var item in valueStyle.ValueItems.SelectMany(v => v.CustomStyles))
                {
                    SetDrawingLevelForStyle(item);
                }
            }
            else if (compositeStyle != null)
            {
                foreach (var subStyle in compositeStyle.Styles)
                {
                    SetDrawingLevelForStyle(subStyle);
                }
            }
        }

        internal static void SetDescriptionLayerBackground(this SimplifyMapPrinterLayer mapPrinterLayer, PointShape centerPoint = null)
        {
            if (mapPrinterLayer.Layers.Count == 0)
            {
                mapPrinterLayer.BackgroundMask.Advanced.FillCustomBrush = new GeoLinearGradientBrush(GeoColor.FromHtml("#FFFFFF"), GeoColor.FromHtml("#E6E6E6"), GeoLinearGradientDirection.BottomToTop);
                mapPrinterLayer.BackgroundMask.OutlinePen = new GeoPen(GeoColor.StandardColors.Black);
                mapPrinterLayer.BackgroundMask.DrawingLevel = DrawingLevel.LabelLevel;
            }
        }

        public static void LoadFromViewModel(this ProjectPathPrinterLayer projectPathPrinterLayer, ProjectPathElementViewModel projectPathElementViewModel)
        {
            DrawingFontStyles drawingFontStyles = DrawingFontStyles.Regular;
            if (projectPathElementViewModel.IsBold)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Bold;
            if (projectPathElementViewModel.IsItalic)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Italic;
            if (projectPathElementViewModel.IsStrikeout)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Strikeout;
            if (projectPathElementViewModel.IsUnderline)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Underline;

            GeoFont font = new GeoFont(projectPathElementViewModel.FontName.Source, projectPathElementViewModel.FontSize, drawingFontStyles);
            projectPathPrinterLayer.ProjectPath = projectPathElementViewModel.ProjectPath;
            projectPathPrinterLayer.Font = font;
            projectPathPrinterLayer.TextBrush = projectPathElementViewModel.FontColor;
            projectPathPrinterLayer.DragMode = projectPathElementViewModel.DragMode;
            projectPathPrinterLayer.ResizeMode = projectPathElementViewModel.ResizeMode;
        }

        public static void LoadFromViewModel(this DatePrinterLayer datePrinterLayer, DateElementViewModel dateElementViewModel)
        {
            DrawingFontStyles drawingFontStyles = DrawingFontStyles.Regular;
            if (dateElementViewModel.IsBold)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Bold;
            if (dateElementViewModel.IsItalic)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Italic;
            if (dateElementViewModel.IsStrikeout)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Strikeout;
            if (dateElementViewModel.IsUnderline)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Underline;

            GeoFont font = new GeoFont(dateElementViewModel.FontName.Source, dateElementViewModel.FontSize, drawingFontStyles);
            datePrinterLayer.DateString = dateElementViewModel.SelectedFormat;
            datePrinterLayer.DateFormat = dateElementViewModel.FormatPairs[dateElementViewModel.SelectedFormat];
            datePrinterLayer.Font = font;
            datePrinterLayer.TextBrush = dateElementViewModel.FontColor;
            datePrinterLayer.DragMode = dateElementViewModel.DragMode;
            datePrinterLayer.ResizeMode = dateElementViewModel.ResizeMode;
        }

        public static void LoadFromViewModel(this LabelPrinterLayer labelPrinterLayer, TextElementViewModel textElementViewModel)
        {
            DrawingFontStyles drawingFontStyles = DrawingFontStyles.Regular;
            if (textElementViewModel.IsBold)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Bold;
            if (textElementViewModel.IsItalic)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Italic;
            if (textElementViewModel.IsStrikeout)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Strikeout;
            if (textElementViewModel.IsUnderline)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Underline;

            GeoFont font = new GeoFont(textElementViewModel.FontName.Source, textElementViewModel.FontSize, drawingFontStyles);
            labelPrinterLayer.PrinterWrapMode = textElementViewModel.WrapText ? PrinterWrapMode.WrapText : PrinterWrapMode.AutoSizeText;
            labelPrinterLayer.Text = textElementViewModel.Text;
            labelPrinterLayer.Font = font;
            labelPrinterLayer.TextBrush = textElementViewModel.FontColor;
            labelPrinterLayer.DragMode = textElementViewModel.DragMode;
            labelPrinterLayer.ResizeMode = textElementViewModel.ResizeMode;
        }

        public static void LoadFromViewModel(this ImagePrinterLayer imagePrinterLayer, ImageElementViewModel imageElementEntity)
        {
            //GeoImage image = new GeoImage(imageElementEntity.SelectedImage);
            //imagePrinterLayer.Image = image;
            if (imageElementEntity.BackgroundStyle != null)
            {
                imagePrinterLayer.BackgroundMask = imageElementEntity.BackgroundStyle;
            }
            imagePrinterLayer.ResizeMode = imageElementEntity.ResizeMode;
            imagePrinterLayer.DragMode = imageElementEntity.DragMode;
        }

        public static void LoadFromViewModel(this ScaleLinePrinterLayer scaleLinePrinterLayer, ScaleLineElementViewModel scaleLineElementEntity)
        {
            scaleLinePrinterLayer.MapUnit = scaleLineElementEntity.MapPrinterLayer.MapUnit;
            scaleLinePrinterLayer.DragMode = scaleLineElementEntity.DragMode;
            scaleLinePrinterLayer.BackgroundMask = scaleLineElementEntity.BackgroundStyle;
            scaleLinePrinterLayer.UnitSystem = scaleLineElementEntity.SelectedUnitSystem;
        }

        public static void LoadFromViewModel(this ScaleBarPrinterLayer scaleBarPrinterLayer, ScaleBarElementViewModel scaleBarElementEntity)
        {
            scaleBarPrinterLayer.BackgroundMask = scaleBarElementEntity.Background;
            scaleBarPrinterLayer.BarBrush = scaleBarElementEntity.Color;
            scaleBarPrinterLayer.AlternateBarBrush = scaleBarElementEntity.AlternatingColor;
            scaleBarPrinterLayer.TextStyle.NumericFormat = scaleBarElementEntity.NumericFormatString;
            scaleBarPrinterLayer.UnitFamily = scaleBarElementEntity.SelectedUnitSystem;
            scaleBarPrinterLayer.DragMode = scaleBarElementEntity.DragMode;
            scaleBarPrinterLayer.ResizeMode = scaleBarElementEntity.ResizeMode;
            scaleBarPrinterLayer.MapUnit = scaleBarPrinterLayer.MapPrinterLayer.MapUnit;
        }

        public static void LoadFromViewModel(this DataGridPrinterLayer dataGridPrinterLayer, DataGridViewModel dataGridEntity)
        {
            DrawingFontStyles drawingFontStyles = DrawingFontStyles.Regular;
            if (dataGridEntity.IsBold)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Bold;
            if (dataGridEntity.IsItalic)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Italic;
            if (dataGridEntity.IsStrikeout)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Strikeout;
            if (dataGridEntity.IsUnderline)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Underline;

            GeoFont geoFont = new GeoFont(dataGridEntity.FontName.Source, dataGridEntity.FontSize, drawingFontStyles);

            dataGridPrinterLayer.TextFont = geoFont;

            int originalCount = dataGridEntity.CurrentDataTable.Rows.Count;

            dataGridPrinterLayer.DataTable = dataGridEntity.CurrentDataTable.DefaultView.ToTable();
            int currentCount = dataGridPrinterLayer.DataTable.Rows.Count;
            //Todo:remove the last row which is all empty value;
            if (originalCount != currentCount)
            {
                dataGridPrinterLayer.DataTable.Rows.RemoveAt(currentCount - 1);
            }

            dataGridPrinterLayer.BackgroundMask = new AreaStyle(new GeoPen(GeoColor.StandardColors.Black, 1));
            dataGridPrinterLayer.TextBrush = dataGridEntity.FontColor;
            dataGridPrinterLayer.DragMode = dataGridEntity.DragMode;
            dataGridPrinterLayer.ResizeMode = dataGridEntity.ResizeMode;
        }

        public static void LoadFromLegendItem(this LegendItemViewModel legendItemViewModel, LegendItem legendItem)
        {
            legendItemViewModel.ImageStyle = legendItem.ImageStyle;
            legendItemViewModel.ImageMask = legendItem.ImageMask;

            legendItemViewModel.Text = legendItem.TextStyle.TextColumnName;
            legendItemViewModel.TextSolidBrush = legendItem.TextStyle.TextSolidBrush;
            legendItemViewModel.TextMask = legendItem.TextMask;

            GeoFontViewModel geoFontViewModel = new GeoFontViewModel();
            geoFontViewModel.FromGeoFont(legendItem.TextStyle.Font);
            legendItemViewModel.NotifiedGeoFont = geoFontViewModel;

            legendItemViewModel.BackgroundMask = legendItem.BackgroundMask;

            legendItemViewModel.ImageLeftPadding = legendItem.ImageLeftPadding;
            legendItemViewModel.ImageRightPadding = legendItem.ImageRightPadding;
            legendItemViewModel.ImageTopPadding = legendItem.ImageTopPadding;
            legendItemViewModel.ImageBottomPadding = legendItem.ImageBottomPadding;

            legendItemViewModel.TextLeftPadding = legendItem.TextLeftPadding;
            legendItemViewModel.TextRightPadding = legendItem.TextRightPadding;
            legendItemViewModel.TextTopPadding = legendItem.TextTopPadding;
            legendItemViewModel.TextBottomPadding = legendItem.TextBottomPadding;

            legendItemViewModel.LeftPadding = legendItem.LeftPadding;
            legendItemViewModel.RightPadding = legendItem.RightPadding;
            legendItemViewModel.TopPadding = legendItem.TopPadding;
            legendItemViewModel.BottomPadding = legendItem.BottomPadding;
        }
    }
}