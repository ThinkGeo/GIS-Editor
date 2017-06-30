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
using System.Collections.Generic;
using System.Linq;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    internal class OutlineDrawingVisualGeoCanvas : DrawingVisualGeoCanvas
    {
        private OutlineDrawMode outlineDrawMode;

        internal OutlineDrawMode OutlineDrawMode
        {
            get { return outlineDrawMode; }
            set { outlineDrawMode = value; }
        }

        protected override void DrawLineCore(IEnumerable<ScreenPointF> screenPoints, GeoPen linePen, DrawingLevel drawingLevel, float xOffset, float yOffset)
        {
            switch (outlineDrawMode)
            {
                case OutlineDrawMode.Open:
                    var tmpScreenPoints = screenPoints.ToArray();
                    base.DrawLineCore(tmpScreenPoints.Take(tmpScreenPoints.Length - 1), linePen, drawingLevel, xOffset, yOffset);
                    break;
                default:
                    base.DrawLineCore(screenPoints, linePen, drawingLevel, xOffset, yOffset);
                    break;
            }
        }

        protected override void DrawAreaCore(IEnumerable<ScreenPointF[]> screenPoints, GeoPen outlinePen, GeoBrush fillBrush, DrawingLevel drawingLevel, float xOffset, float yOffset, PenBrushDrawingOrder penBrushDrawingOrder)
        {
            var outline = screenPoints.FirstOrDefault();
            if (outline != null)
            {
                switch (outlineDrawMode)
                {
                    case OutlineDrawMode.Open:
                        DrawLineCore(outline.Take(outline.Length - 1), outlinePen, drawingLevel, xOffset, yOffset);
                        break;
                    case OutlineDrawMode.Dynamic:
                        DrawLineCore(outline.Skip(outline.Length - 3).Take(3), outlinePen, drawingLevel, xOffset, yOffset);
                        break;
                    case OutlineDrawMode.Sealed:
                        DrawLineCore(outline, outlinePen, drawingLevel, xOffset, yOffset);
                        break;
                    case OutlineDrawMode.LineWithFill:
                    default:
                        base.DrawAreaCore(screenPoints, outlinePen, fillBrush, drawingLevel, xOffset, yOffset, penBrushDrawingOrder);
                        break;
                }
            }
        }
    }
}