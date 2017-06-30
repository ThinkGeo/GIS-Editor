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
using System.Text;
using System.Windows.Threading;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class GisEditorLayerOverlay : LayerOverlay
    {
        private static Dictionary<int, int> rules;

        [NonSerialized]
        private DispatcherTimer delayRefreshingTimer;

        static GisEditorLayerOverlay()
        {
            rules = new Dictionary<int, int>();
            rules.Add(128, 5);
            rules.Add(256, 18);
            rules.Add(512, 35);
            rules.Add(1024, 70);
        }

        public GisEditorLayerOverlay()
        {
            delayRefreshingTimer = new DispatcherTimer();
            delayRefreshingTimer.Interval = TimeSpan.FromMilliseconds(50);
            delayRefreshingTimer.Tick += (s, e) =>
            {
                delayRefreshingTimer.Stop();
                base.RefreshCore();
            };

            DrawingExceptionMode = DrawingExceptionMode.DrawException;
            DrawingException += GisEditorLayerOverlay_DrawingException;
            Layers.Removing += Layers_Removing;
            Layers.ClearingItems += Layers_ClearingItems;
            DrawingQuality = DrawingQuality.HighQuality;
        }

        private void GisEditorLayerOverlay_DrawingException(object sender, DrawingExceptionTileOverlayEventArgs e)
        {
            e.Cancel = true;
            e.Canvas.Clear(new GeoSolidBrush(GeoColor.StandardColors.Transparent));
            string text = GetWrappedText(e.Exception.Message, TileWidth);
            if (text.Length > 0)
            {
                DrawingQuality tempDrawingQuality = e.Canvas.DrawingQuality;
                e.Canvas.DrawingQuality = DrawingQuality.HighQuality;
                e.Canvas.DrawTextWithScreenCoordinate(text, new GeoFont("Arial", 10), new GeoSolidBrush(GeoColor.StandardColors.Black), e.Canvas.Width / 2, e.Canvas.Height / 2, DrawingLevel.LabelLevel);
                e.Canvas.DrawingQuality = tempDrawingQuality;
            }
        }

        protected override void RefreshCore()
        {
            //base.RefreshCore();
            delayRefreshingTimer.Stop();
            delayRefreshingTimer.Start();
        }

        protected override RectangleShape GetBoundingBoxCore()
        {
            double left = double.MaxValue;
            double right = double.MinValue;
            double top = double.MinValue;
            double bottom = double.MaxValue;

            Layer[] layersToGet = null;
            lock (Layers)
            {
                layersToGet = Layers.ToArray();
            }

            foreach (Layer layer in layersToGet)
            {
                if (layer.HasBoundingBox && layer.IsVisible)
                {
                    if (!layer.IsOpen) { layer.Open(); }
                    RectangleShape layerBox = layer.GetBoundingBox();
                    left = left < layerBox.LowerLeftPoint.X ? left : layerBox.LowerLeftPoint.X;
                    right = right > layerBox.LowerRightPoint.X ? right : layerBox.LowerRightPoint.X;
                    top = top > layerBox.UpperLeftPoint.Y ? top : layerBox.UpperLeftPoint.Y;
                    bottom = bottom < layerBox.LowerRightPoint.Y ? bottom : layerBox.LowerRightPoint.Y;
                }
            }

            if (left > right || top < bottom)
            {
                return null;
            }
            else
            {
                return new RectangleShape(left, top, right, bottom);
            }
        }

        private string GetWrappedText(string message, int size)
        {
            if (rules.ContainsKey(size))
            {
                int lengthToWrap = rules[size];
                if (message.Length <= lengthToWrap)
                {
                    return message;
                }
                else
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    int time = message.Length / lengthToWrap;
                    for (int i = 0; i < time; i++)
                    {
                        stringBuilder.AppendLine(message.Substring(i * lengthToWrap, lengthToWrap));
                    }
                    stringBuilder.AppendLine(message.Substring(time * lengthToWrap, message.Length - time * lengthToWrap));
                    return stringBuilder.ToString();
                }
            }
            else return message;
        }

        private void Layers_ClearingItems(object sender, ClearingItemsGeoCollectionEventArgs e)
        {
            Close();
        }

        private void Layers_Removing(object sender, RemovingGeoCollectionEventArgs e)
        {
            Close();
        }
    }
}