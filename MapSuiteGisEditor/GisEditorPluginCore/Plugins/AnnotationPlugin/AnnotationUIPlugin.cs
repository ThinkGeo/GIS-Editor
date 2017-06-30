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


using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class AnnotationUIPlugin : UIPlugin
    {
        private static AnnotationViewModel viewModel;

        private Feature feature;
        private RibbonEntry drawAndEditEntry;
        private AnnotationRibbonGroup drawAndEditGroup;

        static AnnotationUIPlugin()
        {
            viewModel = new AnnotationViewModel();
            MarkerHelper.ViewModel = viewModel;
            AnnotationHelper.ViewModel = viewModel;
        }

        public AnnotationUIPlugin()
        {
            IsActive = false;
            Description = GisEditor.LanguageManager.GetStringResource("AnnotationUIPluginDescription");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/PluginIcon.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/PluginIcon.png", UriKind.RelativeOrAbsolute));
            Index = UIPluginOrder.AnnotationPlugin;

            drawAndEditGroup = new AnnotationRibbonGroup();
            drawAndEditGroup.DataContext = viewModel;
            drawAndEditEntry = new RibbonEntry();
            drawAndEditEntry.RibbonGroup = drawAndEditGroup;
            drawAndEditEntry.RibbonTabName = "AnnotationPluginGroupTabName";
            drawAndEditEntry.RibbonTabIndex = RibbonTabOrder.Annotation;
        }

        protected override Collection<MenuItem> GetMapContextMenuItemsCore(GetMapContextMenuParameters parameters)
        {
            Collection<MenuItem> menuItems = base.GetMapContextMenuItemsCore(parameters);

            RectangleShape rectangle = parameters.GetClickWorldArea(10);
            InMemoryFeatureLayer layer = viewModel.CurrentAnnotationOverlay.TrackShapeLayer;
            layer.Open();
            feature = layer.QueryTools.GetFeaturesIntersecting(rectangle, ReturningColumnsType.AllColumns).FirstOrDefault();

            if (feature != null && feature.GetShape() is PointShape)
            {
                MenuItem addFileLinkItem = new MenuItem();
                addFileLinkItem.Header = "Set file link";
                addFileLinkItem.Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/linkFile.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
                addFileLinkItem.Click += new System.Windows.RoutedEventHandler(AddFileLinkItem_Click);

                menuItems.Add(addFileLinkItem);

                if (feature.ColumnValues.ContainsKey("LinkFileName") && !string.IsNullOrEmpty(feature.ColumnValues["LinkFileName"]))
                {
                    MenuItem removeFileLinkItem = new MenuItem();
                    removeFileLinkItem.Header = "Remove file link";
                    removeFileLinkItem.Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/Delete.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
                    removeFileLinkItem.Click += new System.Windows.RoutedEventHandler(RemoveFileLinkItem_Click);

                    menuItems.Add(removeFileLinkItem);

                    MenuItem openFileLinkItem = new MenuItem();
                    openFileLinkItem.Header = "Open file link";
                    openFileLinkItem.Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/Open.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
                    openFileLinkItem.Click += new System.Windows.RoutedEventHandler(OpenFileLinkItem_Click);

                    menuItems.Add(openFileLinkItem);
                }
            }

            return menuItems;
        }

        private void OpenFileLinkItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            InMemoryFeatureLayer layer = viewModel.CurrentAnnotationOverlay.TrackShapeLayer;
            Feature tempFeature = layer.InternalFeatures.FirstOrDefault(f => f.Id.Equals(feature.Id));
            string columnValue = tempFeature.ColumnValues["LinkFileName"];
            string path = columnValue;
            if (columnValue.Contains("||"))
            {
                int index = columnValue.IndexOf("||");
                path = columnValue.Substring(index + 2, columnValue.Length - index - 2);
            }
            try
            {
                Process.Start(path);
            }
            catch
            { }
        }

        private void RemoveFileLinkItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            InMemoryFeatureLayer layer = viewModel.CurrentAnnotationOverlay.TrackShapeLayer;
            Feature tempFeature = layer.InternalFeatures.FirstOrDefault(f => f.Id.Equals(feature.Id));
            if (tempFeature != null)
            {
                layer.InternalFeatures.Remove(tempFeature);
                var editFeature = viewModel.CurrentEditOverlay.EditShapesLayer.InternalFeatures.FirstOrDefault(f => f.Id == tempFeature.Id);
                if (editFeature != null)
                {
                    viewModel.CurrentEditOverlay.EditShapesLayer.InternalFeatures.Remove(editFeature);
                }
            }
            GisEditor.ActiveMap.Refresh();
        }

        private void AddFileLinkItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                string fileName = openFileDialog.FileName;
                InMemoryFeatureLayer layer = viewModel.CurrentAnnotationOverlay.TrackShapeLayer;
                Feature tempFeature = layer.InternalFeatures.FirstOrDefault(f => f.Id.Equals(feature.Id));
                viewModel.CurrentAnnotationOverlay.SetLinkFileName(tempFeature, fileName);
                GisEditor.ActiveMap.Refresh();
            }
        }

        protected override void LoadCore()
        {
            base.LoadCore();
            if (!RibbonEntries.Contains(drawAndEditEntry))
            {
                RibbonEntries.Add(drawAndEditEntry);
            }
        }

        protected override void UnloadCore()
        {
            base.UnloadCore();
            RibbonEntries.Clear();
        }

        protected override void AttachMapCore(GisEditorWpfMap wpfMap)
        {
            base.AttachMapCore(wpfMap);

            viewModel.CurrentAnnotationOverlay.MapMouseClick -= MarkerHelper.ActiveMap_MapClick;
            viewModel.CurrentAnnotationOverlay.MapMouseClick += MarkerHelper.ActiveMap_MapClick;
            viewModel.CurrentAnnotationOverlay.MapMouseClick -= AnnotationHelper.ActiveMap_MapClick;
            viewModel.CurrentAnnotationOverlay.MapMouseClick += AnnotationHelper.ActiveMap_MapClick;

            var switcher = wpfMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();
            if (switcher != null)
            {
                switcher.SwitcherModeChanged -= SwitcherModeChanged;
                switcher.SwitcherModeChanged += SwitcherModeChanged;
            }
        }

        protected override void DetachMapCore(GisEditorWpfMap wpfMap)
        {
            base.DetachMapCore(wpfMap);

            //viewModel.IsAddingText = false;
            wpfMap.ExtentOverlay.MapMouseClick -= MarkerHelper.ActiveMap_MapClick;
            wpfMap.ExtentOverlay.MapMouseClick -= AnnotationHelper.ActiveMap_MapClick;
            var switcher = wpfMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();
            if (switcher != null)
            {
                switcher.SwitcherModeChanged -= SwitcherModeChanged;
            }
        }

        protected override void RefreshCore(GisEditorWpfMap currentMap, RefreshArgs refreshArgs)
        {
            base.RefreshCore(currentMap, refreshArgs);
            viewModel.SyncUIState();
            viewModel.SyncStylePreview();
            viewModel.Refresh();
        }

        private void SwitcherModeChanged(object sender, SwitcherModeChangedSwitcherPanZoomBarMapToolEventArgs e)
        {
            switch (e.NewSwitcherMode)
            {
                case SwitcherMode.None:
                    break;

                case SwitcherMode.Pan:
                case SwitcherMode.TrackZoom:
                case SwitcherMode.Identify:
                default:
                    viewModel.CurrentAnnotationOverlayTrackMode = TrackMode.None;
                    break;
            }
        }
    }
}