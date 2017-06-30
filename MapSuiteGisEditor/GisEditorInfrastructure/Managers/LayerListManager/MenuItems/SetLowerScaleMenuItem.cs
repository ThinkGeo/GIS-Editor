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


using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor
{
    //public class SetLowerScaleMenuItemViewModel : LayerListMenuItem
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetSetLowerScaleMenuItem()
        {
            var command = new ObservedCommand(SetLowerScale, () => true);
            return GetMenuItem("Set Lower ZoomLevel Limit", "/GisEditorInfrastructure;component/Images/Set Lower Scale.png", command);
        }

        private static void SetLowerScale()
        {
            if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
            ZoomLevel selectedZoomLevel = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as ZoomLevel;
            if (selectedZoomLevel != null)
            {
                FeatureLayer featureLayer = GisEditor.LayerListManager.SelectedLayerListItem.Parent.ConcreteObject as FeatureLayer;
                if (featureLayer != null)
                {
                    int currentZoomLevelIndex = GisEditor.ActiveMap.GetSnappedZoomLevelIndex(GisEditor.ActiveMap.CurrentScale);
                    ZoomLevelHelper.ResetZoomLevelRange(selectedZoomLevel, featureLayer, currentZoomLevelIndex + 1, 20);
                }
            }
        }
    }
}