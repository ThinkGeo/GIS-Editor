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
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class GridlinesPrinterLayer : PrinterLayer
    {
        private double left;
        private double top;
        private double right;
        private double bottom;
        private PrintingUnit marginUnit;

        private int cellWidth;
        private int cellHeight;
        private PrintingUnit cellUnit;

        private int rows;
        private int columns;
        private GeoPen drawingPen;
        private bool useCellSize;

        public GridlinesPrinterLayer()
        {
        }

        public double Left
        {
            get { return left; }
            set { left = value; }
        }

        public double Top
        {
            get { return top; }
            set { top = value; }
        }

        public double Right
        {
            get { return right; }
            set { right = value; }
        }

        public double Bottom
        {
            get { return bottom; }
            set { bottom = value; }
        }

        public PrintingUnit MarginUnit
        {
            get { return marginUnit; }
            set { marginUnit = value; }
        }

        public bool UseCellSize
        {
            get { return useCellSize; }
            set { useCellSize = value; }
        }

        public int CellWidth
        {
            get { return cellWidth; }
            set { cellWidth = value; }
        }
        public int CellHeight
        {
            get { return cellHeight; }
            set { cellHeight = value; }
        }
        public PrintingUnit CellUnit
        {
            get { return cellUnit; }
            set { cellUnit = value; }
        }

        public int Rows
        {
            get { return rows; }
            set { rows = value; }
        }

        public int Columns
        {
            get { return columns; }
            set { columns = value; }
        }

        public GeoPen DrawingPen
        {
            get { return drawingPen; }
            set { drawingPen = value; }
        }

        protected override void DrawCore(GeoCanvas canvas, Collection<SimpleCandidate> labelsInAllLayers)
        {
            base.DrawCore(canvas, labelsInAllLayers);
            var pageBindingBox = GetBoundingBox();
            double leftInPoint = PrinterHelper.ConvertLength(left, marginUnit, PrintingUnit.Point);
            double topInPoint = PrinterHelper.ConvertLength(top, marginUnit, PrintingUnit.Point);
            double rightInPoint = PrinterHelper.ConvertLength(right, marginUnit, PrintingUnit.Point);
            double bottomInPoint = PrinterHelper.ConvertLength(bottom, marginUnit, PrintingUnit.Point);

            PointShape upperLeft = new PointShape(pageBindingBox.UpperLeftPoint.X + leftInPoint, pageBindingBox.UpperLeftPoint.Y - topInPoint);
            PointShape lowerRight = new PointShape(pageBindingBox.LowerRightPoint.X - rightInPoint, pageBindingBox.LowerRightPoint.Y + bottomInPoint);
            RectangleShape newBindingBox = new RectangleShape(upperLeft, lowerRight);

            var columnsCount = columns;
            var rowsCount = rows;

            double widthIncrease = newBindingBox.Width / columnsCount;
            double heightIncrease = newBindingBox.Height / rowsCount;

            if (useCellSize)
            {
                double width = PrinterHelper.ConvertLength(cellWidth, cellUnit, PrintingUnit.Point);
                double height = PrinterHelper.ConvertLength(cellHeight, cellUnit, PrintingUnit.Point);

                if (newBindingBox.Width < width
                    || newBindingBox.Height < height)
                {
                    return;
                }

                columnsCount = (int)(newBindingBox.Width / width);
                rowsCount = (int)(newBindingBox.Height / height);

                widthIncrease = width;
                heightIncrease = height;
            }

            double horizontalLineX1 = newBindingBox.UpperLeftPoint.X;
            double horizontalLineX2 = newBindingBox.UpperRightPoint.X;
            for (int i = 0; i <= rowsCount; i++)
            {
                LineShape horizontalLine = new LineShape();
                var horizontalLineFirstPoint = new Vertex(horizontalLineX1, newBindingBox.UpperLeftPoint.Y - (i) * heightIncrease);
                var horizontalLineSecondPoint = new Vertex(horizontalLineX2, newBindingBox.UpperRightPoint.Y - (i) * heightIncrease);
                horizontalLine.Vertices.Add(horizontalLineFirstPoint);
                horizontalLine.Vertices.Add(horizontalLineSecondPoint);
                canvas.DrawLine(horizontalLine, drawingPen, DrawingLevel.LabelLevel);
            }

            LineShape lastHorizontalLine = new LineShape();
            var lastHorizontalLineFirstPoint = new Vertex(horizontalLineX1, newBindingBox.LowerLeftPoint.Y);
            var lastHorizontalLineSecondPoint = new Vertex(horizontalLineX2, newBindingBox.LowerRightPoint.Y);
            lastHorizontalLine.Vertices.Add(lastHorizontalLineFirstPoint);
            lastHorizontalLine.Vertices.Add(lastHorizontalLineSecondPoint);
            canvas.DrawLine(lastHorizontalLine, drawingPen, DrawingLevel.LabelLevel);

            double verticalLineY1 = newBindingBox.UpperLeftPoint.Y;
            double verticalLineY2 = newBindingBox.LowerRightPoint.Y;
            for (int i = 0; i <= columnsCount; i++)
            {
                LineShape verticalLine = new LineShape();
                var verticalLineX = newBindingBox.UpperLeftPoint.X + (i) * widthIncrease;
                var verticalLineFirstPoint = new Vertex(verticalLineX, verticalLineY1);
                var verticalLineSecondPoint = new Vertex(verticalLineX, verticalLineY2);
                verticalLine.Vertices.Add(verticalLineFirstPoint);
                verticalLine.Vertices.Add(verticalLineSecondPoint);
                canvas.DrawLine(verticalLine, drawingPen, DrawingLevel.LabelLevel);
            }

            LineShape lastVerticalLine = new LineShape();
            var lastVerticalLineFirstPoint = new Vertex(newBindingBox.UpperRightPoint.X, verticalLineY1);
            var lastVerticalLineLineSecondPoint = new Vertex(newBindingBox.UpperRightPoint.X, verticalLineY2);
            lastVerticalLine.Vertices.Add(lastVerticalLineFirstPoint);
            lastVerticalLine.Vertices.Add(lastVerticalLineLineSecondPoint);
            canvas.DrawLine(lastVerticalLine, drawingPen, DrawingLevel.LabelLevel);
        }
    }
}
