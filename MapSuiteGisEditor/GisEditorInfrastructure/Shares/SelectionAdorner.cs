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
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    internal class SelectionAdorner : Adorner
    {
        private static readonly Typeface TypeFace = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectionAdorner" /> class.
        /// </summary>
        /// <param name="uiElement">The UI element.</param>
        public SelectionAdorner(UIElement uiElement)
            : base(uiElement)
        {
            IsHitTestVisible = false;
        }

        /// <summary>
        /// When overridden in a derived class, participates in rendering operations that are directed by the layout system. The rendering instructions for this element are not used directly when this method is invoked, and are instead preserved for later asynchronous use by layout and drawing.
        /// </summary>
        /// <param name="drawingContext">The drawing instructions for a specific element. This context is provided to the layout system.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            var fe = AdornedElement as FrameworkElement;
            if (fe == null)
            {
                return;
            }

            var rect = new Rect(1, 1, Math.Max(0, fe.ActualWidth - 2), Math.Max(0, fe.ActualHeight - 2));
            var color = Colors.Red;
            var brush = new SolidColorBrush(color);
            var pen = new Pen(brush, 1);
            pen.Freeze();

            var dashPen = new Pen(brush, 1) { DashStyle = new DashStyle(new double[] { 1, 6 }, 0) };
            dashPen.Freeze();

            var guidelineSet = new GuidelineSet();
            guidelineSet.GuidelinesX.Add(0.5);
            guidelineSet.GuidelinesY.Add(0.5);

            //var outlinePen = new Pen(new SolidColorBrush(Color.FromArgb(0x70, 0xFF, 0xFF, 0xFF)), 5);
            //outlinePen.Freeze();

            drawingContext.PushGuidelineSet(guidelineSet);

            //drawingContext.DrawRectangle(null, outlinePen, rect);
            drawingContext.DrawRectangle(null, pen, rect);

            var formattedHeight = new FormattedText(string.Format("{0:0}", fe.ActualHeight), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, TypeFace, 10, brush);
            var formattedWidth = new FormattedText(string.Format("{0:0}", fe.ActualWidth), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, TypeFace, 10, brush);
            //drawingContext.DrawText(formattedHeight, new Point(rect.Width + 5, (rect.Height / 2) - (formattedHeight.Height / 2)));
            //drawingContext.DrawText(formattedWidth, new Point(rect.Width / 2 - formattedWidth.Width / 2, rect.Height + 5));

            drawingContext.Pop();
        }
    }
}