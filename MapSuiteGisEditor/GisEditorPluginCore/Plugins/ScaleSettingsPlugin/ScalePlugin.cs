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
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class ScalePlugin : UIPlugin
    {
        private const string addedScaleKey = "AddedScale";
        private const string addedScaleIndexKey = "AddedScaleIndex";

        private RibbonEntry ribbonEntry;
        private ScaleSettingsRibbonGroupViewModel viewModel;

        public ScalePlugin()
        {
            viewModel = new ScaleSettingsRibbonGroupViewModel();
            ScaleSettingsRibbonGroup scaleSettingsRibbonGroup = new ScaleSettingsRibbonGroup();
            scaleSettingsRibbonGroup.DataContext = viewModel;
            ribbonEntry = new RibbonEntry(scaleSettingsRibbonGroup, 1, "HomeRibbonTabHeader");
            Index = UIPluginOrder.ScalePlugin;
        }

        protected override void AttachMapCore(GisEditorWpfMap wpfMap)
        {
            base.AttachMapCore(wpfMap);
            wpfMap.CurrentScaleChanged -= WpfMap_CurrentScaleChanged;
            wpfMap.CurrentScaleChanged += WpfMap_CurrentScaleChanged;
        }

        protected override void DetachMapCore(GisEditorWpfMap wpfMap)
        {
            base.DetachMapCore(wpfMap);
            wpfMap.CurrentScaleChanged -= WpfMap_CurrentScaleChanged;
        }

        protected override Collection<MenuItem> GetMapContextMenuItemsCore(GetMapContextMenuParameters parameters)
        {
            Collection<MenuItem> menuitems = base.GetMapContextMenuItemsCore(parameters);

            MenuItem zoomToPreciseScaleItem = new MenuItem();
            zoomToPreciseScaleItem.Header = GisEditor.LanguageManager.GetStringResource("ScalePluginZoomToScaleText");
            zoomToPreciseScaleItem.Icon = new Image { Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/zoomto.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
            zoomToPreciseScaleItem.Click += ZoomToPreciseScaleItem_Click;
            zoomToPreciseScaleItem.Tag = parameters.WorldCoordinates;
            menuitems.Add(zoomToPreciseScaleItem);

            return menuitems;
        }

        private void ZoomToPreciseScaleItem_Click(object sender, RoutedEventArgs e)
        {
            AddZoomLevelWindow window = new AddZoomLevelWindow();
            window.Title = GisEditor.LanguageManager.GetStringResource("ScalePluginZoomToScaleText");
            window.DataContext = viewModel;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Owner = Application.Current.MainWindow;
            if (window.ShowDialog().GetValueOrDefault())
            {
                var menuItem = sender as MenuItem;
                if (menuItem != null)
                {
                    var centerPoint = menuItem.Tag as PointShape;
                    ScaleSettingsRibbonGroupViewModel.SetNewScale(window.Scale, centerPoint);
                }
                else
                {
                    ScaleSettingsRibbonGroupViewModel.SetNewScale(window.Scale);
                }
            }
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            if (settings.GlobalSettings.ContainsKey(addedScaleKey) && settings.GlobalSettings.ContainsKey(addedScaleIndexKey))
            {
                double value;
                if (double.TryParse(settings.GlobalSettings[addedScaleKey], out value))
                {
                    int index = int.Parse(settings.GlobalSettings[addedScaleIndexKey]);
                    if (GisEditor.ActiveMap != null)
                    {
                        GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Insert(index, new PreciseZoomLevel(value));
                    }
                }
            }
        }

        protected override StorableSettings GetSettingsCore()
        {
            StorableSettings settings = base.GetSettingsCore();
            if (GisEditor.ActiveMap != null)
            {
                Collection<ZoomLevel> zoomLevels = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels;
                ZoomLevel zoomLevel = zoomLevels.FirstOrDefault(c => c is PreciseZoomLevel);
                if (zoomLevel != null)
                {
                    settings.GlobalSettings[addedScaleKey] = zoomLevel.Scale.ToString(CultureInfo.InvariantCulture);
                    settings.GlobalSettings[addedScaleIndexKey] = zoomLevels.IndexOf(zoomLevel).ToString(CultureInfo.InvariantCulture);
                }
            }
            return settings;
        }

        private void WpfMap_CurrentScaleChanged(object sender, CurrentScaleChangedWpfMapEventArgs e)
        {
            double currentScale = e.CurrentScale * Conversion.ConvertMeasureUnits(1, DistanceUnit.Inch, viewModel.SelectedDistanceUnit);
            viewModel.UpdateValues();
            viewModel.SelectedScale = viewModel.Scales.FirstOrDefault(s => Math.Abs(s.Scale - currentScale) < 1);
        }
    }
}