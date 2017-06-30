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
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using ThinkGeo.MapSuite.WpfDesktop.Extension;
using System.Collections.ObjectModel;
using System.Linq;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ViewUIPlugin : UIPlugin
    {
        [NonSerialized]
        private ViewRibbonGroup viewGroup;
        private RibbonEntry viewEntry;
        private GisEditorWpfMap previousMap;
        private Dictionary<string, List<double>> customZoomLevelSets;

        public ViewUIPlugin()
        {
            customZoomLevelSets = new Dictionary<string, List<double>>();
            Index = UIPluginOrder.MapElementsPlugin;
            Description = GisEditor.LanguageManager.GetStringResource("ViewUIPluginDescription");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/View.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/View.png", UriKind.RelativeOrAbsolute));

            viewGroup = new ViewRibbonGroup();
            viewEntry = new RibbonEntry();
            viewEntry.RibbonGroup = viewGroup;
            viewEntry.RibbonTabIndex = RibbonTabOrder.Home;
        }

        public Dictionary<string, List<double>> CustomZoomLevelSets
        {
            get { return customZoomLevelSets; }
        }

        protected override void LoadCore()
        {
            base.LoadCore();
            if (!RibbonEntries.Contains(viewEntry))
            {
                RibbonEntries.Add(viewEntry);
            }
        }

        protected override void RefreshCore(GisEditorWpfMap currentMap, RefreshArgs refreshArgs)
        {
            base.RefreshCore(currentMap, refreshArgs);
            if (currentMap != null)
            {
                if (previousMap != currentMap)
                {
                    viewGroup.viewModel.SelectedBackground = currentMap.BackgroundOverlay.BackgroundBrush;
                    previousMap = currentMap;
                }
            }
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            XElement customZoomLevelSetsXElement = new XElement("CustomZoomLevelSets");
            foreach (var item in CustomZoomLevelSets)
            {
                XElement zoomLevelSetXElement = new XElement("ZoomLevelSet", new XAttribute("name", item.Key));
                foreach (var scale in item.Value)
                {
                    zoomLevelSetXElement.Add(new XElement("Scale", scale));
                }
                customZoomLevelSetsXElement.Add(zoomLevelSetXElement);
            }
            settings.GlobalSettings["CustomZoomLevelSets"] = customZoomLevelSetsXElement.ToString();
            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            CustomZoomLevelSets.Clear();
            if (settings.GlobalSettings.ContainsKey("CustomZoomLevelSets"))
            {
                try
                {
                    var xmlText = settings.GlobalSettings["CustomZoomLevelSets"];
                    var customZoomLevelSetsXElement = XElement.Parse(xmlText);
                    foreach (var item in customZoomLevelSetsXElement.Descendants("ZoomLevelSet"))
                    {
                        if (item.HasAttributes && item.FirstAttribute.Value != null)
                        {
                            var scales = new List<double>();
                            foreach (var scaleXElement in item.Descendants())
                            {
                                double scale;
                                if (double.TryParse(scaleXElement.Value, out scale))
                                {
                                    scales.Add(scale);
                                }
                            }
                            CustomZoomLevelSets.Add(item.FirstAttribute.Value, scales);
                        }
                    }
                    FillDefaultZoomLevels();
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
            }
        }

        private void FillDefaultZoomLevels()
        {
            string landProName = "LandProZoomLevelSet";
            if (!CustomZoomLevelSets.ContainsKey(landProName))
            {
                List<double> scales = new List<double>();

                GoogleMapsZoomLevelSet zoomLevelSet = new GoogleMapsZoomLevelSet();
                foreach (var item in zoomLevelSet.GetZoomLevels())
                {
                    item.Scale = Math.Round(item.Scale, 6);
                    zoomLevelSet.CustomZoomLevels.Add(item);
                }
                for (int i = 0; i < 5; i++)
                {
                    var scale = zoomLevelSet.CustomZoomLevels.LastOrDefault().Scale * 0.5;
                    var zoomLevel = new ZoomLevel(Math.Round(scale, 6));
                    zoomLevelSet.CustomZoomLevels.Add(zoomLevel);
                }

                Collection<ZoomLevel> zoomLevels = zoomLevelSet.CustomZoomLevels;
                for (int i = 0; i < zoomLevels.Count; i++)
                {
                    double scale1 = zoomLevels[i].Scale;
                    double scale2 = zoomLevels[i].Scale;
                    if (zoomLevels.Count > i + 1)
                    {
                        scale2 = zoomLevels[i + 1].Scale;
                    }
                    else
                    {
                        scale2 = scale2 - scale2 / 4;
                    }

                    scale2 = (scale2 + scale1) / 2;
                    scales.Add(scale1);
                    scales.Add(Math.Round(scale2, 6));
                }
                CustomZoomLevelSets.Add(landProName, scales);
            }
            string googleName = "GoogleZoomLevelSet";
            if (!CustomZoomLevelSets.ContainsKey(googleName))
            {
                GoogleMapsZoomLevelSet zoomLevelSet = new GoogleMapsZoomLevelSet();
                CustomZoomLevelSets.Add(googleName, zoomLevelSet.GetZoomLevels().Select(z => z.Scale).ToList());
            }
            string bingName = "BingZoomLevelSet";
            if (!CustomZoomLevelSets.ContainsKey(bingName))
            {
                BingMapsZoomLevelSet zoomLevelSet = new BingMapsZoomLevelSet();
                CustomZoomLevelSets.Add(bingName, zoomLevelSet.GetZoomLevels().Select(z => z.Scale).ToList());
            }
        }
    }
}