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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Microsoft.Windows.Controls.Ribbon;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Serialize;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class SelectionUIPlugin : UIPlugin
    {
        internal static string FeatureIdColumnName = "FeatureId";

        [NonSerialized]
        private SelectionAndQueryingRibbonGroup selectionAndQueryingGroup;

        private string displayFormat = "| {0}: {1} ";
        private string selectedFormat = "| {0} {1} ";

        private DispatcherTimer setAreaTextTimer;

        [NonSerialized]
        private RibbonGroup helpRibbonGroup;

        private RibbonEntry selectionAndQueryingEntry;
        private RibbonEntry helpEntry;

        [NonSerialized]
        private TextBlock displayTextBlock;
        [NonSerialized]
        private TextBlock selectedTextBlock;

        public SelectionUIPlugin()
        {
            Index = UIPluginOrder.SelectionPlugin;
            selectionAndQueryingGroup = new SelectionAndQueryingRibbonGroup();
            selectionAndQueryingEntry = new RibbonEntry(selectionAndQueryingGroup, RibbonTabOrder.SelectFeatures, "SelectFeaturesTabHeader");

            helpRibbonGroup = new RibbonGroup();
            helpRibbonGroup.Items.Add(HelpResourceHelper.GetHelpButton("SelectFeaturesPluginHelp", HelpButtonMode.RibbonButton));
            helpRibbonGroup.GroupSizeDefinitions.Add(new RibbonGroupSizeDefinition() { IsCollapsed = false });
            helpRibbonGroup.SetResourceReference(RibbonGroup.HeaderProperty, "HelpButtonContent");
            helpEntry = new RibbonEntry(helpRibbonGroup, RibbonTabOrder.SelectFeatures, "SelectFeaturesTabHeader");
            displayTextBlock = new TextBlock();
            selectedTextBlock = new TextBlock();
        }

        protected override void AttachMapCore(GisEditorWpfMap wpfMap)
        {
            base.AttachMapCore(wpfMap);
            wpfMap.SelectionOverlay.FeatureSelected -= new EventHandler<EventArgs>(SelectionOverlay_FeatureSelected);
            wpfMap.SelectionOverlay.FeatureSelected += new EventHandler<EventArgs>(SelectionOverlay_FeatureSelected);
            wpfMap.SelectionOverlay.HighlightFeatureLayer.InternalFeatures.CollectionChanged -= new NotifyCollectionChangedEventHandler(InternalFeatures_CollectionChanged);
            wpfMap.SelectionOverlay.HighlightFeatureLayer.InternalFeatures.CollectionChanged += new NotifyCollectionChangedEventHandler(InternalFeatures_CollectionChanged);

            var viewMode = selectionAndQueryingGroup.DataContext as SelectionAndQueryingRibbonGroupViewModel;
            if (viewMode == null || viewMode.SelectionCompositeStyle == null) return;

            viewMode.SelectionOverlay.HighlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = viewMode.SelectionCompositeStyle.Styles.OfType<AreaStyle>().FirstOrDefault();
            viewMode.SelectionOverlay.HighlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle = viewMode.SelectionCompositeStyle.Styles.OfType<LineStyle>().FirstOrDefault();
            viewMode.SelectionOverlay.HighlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = viewMode.SelectionCompositeStyle.Styles.OfType<PointStyle>().FirstOrDefault();
        }

        protected override void DetachMapCore(GisEditorWpfMap wpfMap)
        {
            base.DetachMapCore(wpfMap);
            if (wpfMap.SelectionOverlay.TrackMode != TrackMode.None)
            {
                wpfMap.SelectionOverlay.TrackMode = TrackMode.None;
                wpfMap.Cursor = System.Windows.Input.Cursors.Arrow;
            }
            wpfMap.SelectionOverlay.HighlightFeatureLayer.InternalFeatures.Clear();
            wpfMap.SelectionOverlay.HighlightFeatureLayer.BuildIndex();
            wpfMap.SelectionOverlay.Refresh();
            wpfMap.SelectionOverlay.FeatureSelected -= new EventHandler<EventArgs>(SelectionOverlay_FeatureSelected);
            wpfMap.SelectionOverlay.HighlightFeatureLayer.InternalFeatures.CollectionChanged -= new NotifyCollectionChangedEventHandler(InternalFeatures_CollectionChanged);
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);

            var viewMode = selectionAndQueryingGroup.DataContext as SelectionAndQueryingRibbonGroupViewModel;
            if (viewMode != null)
            {
                GeoSerializer serializer = new GeoSerializer();
                if (settings.GlobalSettings.ContainsKey("SelectionCompositeStyle"))
                {
                    try
                    {
                        viewMode.SelectionCompositeStyle = (CompositeStyle)serializer.Deserialize(settings.GlobalSettings["SelectionCompositeStyle"]);
                    }
                    catch (Exception ex)
                    {
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    }
                }
            }
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            var viewMode = selectionAndQueryingGroup.DataContext as SelectionAndQueryingRibbonGroupViewModel;
            if (viewMode != null)
            {
                try
                {
                    GeoSerializer serializer = new GeoSerializer();
                    settings.GlobalSettings["SelectionCompositeStyle"] = serializer.Serialize(viewMode.SelectionCompositeStyle);
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
            }
            return settings;
        }

        protected override void LoadCore()
        {
            base.LoadCore();

            if (!RibbonEntries.Contains(selectionAndQueryingEntry)) RibbonEntries.Add(selectionAndQueryingEntry);
            if (!RibbonEntries.Contains(helpEntry)) RibbonEntries.Add(helpEntry);
            if (!StatusBarItems.Contains(displayTextBlock)) StatusBarItems.Add(displayTextBlock);
            if (!StatusBarItems.Contains(selectedTextBlock)) StatusBarItems.Add(selectedTextBlock);
        }

        protected override void UnloadCore()
        {
            base.UnloadCore();

            if (StatusBarItems.Contains(displayTextBlock)) StatusBarItems.Remove(displayTextBlock);
            if (StatusBarItems.Contains(selectedTextBlock)) StatusBarItems.Remove(selectedTextBlock);
        }

        protected override void RefreshCore(GisEditorWpfMap currentMap, RefreshArgs refreshArgs)
        {
            var allFeatureLayers = GisEditor.ActiveMap.GetFeatureLayers(true);
            var featureLayersToRemove = GisEditor.ActiveMap.SelectionOverlay.FilteredLayers.Where(l => !allFeatureLayers.Contains(l)).ToList();
            foreach (var item in featureLayersToRemove)
            {
                GisEditor.ActiveMap.SelectionOverlay.FilteredLayers.Remove(item);
            }

            selectionAndQueryingGroup.Synchronize(currentMap, refreshArgs);
            CommandHelper.CloseFindFeaturesWindow();
        }

        protected override Collection<MenuItem> GetLayerListItemContextMenuItemsCore(GetLayerListItemContextMenuParameters parameters)
        {
            Collection<MenuItem> menuItems = base.GetLayerListItemContextMenuItemsCore(parameters);

            if (parameters.LayerListItem.ConcreteObject is FeatureLayer || parameters.LayerListItem.ConcreteObject is LayerOverlay)
            {
                MenuItem selectLayerItem = new MenuItem();
                selectLayerItem.Header = "Select Layer";
                selectLayerItem.Icon = new Image { Width = 16, Height = 16, Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/SelectTool.png", UriKind.RelativeOrAbsolute)) };
                selectLayerItem.Command = new ObservedCommand(SelectLayer, () => true);

                menuItems.Add(selectLayerItem);
            }

            return menuItems;
        }

        private static void SelectLayer()
        {
            if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
            Collection<FeatureLayer> featureLayers = new Collection<FeatureLayer>();
            FeatureLayer featureLayer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as FeatureLayer;
            LayerOverlay layerOverlay = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as LayerOverlay;
            if (featureLayer != null)
            {
                featureLayers.Add(featureLayer);
            }
            else if (layerOverlay != null)
            {
                foreach (var tempFeatureLayer in layerOverlay.Layers.OfType<FeatureLayer>())
                {
                    featureLayers.Add(tempFeatureLayer);
                }
            }

            if (featureLayers.Count > 0)
            {
                var selectionOverlay = GisEditor.SelectionManager.GetSelectionOverlay();
                if (selectionOverlay != null)
                {
                    selectionOverlay.FilteredLayers.Clear();
                    foreach (var tempFeatureLayer in featureLayers)
                    {
                        selectionOverlay.FilteredLayers.Add(tempFeatureLayer);
                    }

                    if (Application.Current != null && Application.Current.Dispatcher != null)
                    {
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            GisEditor.UIManager.RefreshPlugins();
                        });
                    }
                }
            }
        }

        protected override Collection<MenuItem> GetMapContextMenuItemsCore(GetMapContextMenuParameters parameters)
        {
            Collection<MenuItem> menuItems = new Collection<MenuItem>();
            if (GisEditor.SelectionManager.GetSelectedFeatures().Count > 0 && menuItems.Count(m => m.Header != null && m.Header.ToString() == "Selected features") == 0)
            {
                MenuItem highlightedFeatureMenuItem = HighlightedFeaturesHelper.GetHighlightedFeaturesMenuitem();
                menuItems.Add(highlightedFeatureMenuItem);
            }

            MenuItem exportPictureMenuItem = new MenuItem();
            exportPictureMenuItem.Header = "Export Map to Image";
            exportPictureMenuItem.Icon = new Image() { Width = 16, Height = 16, Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/jpeg2000.png", UriKind.RelativeOrAbsolute)) };

            MenuItem exportToImageMenuItem = new MenuItem();
            exportToImageMenuItem.Header = "Export Map to Image";
            exportToImageMenuItem.Click += exportToImageMenuItem_Click;
            exportPictureMenuItem.Items.Add(exportToImageMenuItem);


            MenuItem exportToClipboardMenuItem = new MenuItem();
            exportToClipboardMenuItem.Header = "Export Map Image to Clipboard";
            exportToClipboardMenuItem.Click += exportToClipboardMenuItem_Click;
            exportPictureMenuItem.Items.Add(exportToClipboardMenuItem);

            menuItems.Add(exportPictureMenuItem);

            return menuItems;
        }

        private void exportToClipboardMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Drawing.Image bitmap = System.Drawing.Bitmap.FromStream(new MemoryStream(BoundingBoxSelectorMapTool.GetCroppedMapPreviewImage(GisEditor.ActiveMap, new System.Windows.Int32Rect(0, 0, (int)GisEditor.ActiveMap.RenderSize.Width, (int)GisEditor.ActiveMap.RenderSize.Height))));
            Clipboard.SetDataObject(bitmap);
        }

        private void exportToImageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JPEG (*.jpg)|*.jpg|PNG (*.png)|*.png";
            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                System.Drawing.Image bitmap = System.Drawing.Bitmap.FromStream(new MemoryStream(BoundingBoxSelectorMapTool.GetCroppedMapPreviewImage(GisEditor.ActiveMap, new System.Windows.Int32Rect(0, 0, (int)GisEditor.ActiveMap.RenderSize.Width, (int)GisEditor.ActiveMap.RenderSize.Height))));
                bitmap.Save(saveFileDialog.FileName);
            }
        }

        private void SelectionOverlay_FeatureSelected(object sender, EventArgs e)
        {
            Dictionary<string, string> columnValues = new Dictionary<string, string>();

            foreach (var feature in GisEditor.SelectionManager.GetSelectedFeatures())
            {
                if (!feature.ColumnValues.ContainsKey(FeatureIdColumnName))
                {
                    string featureId = feature.Id;

                    var selectLayer = GisEditor.SelectionManager.GetSelectedFeaturesLayer(feature);
                    string featureIdColumn = LayerPluginHelper.GetFeatureIdColumn(selectLayer);

                    if (feature.ColumnValues.ContainsKey(featureIdColumn))
                    {
                        featureId = feature.ColumnValues[featureIdColumn];
                    }
                    //else if (feature.LinkColumnValues.ContainsKey(featureIdColumn))
                    //{
                    //    featureId = string.Join(Environment.NewLine, feature.LinkColumnValues[featureIdColumn].Select(f => f.Value));
                    //}

                    if (featureId.Contains(SelectionTrackInteractiveOverlay.FeatureIdSeparator))
                    {
                        featureId = featureId.Split(new string[] { SelectionTrackInteractiveOverlay.FeatureIdSeparator }, StringSplitOptions.RemoveEmptyEntries)[0];
                    }

                    feature.ColumnValues.Add(SelectionUIPlugin.FeatureIdColumnName, featureId);
                }
            }

            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.SelectionOverlayFeatureSelectedDescription));
            GisEditor.LoggerManager.Log(LoggerLevel.Debug, "Feature Selected, Count: " + GisEditor.ActiveMap.SelectionOverlay.HighlightFeatureLayer.InternalFeatures.Count.ToString(), "Selection");
        }

        private void InternalFeatures_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                SetAreaText(0);
                SetSelectedText(0);
                FeatureInfoWindow.Instance.Refresh(new Dictionary<FeatureLayer, Collection<Feature>>());
            }
            else
            {
                if (setAreaTextTimer == null)
                {
                    setAreaTextTimer = new DispatcherTimer();
                    setAreaTextTimer.Interval = TimeSpan.FromMilliseconds(500);
                    setAreaTextTimer.Tick += (s, args) =>
                    {
                        Task.Factory.StartNew(() =>
                        {
                            double area = 0;
                            int count = 0;
                            Proj4Projection managedProj4Projection = GetProjection();

                            try
                            {
                                Collection<AreaBaseShape> areaShapes = new Collection<AreaBaseShape>();
                                count = GisEditor.ActiveMap.SelectionOverlay.HighlightFeatureLayer.InternalFeatures.Count;
                                foreach (var feature in GisEditor.ActiveMap.SelectionOverlay.HighlightFeatureLayer.InternalFeatures)
                                {
                                    AreaBaseShape areaBaseShape = feature.GetShape() as AreaBaseShape;
                                    if (areaBaseShape != null)
                                    {
                                        AreaBaseShape projectedAreaShape = managedProj4Projection.ConvertToExternalProjection(areaBaseShape) as AreaBaseShape;
                                        if (projectedAreaShape != null)
                                        {
                                            areaShapes.Add(projectedAreaShape);
                                        }
                                    }
                                }
                                if (areaShapes.Count > 0)
                                {
                                    var unionShape = AreaBaseShape.Union(areaShapes);
                                    area = unionShape.GetArea(GeographyUnit.DecimalDegree, MeasureTrackInteractiveOverlay.AreaUnit);
                                }
                            }
                            catch (Exception ex)
                            {
                                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                            }

                            Application.Current.Dispatcher.BeginInvoke(() =>
                            {
                                SetAreaText(Math.Round(area, 2));
                                SetSelectedText(count);
                            }, DispatcherPriority.Background);
                        });

                        setAreaTextTimer.Stop();
                    };
                }

                if (setAreaTextTimer.IsEnabled)
                {
                    setAreaTextTimer.Stop();
                }

                setAreaTextTimer.Start();
            }
        }

        private Proj4Projection GetProjection()
        {
            Proj4Projection projection = new Proj4Projection();
            projection.InternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
            projection.ExternalProjectionParametersString = Proj4Projection.GetDecimalDegreesParametersString();
            projection.Open();
            return projection;
        }

        private void SetAreaText(double area)
        {
            if (area == 0)
            {
                displayTextBlock.Text = string.Empty;
            }
            else
            {
                displayTextBlock.Text = string.Format(CultureInfo.InvariantCulture, displayFormat, MeasureTrackInteractiveOverlay.AreaUnit, area);
            }
        }

        private void SetSelectedText(int count)
        {
            if (count == 0)
            {
                selectedTextBlock.Text = string.Empty;
            }
            else
            {
                selectedTextBlock.Text = string.Format(CultureInfo.InvariantCulture, selectedFormat, count, "Selected");
            }
        }
    }
}