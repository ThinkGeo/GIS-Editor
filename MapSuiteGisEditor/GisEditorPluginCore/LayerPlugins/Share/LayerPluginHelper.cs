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
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class LayerPluginHelper
    {
        public static bool IsDecimalDegree(Layer layer)
        {
            RectangleShape boundingBox = null;
            if (layer.HasBoundingBox)
            {
                lock (layer)
                {
                    layer.SafeProcess(() =>
                    {
                        boundingBox = layer.GetBoundingBox();
                    });
                }

                return boundingBox.LowerLeftPoint.X >= -185
                    && boundingBox.LowerLeftPoint.Y >= -95
                    && boundingBox.UpperRightPoint.X <= 185
                    && boundingBox.UpperRightPoint.Y <= 95;
            }
            return false;
        }

        public static string GetFeatureIdColumn(FeatureLayer featureLayer)
        {
            string featureIdColumn = string.Empty;
            if (featureLayer != null)
            {
                LayerPlugin layerPlugin = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).FirstOrDefault();
                if (layerPlugin != null)
                {
                    Uri uri = null;
                    try
                    {
                        uri = layerPlugin.GetUri(featureLayer);
                    }
                    catch
                    { }

                    if (uri != null && GisEditor.LayerManager.FeatureIdColumnNames.ContainsKey(uri.ToString()))
                    {
                        featureIdColumn = GisEditor.LayerManager.FeatureIdColumnNames[uri.ToString()];
                        GisEditor.LayerManager.FeatureIdColumnNames.Remove(uri.ToString());
                        GisEditor.LayerManager.FeatureIdColumnNames[featureLayer.FeatureSource.Id] = featureIdColumn;
                    }
                    else if (GisEditor.LayerManager.FeatureIdColumnNames.ContainsKey(featureLayer.FeatureSource.Id))
                    {
                        featureIdColumn = GisEditor.LayerManager.FeatureIdColumnNames[featureLayer.FeatureSource.Id];
                    }
                    else if (GisEditor.LayerManager.FeatureIdColumnNames.Count == 0)
                    {
                        if (string.IsNullOrEmpty(featureIdColumn)
                           && featureLayer.FeatureSource.IsOpen)
                        {
                            var apnColumn = featureLayer.FeatureSource.GetColumns().FirstOrDefault(c => { return c.ColumnName == "APN"; });
                            if (apnColumn != null) featureIdColumn = "APN";
                        }
                    }
                }
            }

            return featureIdColumn;
        }

        public static string GetLayerUriToSave(Uri layerUri, string filter)
        {
            if (layerUri == null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = filter };
                if (saveFileDialog.ShowDialog().GetValueOrDefault())
                {
                    return saveFileDialog.FileName;
                }
                return string.Empty;
            }
            return layerUri.OriginalString;
        }

        public static string GetProj4ProjectionParameter(string prjPathFileName)
        {
            string proj4Parameter = string.Empty;
            try
            {
                if (!File.Exists(prjPathFileName))
                {
                    var projectionWindow = new ProjectionWindow();
                    if (projectionWindow.ShowDialog().GetValueOrDefault())
                    {
                        proj4Parameter = projectionWindow.Proj4ProjectionParameters;
                        File.WriteAllText(prjPathFileName, Proj4Projection.ConvertProj4ToPrj(projectionWindow.Proj4ProjectionParameters));
                    }
                }
                else
                {
                    string wkt = File.ReadAllText(prjPathFileName);
                    proj4Parameter = Proj4Projection.ConvertPrjToProj4(wkt);
                }
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }

            return proj4Parameter;
        }

        /// <summary>
        /// This method sets projection for raster layers.
        /// Just pass in the parameters it requires, then you don't need to do anything about raster layer's projection in your layer provider.
        /// </summary>
        /// <param name="infos">infos</param>
        internal static void SetInternalProjectionForRasterLayers(IEnumerable<RasterLayerInfo> infos)
        {
            string proj4StringForAll = string.Empty;
            bool savePrjFileForAll = false;

            foreach (var info in infos)
            {
                string currentProj4 = string.Empty;
                info.Layer.Open();
                if (info.Layer.HasProjectionText)
                {
                    currentProj4 = info.Layer.GetProjectionText();
                }

                if (!string.IsNullOrEmpty(proj4StringForAll))
                {
                    info.Layer.InitializeProj4Projection(proj4StringForAll);
                    if (savePrjFileForAll)
                    {
                        File.WriteAllText(info.PrjFilePath, Proj4Projection.ConvertProj4ToPrj(proj4StringForAll));
                    }
                }
                else if (!string.IsNullOrEmpty(currentProj4))
                {
                    string proj4 = currentProj4;
                    if (proj4.Trim().Equals(Proj4Projection.GetEpsgParametersString(4326).Trim()))
                    {
                        if (info.Layer.HasBoundingBox)
                        {
                            if (!info.Layer.IsOpen) info.Layer.Open();
                            if (info.Layer.GetBoundingBox().Width > 361)
                            {
                                var result = System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("LayerPluginHelperLayerNotWGText"), GisEditor.LanguageManager.GetStringResource("LayerPluginHelperProjectionNotMatchCaption"), System.Windows.Forms.MessageBoxButtons.YesNo);
                                if (result == System.Windows.Forms.DialogResult.Yes)
                                {
                                    proj4 = Proj4Projection.GetGoogleMapParametersString();
                                }
                            }
                        }
                    }
                    info.Layer.InitializeProj4Projection(proj4);
                }
                else if (IsDecimalDegree(info.Layer))
                {
                    info.Layer.InitializeProj4Projection(Proj4Projection.GetEpsgParametersString(4326));
                }
                else
                {
                    string description = GisEditor.LanguageManager.GetStringResource("selectAProjectionForAllLayersDescription");
                    ProjectionWindow projectionWindow = new ProjectionWindow("", description, "Apply For All Layers");
                    if (projectionWindow.ShowDialog().GetValueOrDefault())
                    {
                        info.Layer.InitializeProj4Projection(projectionWindow.Proj4ProjectionParameters);
                        savePrjFileForAll = true;
                        File.WriteAllText(info.PrjFilePath, Proj4Projection.ConvertProj4ToPrj(projectionWindow.Proj4ProjectionParameters));
                        if (projectionWindow.SyncProj4ProjectionForAll)
                        {
                            proj4StringForAll = projectionWindow.Proj4ProjectionParameters;
                        }
                    }
                    else
                    {
                        info.Layer.InitializeProj4Projection(string.Empty);
                    }
                }
            }
        }

        internal static void SaveFeatureIDColumns(FeatureLayer featureLayer, UserControl metadataUserControl)
        {
            metadataUserControl.Loaded += MetadataUserControl_Loaded;

            var shpLayerViewModel = metadataUserControl.DataContext as ShapefileFeatureLayerPropertiesUserControlViewModel;
            var featureLayerViewModel = metadataUserControl.DataContext as FeatureLayerPropertiesUserControlViewModel;

            string featureIdColumn = GetFeatureIdColumn(featureLayer);
            if (shpLayerViewModel != null && !string.IsNullOrEmpty(featureIdColumn))
            {
                shpLayerViewModel.FeatureIDColumn = featureIdColumn;
            }
            else if (featureLayerViewModel != null && !string.IsNullOrEmpty(featureIdColumn))
            {
                featureLayerViewModel.FeatureIDColumn = featureIdColumn;
            }
        }

        private static void MetadataUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UserControl propertiesWindow = sender as UserControl;
            Window window = null;
            if (propertiesWindow != null && (window = propertiesWindow.Parent as Window) != null)
            {
                propertiesWindow.Loaded -= MetadataUserControl_Loaded;
                window.Closing -= window_Closing;
                window.Closing += window_Closing;
            }
        }

        private static void window_Closing(object sender, CancelEventArgs e)
        {
            Window window = (Window)sender;
            ShapefileFeatureLayerPropertiesUserControlViewModel shpLayerViewModel = window.Content.GetDataContext<ShapefileFeatureLayerPropertiesUserControlViewModel>();
            FeatureLayerPropertiesUserControlViewModel featureLayerViewModel = window.Content.GetDataContext<FeatureLayerPropertiesUserControlViewModel>();

            if (window.DialogResult.GetValueOrDefault())
            {
                if (shpLayerViewModel != null)
                {
                    GisEditor.LayerManager.FeatureIdColumnNames[shpLayerViewModel.TargetFeatureLayer.FeatureSource.Id] = shpLayerViewModel.FeatureIDColumn;
                }
                else if (featureLayerViewModel != null)
                {
                    GisEditor.LayerManager.FeatureIdColumnNames[featureLayerViewModel.TargetFeatureLayer.FeatureSource.Id] = featureLayerViewModel.FeatureIDColumn;
                }
            }
        }
    }
}