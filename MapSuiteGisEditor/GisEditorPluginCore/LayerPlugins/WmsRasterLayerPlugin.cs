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
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class WmsRasterLayerPlugin : RasterLayerPlugin
    {
        public WmsRasterLayerPlugin()
        {
            Name = GisEditor.LanguageManager.GetStringResource("WmsRasterLayerPluginName");
            Description = GisEditor.LanguageManager.GetStringResource("WmsRasterLayerPluginDescription");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_fileicon_raster.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dataformats_wms.png", UriKind.RelativeOrAbsolute));
            IsActive = true;
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(WmsRasterLayer);
        }

        protected override Collection<Layer> GetLayersCore(GetLayersParameters getLayersParameters)
        {
            Collection<Layer> layers = base.GetLayersCore(getLayersParameters);
            WmsRasterLayerConfigWindow wmsWindow = new WmsRasterLayerConfigWindow();
            wmsWindow.ViewModel.AddToDataRepositoryVisibility = Visibility.Visible;
            if (wmsWindow.ShowDialog().GetValueOrDefault())
            {
                WmsRasterLayer wmsRasterlayer = wmsWindow.ViewModel.WmsRasterLayer;
                wmsRasterlayer.InitializeProj4Projection(GisEditor.ActiveMap.DisplayProjectionParameters);
                if (wmsRasterlayer != null && wmsRasterlayer.ActiveLayerNames.Count > 0)
                {
                    WmsRasterLayer layer = wmsRasterlayer;
                    layers.Add(layer);
                }
                if (wmsWindow.ViewModel.DoesAddToDataRepository)
                {
                    var wmsDataPlugin = GisEditor.DataRepositoryManager.GetPlugins().OfType<WmsDataRepositoryPlugin>().FirstOrDefault();
                    if (wmsDataPlugin != null)
                    {
                        wmsDataPlugin.RootDataRepositoryItem.Children.Add(new WmsDataRepositoryItem(
                            wmsWindow.ViewModel.Name,
                            new ObservableCollection<string>(wmsWindow.ViewModel.AvailableLayers.Select(l => l.Name)),
                            wmsWindow.ViewModel.WmsServerUrl,
                            wmsWindow.ViewModel.UserName,
                            wmsWindow.ViewModel.Password,
                            wmsWindow.ViewModel.Parameters,
                            wmsWindow.ViewModel.Formats,
                            wmsWindow.ViewModel.Styles,
                            wmsWindow.ViewModel.SelectedFormat,
                            wmsWindow.ViewModel.SelectedStyle));
                    }
                }
            }

            return layers;
        }

        protected override RasterLayer GetRasterLayer(Uri uri)
        {
            WmsRasterLayer layer = null;
            WmsRasterLayerConfigWindow wmsWindow = new WmsRasterLayerConfigWindow();
            wmsWindow.ViewModel.AddToDataRepositoryVisibility = Visibility.Visible;
            if (wmsWindow.ShowDialog().GetValueOrDefault())
            {
                WmsRasterLayer wmsRasterlayer = wmsWindow.ViewModel.WmsRasterLayer;
                wmsRasterlayer.InitializeProj4Projection(GisEditor.ActiveMap.DisplayProjectionParameters);
                if (wmsRasterlayer != null && wmsRasterlayer.ActiveLayerNames.Count > 0)
                {
                    layer = wmsRasterlayer;
                }
                if (wmsWindow.ViewModel.DoesAddToDataRepository)
                {
                    var wmsDataPlugin = GisEditor.DataRepositoryManager.GetPlugins().OfType<WmsDataRepositoryPlugin>().FirstOrDefault();
                    if (wmsDataPlugin != null)
                    {
                        wmsDataPlugin.RootDataRepositoryItem.Children.Add(new WmsDataRepositoryItem(
                            wmsWindow.ViewModel.Name,
                            new ObservableCollection<string>(wmsWindow.ViewModel.AvailableLayers.Select(l => l.Name)),
                            wmsWindow.ViewModel.WmsServerUrl,
                            wmsWindow.ViewModel.UserName,
                            wmsWindow.ViewModel.Password,
                            wmsWindow.ViewModel.Parameters,
                            wmsWindow.ViewModel.Formats,
                            wmsWindow.ViewModel.Styles,
                            wmsWindow.ViewModel.SelectedFormat,
                            wmsWindow.ViewModel.SelectedStyle));
                    }
                }
            }
            return layer;
        }

        //protected override Collection<Layer> GetLayersCore(GetLayersParameters getLayersParameters)
        //{
        //    Collection<Layer> resultLayers = base.GetLayersCore(getLayersParameters);
        //    WmsRasterLayerConfigWindow wmsWindow = new WmsRasterLayerConfigWindow();
        //    wmsWindow.ViewModel.AddToDataRepositoryVisibility = Visibility.Visible;
        //    if (wmsWindow.ShowDialog().GetValueOrDefault())
        //    {
        //        WmsRasterLayer wmsRasterlayer = wmsWindow.ViewModel.WmsRasterLayer;
        //        wmsRasterlayer.InitializeProj4Projection(GisEditor.ActiveMap.DisplayProjectionParameters);
        //        if (wmsRasterlayer != null && wmsRasterlayer.ActiveLayerNames.Count > 0)
        //        {
        //            resultLayers.Add(wmsRasterlayer);
        //        }
        //        if (wmsWindow.ViewModel.DoesAddToDataRepository)
        //        {
        //            var wmsDataPlugin = GisEditor.DataRepositoryManager.GetPlugins().OfType<WmsDataRepositoryPlugin>().FirstOrDefault();
        //            if (wmsDataPlugin != null)
        //            {
        //                wmsDataPlugin.RootDataRepositoryItem.Children.Add(new WmsDataRepositoryItem(
        //                    wmsWindow.ViewModel.Name,
        //                    new ObservableCollection<string>(wmsWindow.ViewModel.AvailableLayers.Select(l => l.Name)),
        //                    wmsWindow.ViewModel.WmsServerUrl,
        //                    wmsWindow.ViewModel.UserName,
        //                    wmsWindow.ViewModel.Password,
        //                    wmsWindow.ViewModel.Parameters,
        //                    wmsWindow.ViewModel.Formats,
        //                    wmsWindow.ViewModel.Styles,
        //                    wmsWindow.ViewModel.SelectedFormat,
        //                    wmsWindow.ViewModel.SelectedStyle));
        //            }
        //        }
        //    }
        //    return resultLayers;
        //}
    }
}