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
using System.Linq;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetSaveStyleMenuItem()
        {
            var command = new ObservedCommand(SaveStyle, () => true);
            return GetMenuItem("Save Style", "/GisEditorPluginCore;component/Images/Export.png", command);
        }

        private static void SaveStyle()
        {
            if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
            var styleItem = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as CompositeStyle;
            if (styleItem != null && GisEditor.LayerListManager.SelectedLayerListItem is StyleLayerListItem)
            {
                int from = 0, to = 0;
                var array = ((StyleLayerListItem)GisEditor.LayerListManager.SelectedLayerListItem).ZoomLevelRange.Split(" to ".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                if (array.Length == 2)
                {
                    int.TryParse(array[0].Replace("(", "").Trim(), out from);
                    int.TryParse(array[1].Replace(")", "").Trim(), out to);
                }
                var count = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Count;
                if (count > from - 1 && count > to - 1)
                {
                    var upperScale = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels[from - 1].Scale;
                    var lowerScale = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels[to - 1].Scale;
                    GisEditor.StyleManager.SaveStyleToLibrary(styleItem, lowerScale, upperScale);
                }
            }
        }
    }
}