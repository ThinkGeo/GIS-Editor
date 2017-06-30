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
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class GridlinesSettingsViewModel : ViewModelBase
    {
        private bool showGridlines;
        private int rows;
        private int columns;
        private LineDashStyle selectedDashStyle;
        private float penWidth;
        private Collection<LineDashStyle> dashStyles;
        private GeoSolidBrush selectedBrush;

        private double left;
        private double top;
        private double right;
        private double bottom;
        private Collection<PrintingUnit> units;
        private PrintingUnit selectedUnit;

        private PrintingUnit selectedCellUnit;
        private int cellWidth;
        private int cellHeight;
        private bool useCellSize;

        public GridlinesSettingsViewModel(PrinterLayer printerLayer)
        {
            units = new Collection<PrintingUnit>();
            units.Add(PrintingUnit.Inch);
            units.Add(PrintingUnit.Centimeter);
            dashStyles = new Collection<LineDashStyle>();
            foreach (LineDashStyle item in Enum.GetValues(typeof(LineDashStyle)))
            {
                dashStyles.Add(item);
            }
            showGridlines = true;
            useCellSize = true;
            GridlinesPrinterLayer gridlinesPrinterLayer = printerLayer as GridlinesPrinterLayer;
            if (gridlinesPrinterLayer == null)
            {
                selectedBrush = new GeoSolidBrush(GeoColor.StandardColors.Gray);
                selectedDashStyle = LineDashStyle.Dot;
                rows = 10;
                columns = 10;
                penWidth = 1;
                selectedUnit = PrintingUnit.Inch;
                left = 1;
                top = 1;
                right = 1;
                bottom = 1;

                cellHeight = 1;
                cellWidth = 1;
                selectedCellUnit = PrintingUnit.Inch;
            }
            else
            {
                selectedBrush = gridlinesPrinterLayer.DrawingPen.Brush as GeoSolidBrush;
                selectedDashStyle = gridlinesPrinterLayer.DrawingPen.DashStyle;
                rows = gridlinesPrinterLayer.Rows;
                columns = gridlinesPrinterLayer.Columns;
                penWidth = gridlinesPrinterLayer.DrawingPen.Width;
                selectedUnit = gridlinesPrinterLayer.MarginUnit;
                left = gridlinesPrinterLayer.Left;
                top = gridlinesPrinterLayer.Top;
                right = gridlinesPrinterLayer.Right;
                bottom = gridlinesPrinterLayer.Bottom;

                cellHeight = gridlinesPrinterLayer.CellHeight;
                cellWidth = gridlinesPrinterLayer.CellWidth;
                selectedCellUnit = gridlinesPrinterLayer.CellUnit;
                useCellSize = gridlinesPrinterLayer.UseCellSize;
            }
        }

        public bool ShowGridlines
        {
            get { return showGridlines; }
            set { showGridlines = value; }
        }

        public bool UseCellSize
        {
            get { return useCellSize; }
            set { useCellSize = value; }
        }

        public int Rows
        {
            get { return rows; }
            set
            {
                rows = value;
                RaisePropertyChanged(()=>Rows);
            }
        }

        public int Columns
        {
            get { return columns; }
            set
            {
                columns = value;
                RaisePropertyChanged(()=>Columns);
            }
        }

        public int CellWidth
        {
            get { return cellWidth; }
            set
            {
                cellWidth = value;
                RaisePropertyChanged(()=>CellWidth);
            }
        }

        public int CellHeight
        {
            get { return cellHeight; }
            set
            {
                cellHeight = value;
                RaisePropertyChanged(()=>CellHeight);
            }
        }

        public float PenWidth
        {
            get { return penWidth; }
            set
            {
                penWidth = value;
                RaisePropertyChanged(()=>PenWidth);
            }
        }

        public LineDashStyle SelectedDashStyle
        {
            get { return selectedDashStyle; }
            set
            {
                selectedDashStyle = value;
                RaisePropertyChanged(()=>SelectedDashStyle);
            }
        }

        public Collection<LineDashStyle> DashStyles
        {
            get { return dashStyles; }
        }

        public GeoSolidBrush SelectedBrush
        {
            get { return selectedBrush; }
            set
            {
                selectedBrush = value;
                RaisePropertyChanged(()=>SelectedBrush);
            }
        }

        public double Left
        {
            get { return left; }
            set
            {
                left = value;
                RaisePropertyChanged(()=>Left);
            }
        }

        public double Top
        {
            get { return top; }
            set
            {
                top = value;
                RaisePropertyChanged(()=>Top);
            }
        }

        public double Right
        {
            get { return right; }
            set
            {
                right = value;
                RaisePropertyChanged(()=>Right);
            }
        }

        public double Bottom
        {
            get { return bottom; }
            set
            {
                bottom = value;
                RaisePropertyChanged(()=>Bottom);
            }
        }

        public Collection<PrintingUnit> Units
        {
            get { return units; }
        }


        public PrintingUnit SelectedCellUnit
        {
            get { return selectedCellUnit; }
            set
            {
                selectedCellUnit = value;
                RaisePropertyChanged(()=>SelectedCellUnit);
            }
        }

        public PrintingUnit SelectedUnit
        {
            get { return selectedUnit; }
            set
            {
                selectedUnit = value;
                RaisePropertyChanged(()=>SelectedUnit);
            }
        }

        public GridlinesPrinterLayer ToGridlinesPrinterLayer()
        {
            GridlinesPrinterLayer gridlinesPrinterLayer = new GridlinesPrinterLayer();
            gridlinesPrinterLayer.Columns = columns;
            gridlinesPrinterLayer.Rows = rows;
            gridlinesPrinterLayer.DrawingPen = new GeoPen(SelectedBrush, penWidth);
            gridlinesPrinterLayer.DrawingPen.DashStyle = selectedDashStyle;
            gridlinesPrinterLayer.MarginUnit = selectedUnit;
            gridlinesPrinterLayer.Left = Left;
            gridlinesPrinterLayer.Top = Top;
            gridlinesPrinterLayer.Right = Right;
            gridlinesPrinterLayer.Bottom = Bottom;
            gridlinesPrinterLayer.CellHeight = CellHeight;
            gridlinesPrinterLayer.CellWidth = CellWidth;
            gridlinesPrinterLayer.CellUnit = SelectedCellUnit;
            gridlinesPrinterLayer.UseCellSize = UseCellSize;

            return gridlinesPrinterLayer;
        }
    }
}
