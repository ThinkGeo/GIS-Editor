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


using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor
{
    //public class ZoomToExtentMenuItemViewModel : LayerListMenuItem
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetZoomToExtentMenuItem()
        {
            var command = new ObservedCommand(ZoomToExtent, () => true);
            return GetMenuItem(GisEditor.LanguageManager.GetStringResource("MapElementsListPluginZoomToExtent"), "/GisEditorInfrastructure;component/Images/zoomextent.png", command);
        }

        private static void ZoomToExtent()
        {
            RectangleShape resultExtent = null;
            if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
            if (GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is Overlay && GisEditor.LayerListManager.SelectedLayerListItems.Count > 0)
            {
                Collection<RectangleShape> extents = new Collection<RectangleShape>();
                foreach (var item in GisEditor.LayerListManager.SelectedLayerListItems)
                {
                    var tmpOverlay = item.ConcreteObject as Overlay;
                    if (tmpOverlay != null)
                    {
                        extents.Add(tmpOverlay.GetBoundingBox());
                    }
                }
                resultExtent = ExtentHelper.GetBoundingBoxOfItems(extents);
            }
            else if (GisEditor.LayerListManager.SelectedLayerListItems.Count > 0)
            {
                Collection<RectangleShape> extents = new Collection<RectangleShape>();
                foreach (var item in GisEditor.LayerListManager.SelectedLayerListItems)
                {
                    Layer tmpLayer = item.ConcreteObject as Layer;
                    if (tmpLayer != null && tmpLayer.HasBoundingBox)
                    {
                        tmpLayer.SafeProcess(() =>
                        {
                            extents.Add(tmpLayer.GetBoundingBox());
                        });

                        //tmpLayer.Open();
                        //extents.Add(tmpLayer.GetBoundingBox());
                        //tmpLayer.Close();
                    }
                }
                resultExtent = ExtentHelper.GetBoundingBoxOfItems(extents);
            }
            else if (GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is Overlay)
            {
                resultExtent = (GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as Overlay).GetBoundingBox();
            }
            else if (GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is Layer)
            {
                Layer layer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as Layer;
                if (layer.HasBoundingBox)
                {
                    layer.SafeProcess(() =>
                    {
                        resultExtent = layer.GetBoundingBox();
                    });
                }
            }
            else if (GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is ValueItem)
            {
                string value = ((ValueItem)GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject).Value;
                string columnName = ((ValueStyle)GisEditor.LayerListManager.SelectedLayerListItem.Parent.ConcreteObject).ColumnName;
                FeatureLayer featureLayer = LayerListHelper.FindMapElementInTree<FeatureLayer>(GisEditor.LayerListManager.SelectedLayerListItem);
                if (featureLayer != null)
                {
                    System.Windows.Forms.DialogResult dialogResult = System.Windows.Forms.DialogResult.Yes;
                    FeatureLayerPlugin[] layerPlugins = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).OfType<FeatureLayerPlugin>().ToArray();
                    if (layerPlugins.Length > 0 && !layerPlugins[0].CanQueryFeaturesEfficiently)
                    {
                        dialogResult = System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ZoomToExtentWarning"), GisEditor.LanguageManager.GetStringResource("MapElementsListPluginZoomToExtent"), System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Information);
                    }
                    if (dialogResult == System.Windows.Forms.DialogResult.Yes)
                    {
                        Collection<Feature> features = new Collection<Feature>();
                        featureLayer.SafeProcess(() =>
                        {
                            features = featureLayer.QueryTools.GetFeaturesByColumnValue(columnName, value);
                            resultExtent = ExtentHelper.GetBoundingBoxOfItems(features);
                        });
                        if (features.Count == 0)
                        {
                            MessageBoxHelper.ShowMessage("No features matched.", "Zoom to extent", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                        }
                    }
                }
            }

            if (resultExtent != null)
            {
                GisEditor.ActiveMap.CurrentExtent = resultExtent;
                GisEditor.ActiveMap.Refresh();
            }
        }
    }
}