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


using System.Reflection;
using System.Windows;
using System.Windows.Media;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    internal static class DataViewerHelper
    {
        internal static Brush GetHightlightLayerColor()
        {
            SolidColorBrush brush = SystemColors.HighlightBrush;

            return brush;
        }

        internal static void HightlightSelectedFeature(Feature feature)
        {
            var selectionOverlay = GisEditor.SelectionManager.GetSelectionOverlay();
            if (selectionOverlay != null && !selectionOverlay.HighlightFeatureLayer.InternalFeatures.Contains(feature.Id))
            {
                selectionOverlay.HighlightFeatureLayer.InternalFeatures.Add(feature.Id, feature);
                selectionOverlay.HighlightFeatureLayer.BuildIndex();
                GisEditor.ActiveMap.Refresh(selectionOverlay);
            }
        }

        internal static void RemoveHightlightFeature(Feature feature)
        {
            var selectionOverlay = GisEditor.SelectionManager.GetSelectionOverlay();
            if (selectionOverlay != null)
            {
                if (selectionOverlay.HighlightFeatureLayer.InternalFeatures.Contains(feature.Id))
                {
                    selectionOverlay.HighlightFeatureLayer.InternalFeatures.Remove(feature.Id);
                    selectionOverlay.HighlightFeatureLayer.BuildIndex();
                    selectionOverlay.Refresh();
                }
            }
        }
    }
}