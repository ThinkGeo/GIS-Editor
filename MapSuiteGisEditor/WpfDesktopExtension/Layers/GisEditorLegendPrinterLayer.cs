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
using System.Collections.ObjectModel;
using System.Reflection;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class GisEditorLegendPrinterLayer : LegendPrinterLayer
    {
        [Obfuscation]
        private bool isPrinting;

        [NonSerialized]
        private double originalScale;

        [Obfuscation]
        private Dictionary<LegendItem, Style> cachedStyles;

        public GisEditorLegendPrinterLayer()
            : base()
        {
            cachedStyles = new Dictionary<LegendItem, Style>();
        }

        public GisEditorLegendPrinterLayer(LegendAdornmentLayer legendAdornmentLayer)
            : base(legendAdornmentLayer)
        {
            cachedStyles = new Dictionary<LegendItem, Style>();
        }

        public bool IsPrinting
        {
            get { return isPrinting; }
            set { isPrinting = value; }
        }

        protected override void DrawCore(GeoCanvas canvas, Collection<SimpleCandidate> labelsInAllLayers)
        {
            if (originalScale == 0) originalScale = canvas.CurrentScale;

            cachedStyles.Clear();
            if (isPrinting)
            {
                base.DrawCore(canvas, labelsInAllLayers);
            }
            else
            {
                foreach (var legend in LegendItems)
                {
                    cachedStyles[legend] = legend.ImageStyle;
                    var originalCircleStyle = legend.ImageStyle as PointStyle;
                    if (originalCircleStyle != null && originalCircleStyle.SymbolType == PointSymbolType.Circle)
                    {
                        var newCircleStyle = (PointStyle)originalCircleStyle.CloneDeep();
                        newCircleStyle.SymbolSize = (float)(originalScale / canvas.CurrentScale) * originalCircleStyle.SymbolSize;
                        legend.ImageStyle = newCircleStyle;
                    }
                }

                base.DrawCore(canvas, labelsInAllLayers);

                foreach (var legend in LegendItems)
                {
                    if (cachedStyles.ContainsKey(legend))
                    {
                        legend.ImageStyle = cachedStyles[legend];
                    }
                }
            }
            cachedStyles.Clear();
        }
    }
}