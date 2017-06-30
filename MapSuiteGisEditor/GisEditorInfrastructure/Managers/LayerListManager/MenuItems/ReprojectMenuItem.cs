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


using System.IO;
using System.Linq;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    //public class ReprojectMenuItemViewModel : LayerListMenuItem
    internal partial class LayerListMenuItemHelper
    {
        private static readonly int bytesInMB = 1024 * 1024;
        private static readonly int shpSize = 30;

        public static MenuItem GetReprojectMenuItem()
        {
            var command = new ObservedCommand(Reproject, () => !(GisEditor.LayerListManager.SelectedLayerListItems.Count > 0));
            return GetMenuItem("Layer Projection", "/GisEditorInfrastructure;component/Images/reprojection.png", command);
        }

        private static void Reproject()
        {
            var result = System.Windows.Forms.MessageBox.Show("Warning! This will modify the internal projection information of your layer. This should only be done if the wrong projection information was selected when initially loading the layer.\r\n\r\nTo change the map's display projection, please use the Map Projection button on the Home tab of the ribbon bar.", "Warning", System.Windows.Forms.MessageBoxButtons.OKCancel, System.Windows.Forms.MessageBoxIcon.Warning);
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
                FeatureLayer layer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as FeatureLayer;

                if (layer != null)
                {
                    bool overSized = IsLayerOverSized(layer);
                    if (overSized)
                    {
                        string reprojectShapeFileNotification = "Applying a projection for this layer may affect performance. Please use the \"Reprojection Wizard\" in the \"Tools\" tab to reproject the ShapeFile to gain better performance.";
                        System.Windows.Forms.MessageBox.Show(reprojectShapeFileNotification, "Info", System.Windows.Forms.MessageBoxButtons.OK);
                    }

                    string initProj4String = GetActiveLayersProj4ProjectionParameter();
                    var description = GisEditor.LanguageManager.GetStringResource("SelectAProjectionForSingleLayerDescription");

                    ProjectionWindow proj4Window = new ProjectionWindow(initProj4String, description, "");
                    if (proj4Window.ShowDialog().GetValueOrDefault())
                    {
                        if (!string.IsNullOrEmpty(proj4Window.Proj4ProjectionParameters))
                        {
                            var projection = new Proj4Projection(proj4Window.Proj4ProjectionParameters, GisEditor.ActiveMap.DisplayProjectionParameters);
                            projection.SyncProjectionParametersString();
                            projection.Open();
                            layer.FeatureSource.Projection = projection;
                            ClearCache(layer);
                            GisEditor.ActiveMap.Refresh();
                        }
                    }
                }
            }
        }

        private static string GetActiveLayersProj4ProjectionParameter()
        {
            string initProj4String = "";
            if (GisEditor.ActiveMap != null && GisEditor.ActiveMap.ActiveLayer != null && GisEditor.ActiveMap.ActiveLayer is FeatureLayer)
            {
                var featureLayer = (FeatureLayer)GisEditor.ActiveMap.ActiveLayer;
                var projection = featureLayer.FeatureSource.Projection;
                var managedProj4Projection = projection as Proj4Projection;
                var proj4Projection = projection as Proj4Projection;
                if (managedProj4Projection != null)
                {
                    initProj4String = managedProj4Projection.InternalProjectionParametersString;
                }
                else if (proj4Projection != null)
                {
                    initProj4String = proj4Projection.InternalProjectionParametersString;
                }
            }
            return initProj4String;
        }

        private static bool IsLayerOverSized(FeatureLayer layer)
        {
            if (layer is ShapeFileFeatureLayer)
            {
                ShapeFileFeatureLayer shpLayer = (ShapeFileFeatureLayer)layer;
                string shpPath = shpLayer.ShapePathFilename;

                if (File.Exists(shpPath))
                {
                    long sizeInBytes = new FileInfo(shpPath).Length;
                    long sizeInMB = sizeInBytes / bytesInMB;

                    return sizeInMB >= shpSize;
                }
            }

            return false;
        }

        private static void ClearCache(FeatureLayer layer)
        {
            TileOverlay cachedOverlay = (from overlay in GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>()
                                         where overlay.Layers.Contains(layer)
                                         select (LayerOverlay)overlay).FirstOrDefault();

            if (cachedOverlay != null && cachedOverlay.TileCache != null)
            {
                cachedOverlay.RefreshCache(RefreshCacheMode.ApplyNewCache);
            }
        }
    }
}