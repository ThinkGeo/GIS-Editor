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
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class LayerListUIPlugin : UIPlugin
    {
        private static LinearGradientBrush defaultBackground;

        [NonSerialized]
        private DockWindow dockWindow;

        [NonSerialized]
        private UserControl layerListUserControl;

        [NonSerialized]
        private DispatcherTimer dispatcherTimer;

        [NonSerialized]
        private Dictionary<string, LayerListItem> allMapElementsEntity;

        [NonSerialized]
        private Dictionary<string, Dictionary<string, Visibility>> states;
        [NonSerialized]
        private Dictionary<string, Dictionary<string, bool>> expandStates;

        public LayerListUIPlugin()
        {
            Description = GisEditor.LanguageManager.GetStringResource("LayerListPluginDescreption");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/tree_view.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/tree_view.png", UriKind.RelativeOrAbsolute));
            allMapElementsEntity = new Dictionary<string, LayerListItem>();
            Index = UIPluginOrder.LayerListPlugin;
            expandStates = new Dictionary<string, Dictionary<string, bool>>();
        }

        public Dictionary<string, Dictionary<string, bool>> ExpandStates
        {
            get { return expandStates; }
        }

        protected override void AttachMapCore(GisEditorWpfMap wpfMap)
        {
            base.AttachMapCore(wpfMap);
            wpfMap.CurrentScaleChanged -= MapCurrentScaleChanged;
            wpfMap.CurrentScaleChanged += MapCurrentScaleChanged;
        }

        protected override void DetachMapCore(GisEditorWpfMap wpfMap)
        {
            base.DetachMapCore(wpfMap);
            wpfMap.CurrentScaleChanged -= MapCurrentScaleChanged;
        }

        protected override void LoadCore()
        {
            base.LoadCore();
            if (layerListUserControl == null)
            {
                layerListUserControl = new LayerListUserControl();
                dockWindow = new DockWindow
                {
                    Name = "LayerList",
                    Title = "LayerListPluginTitle",
                    Position = DockWindowPosition.Left,
                    Content = layerListUserControl
                };
                DockWindows.Add(dockWindow);
            }
            if (!DockWindows.Contains(dockWindow))
            {
                DockWindows.Add(dockWindow);
            }
        }

        protected override Collection<MenuItem> GetLayerListItemContextMenuItemsCore(GetLayerListItemContextMenuParameters parameters)
        {
            Collection<MenuItem> menuItems = base.GetLayerListItemContextMenuItemsCore(parameters);
            object concreteObject = parameters.LayerListItem.ConcreteObject;

            //if (concreteObject is FeatureLayer)
            //{
            //    if (EditorUIPlugin.IsRelateAndAliasEnabled)
            //    {
            //        MenuItem relateMenuItem = GetRelateMenuItem((FeatureLayer)concreteObject);
            //        menuItems.Add(relateMenuItem);
            //    }
            //}

            if (concreteObject is RasterLayer)
            {
                MenuItem keyColorsMenuItem = GetKeyColorsMenuItem((RasterLayer)concreteObject);
                menuItems.Add(keyColorsMenuItem);
            }

            return menuItems;
        }

        public MenuItem GetKeyColorsMenuItem(RasterLayer layer)
        {
            var menuItem = new MenuItem();
            menuItem.Header = "Transparency Colors";
            menuItem.Icon = new Image() { Source = new BitmapImage(new Uri("/GisEditorInfrastructure;component/Images/Transparent.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
            menuItem.Click += (s, e) =>
            {
                if (layer != null)
                {
                    MrSidKeyColorConfigureWindow window = new MrSidKeyColorConfigureWindow(layer.KeyColors);
                    if (window.ShowDialog().GetValueOrDefault())
                    {
                        MrSidKeyColorConfigureViewModel model = window.DataContext as MrSidKeyColorConfigureViewModel;
                        layer.KeyColors.Clear();

                        foreach (var color in model.Colors)
                        {
                            layer.KeyColors.Add(new GeoColor(color.Color.A, color.Color.R, color.Color.G, color.Color.B));
                        }
                        TileOverlay tileOverlay = LayerListHelper.FindMapElementInLayerList<TileOverlay>(GisEditor.LayerListManager.SelectedLayerListItem);
                        if (tileOverlay != null)
                        {
                            tileOverlay.Refresh();
                        }
                    }
                }
            };

            return menuItem;
        }

        //private MenuItem GetRelateMenuItem(FeatureLayer featureLayer)
        //{
        //    MenuItem rootMenuItem = new MenuItem();
        //    rootMenuItem.Header = GisEditor.LanguageManager.GetStringResource("LayerListUIPluginRelateText");
        //    rootMenuItem.Icon = new Image() { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/relation.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
        //    rootMenuItem.Click += new RoutedEventHandler(RelateMenuItem_Click);
        //    rootMenuItem.Tag = featureLayer;

        //    PluginHelper.ApplyReadonlyMode(rootMenuItem);
        //    return rootMenuItem;
        //}

        //private void RelateMenuItem_Click(object sender, RoutedEventArgs e)
        //{
        //    MenuItem menuItem = (MenuItem)sender;
        //    FeatureLayer sourceFeatureLayer = (FeatureLayer)menuItem.Tag;

        //    LinkSourceWindow window = new LinkSourceWindow(sourceFeatureLayer);
        //    window.Owner = Application.Current.MainWindow;
        //    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        //    if (window.ShowDialog().GetValueOrDefault())
        //    {
        //        LinkSourceItem resultItem = window.ResultLinkSource;
        //        sourceFeatureLayer.FeatureSource.LinkSources.Clear();
        //        sourceFeatureLayer.FeatureSource.LinkExpressions.Clear();
        //        ApplyLinkSourceItems(sourceFeatureLayer, resultItem.LinkSourceItems);
        //        sourceFeatureLayer.FeatureSource.RefreshColumns();
        //        GisEditor.ActiveMap.RefreshActiveOverlay();
        //        GisEditor.ActiveMap.Refresh();
        //    }
        //}

        //private void ApplyLinkSourceItems(FeatureLayer featureLayer, Collection<LinkSourceItem> linkSourceItems)
        //{
        //    LinkSource linkSource = new LinkSource();
        //    FillLinkSource(linkSource, linkSourceItems);

        //    foreach (var item in linkSource.LinkSources)
        //    {
        //        featureLayer.FeatureSource.LinkSources.Add(item);
        //    }

        //    foreach (var item in linkSource.LinkExpressions)
        //    {
        //        if (item.Split(new[] { featureLayer.Name }, StringSplitOptions.RemoveEmptyEntries).Length >= 2)
        //        {
        //            string[] results = item.Split('.');
        //            if (results.Length == 3)
        //            {
        //                string expressions = "feature";
        //                foreach (var result in results.Skip(1))
        //                {
        //                    expressions += "." + result;
        //                }
        //                featureLayer.FeatureSource.LinkExpressions.Add(expressions);
        //            }
        //            else
        //            {
        //                string expressions = item.Replace(featureLayer.Name, "feature");
        //                featureLayer.FeatureSource.LinkExpressions.Add(expressions);
        //            }
        //        }
        //        else
        //        {
        //            string expressions = item.Replace(featureLayer.Name, "feature");
        //            featureLayer.FeatureSource.LinkExpressions.Add(expressions);
        //        }
        //    }
        //}

        //private void FillLinkSource(LinkSource linkSource, Collection<LinkSourceItem> linkSourceItems)
        //{
        //    foreach (var item in linkSourceItems)
        //    {
        //        LinkSourcePlugin plugin = LinkSourcePluginManager.Instance.GetLinkSourcePlugin(item.Source.GetType());
        //        LinkSource newlinkSource = plugin.GetLinkSource(item);
        //        linkSource.LinkSources.Add(newlinkSource);
        //        linkSource.LinkExpressions.Add(item.LinkExpression);
        //        if (item.LinkSourceItems.Count > 0)
        //        {
        //            FillLinkSource(newlinkSource, item.LinkSourceItems);
        //        }
        //    }
        //}

        protected override void RefreshCore(GisEditorWpfMap currentMap, RefreshArgs refreshArgs)
        {
            // RefreshInternal(currentMap, refreshArgs);
            if (dispatcherTimer == null)
            {
                dispatcherTimer = new DispatcherTimer();
                dispatcherTimer.Interval = TimeSpan.FromMilliseconds(200);
                dispatcherTimer.Tick += (s, e) =>
                {
                    DispatcherTimer currentTimer = (DispatcherTimer)s;
                    currentTimer.Stop();

                    Tuple<GisEditorWpfMap, RefreshArgs> currentTimerTag = currentTimer.Tag as Tuple<GisEditorWpfMap, RefreshArgs>;
                    RefreshInternal(currentTimerTag.Item1, currentTimerTag.Item2);
                };
            }

            if (dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Stop();
            }

            Tuple<GisEditorWpfMap, RefreshArgs> timerTag = new Tuple<GisEditorWpfMap, RefreshArgs>(currentMap, refreshArgs);
            dispatcherTimer.Tag = timerTag;
            dispatcherTimer.Start();
        }

        private void RefreshInternal(GisEditorWpfMap currentMap, RefreshArgs refreshArgs)
        {
            if (currentMap != null && !GetRefreshArgsToSkip().Any(func => func(refreshArgs)))
            {
                var viewModel = GisEditor.LayerListManager.GetRootLayerListItem(currentMap);
                currentMap.Dispatcher.BeginInvoke(() =>
                {
                    RefreshExpandedStates(currentMap.Name, viewModel);

                    if (states != null && states.ContainsKey(currentMap.Name))
                    {
                        foreach (var overlayEntity in viewModel.Children)
                        {
                            if (states[currentMap.Name].ContainsKey(overlayEntity.Name))
                            {
                                overlayEntity.ChildrenContainerVisibility = states[currentMap.Name][overlayEntity.Name];
                            }
                            foreach (var layerEntity in overlayEntity.Children)
                            {
                                if (states[currentMap.Name].ContainsKey(layerEntity.Name))
                                {
                                    layerEntity.ChildrenContainerVisibility = states[currentMap.Name][layerEntity.Name];
                                }
                            }
                        }
                        if (viewModel.Children.Count > 0) states.Remove(currentMap.Name);
                    }
                    else if (allMapElementsEntity.ContainsKey(currentMap.Name))
                    {
                        //LoadVisibilityFromMapElementEntity(allMapElementsEntity[currentMap.Name], viewModel);
                        UpdateNewEntityVisibility(allMapElementsEntity[currentMap.Name], viewModel);
                        allMapElementsEntity[currentMap.Name] = viewModel;
                    }
                    else if (!string.IsNullOrEmpty(currentMap.Name))
                    {
                        allMapElementsEntity.Add(currentMap.Name, viewModel);
                    }
                    layerListUserControl.DataContext = viewModel;
                }, DispatcherPriority.Background);
            }
        }

        private void RefreshExpandedStates(string mapName, LayerListItem item)
        {
            if (expandStates != null && expandStates.ContainsKey(mapName))
            {
                foreach (var overlayEntity in item.Children)
                {
                    if (expandStates[mapName].ContainsKey(overlayEntity.Name))
                    {
                        overlayEntity.IsExpanded = expandStates[mapName][overlayEntity.Name];
                        overlayEntity.ChildrenContainerVisibility = overlayEntity.IsExpanded ? Visibility.Visible : Visibility.Collapsed;
                    }
                    foreach (var layerEntity in overlayEntity.Children)
                    {
                        if (expandStates[mapName].ContainsKey(layerEntity.Name))
                        {
                            layerEntity.IsExpanded = expandStates[mapName][layerEntity.Name];
                            layerEntity.ChildrenContainerVisibility = layerEntity.IsExpanded ? Visibility.Visible : Visibility.Collapsed;
                        }
                    }
                }
                if (item.Children.Count > 0) states.Remove(mapName);
            }
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            settings.ProjectSettings.Add("Setting", SaveProjectSettingInternal().ToString());
            settings.ProjectSettings.Add("ExpandSettings", SaveProjectExpandSettingInternal().ToString());
            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            if (settings.ProjectSettings.ContainsKey("Setting"))
            {
                try
                {
                    LoadProjectSettingInternal(XElement.Parse(settings.ProjectSettings["Setting"]));
                    LoadProjectExpandSettingInternal(XElement.Parse(settings.ProjectSettings["ExpandSettings"]));
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
            }
        }

        protected override LayerListItem GetLayerListItemCore(object concreteObject)
        {
            if (concreteObject is Overlay)
            {
                var overlay = (Overlay)concreteObject;
                var overlayListItem = new LayerListItem();
                overlayListItem.ConcreteObject = concreteObject;
                overlayListItem.CheckBoxVisibility = Visibility.Visible;
                overlayListItem.ChildrenContainerVisibility = Visibility.Visible;
                overlayListItem.IsChecked = overlay.IsVisible;
                overlayListItem.Name = overlay.Name;
                overlayListItem.HighlightBackgroundBrush = GetDefaultLayerGroupBackground();
                overlayListItem.PropertyChanged += OverlayItemPropertyChanged;
                if (GisEditor.LayerListManager.SelectedLayerListItem != null && overlay == GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject)
                {
                    overlayListItem.HighlightBackgroundBrush = new SolidColorBrush(Colors.LightBlue);
                    GisEditor.LayerListManager.SelectedLayerListItem = overlayListItem;
                }
                MenuItem toTopMenuItem = LayerListMenuItemHelper.GetMovementMenuItem(MovementAction.ToTop);
                MenuItem toBottomMenuItem = LayerListMenuItemHelper.GetMovementMenuItem(MovementAction.ToBottom);
                MenuItem upMenuItem = LayerListMenuItemHelper.GetMovementMenuItem(MovementAction.Up);
                MenuItem downMenuItem = LayerListMenuItemHelper.GetMovementMenuItem(MovementAction.Down);

                overlayListItem.ContextMenuItems.Add(toTopMenuItem);
                overlayListItem.ContextMenuItems.Add(toBottomMenuItem);
                overlayListItem.ContextMenuItems.Add(upMenuItem);
                overlayListItem.ContextMenuItems.Add(downMenuItem);
                overlayListItem.ContextMenuItems.Add(new MenuItem() { Header = "--" });
                overlayListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetZoomToExtentMenuItem());
                overlayListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetRenameMenuItem());
                overlayListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetRemoveOverlayMenuItem());
                overlayListItem.ContextMenuItems.Add(new MenuItem() { Header = "--" });
                if (concreteObject is LayerOverlay)
                {
                    overlayListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetAddLayerMenuItem());
                    MenuItem newLayerItem = LayerListMenuItemHelper.GetNewLayerMenuItem();
                    if (newLayerItem != null)
                    {
                        overlayListItem.ContextMenuItems.Add(newLayerItem);
                    }
                    overlayListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetRefreshLayersMenuItem((LayerOverlay)concreteObject));
                    //overlayListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetCloneLayerCountMenuItem((LayerOverlay)concreteObject));
                }
                if (concreteObject is TileOverlay)
                {
                    overlayListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetTileTypeMenuItem((TileOverlay)concreteObject));
                }

                //overlayListItem.ContextMenuItems.Add(new MenuItem() { Header = "--" });
                //overlayListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetSetExceptionModeMenuItem());

                if (overlayListItem.ConcreteObject is BingMapsOverlay)
                {
                    //e.LayerListItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/BingMaps.png", UriKind.Relative)) };
                    overlayListItem.SideImage = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_base_maps.png", UriKind.Relative)), Width = 16, Height = 16 };
                    overlayListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetBingMapStyleMenuItem());
                    overlayListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetBaseMapsCacheMenuItem());
                    overlayListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetTransparencyMenuItem(((BingMapsOverlay)overlayListItem.ConcreteObject).OverlayCanvas.Opacity));
                }
                else if (overlayListItem.ConcreteObject is OpenStreetMapOverlay)
                {
                    //e.LayerListItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/osm_logo.png", UriKind.Relative)) };
                    overlayListItem.SideImage = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_base_maps.png", UriKind.Relative)), Width = 16, Height = 16 };
                    overlayListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetBaseMapsCacheMenuItem());
                    overlayListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetTransparencyMenuItem(((OpenStreetMapOverlay)overlayListItem.ConcreteObject).OverlayCanvas.Opacity));
                }
                else if (overlayListItem.ConcreteObject is WorldMapKitMapOverlay)
                {
                    overlayListItem.SideImage = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_base_maps.png", UriKind.Relative)), Width = 16, Height = 16 };
                    overlayListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetWorldMapKitStyleMenuItem());
                    overlayListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetBaseMapsCacheMenuItem());

                    //e.LayerListItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/WMKOverlay.png", UriKind.Relative)) };
                    overlayListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetTransparencyMenuItem(((WorldMapKitMapOverlay)overlayListItem.ConcreteObject).OverlayCanvas.Opacity));
                }
                else if (overlayListItem.ConcreteObject is DynamicLayerOverlay)
                {
                    overlayListItem.Name = string.IsNullOrEmpty(overlay.Name) ? "Dynamic Layer Group" : overlay.Name;
                    toTopMenuItem.IsEnabled = false;
                    toBottomMenuItem.IsEnabled = false;
                    upMenuItem.IsEnabled = false;
                    downMenuItem.IsEnabled = false;
                    InMemoryFeatureLayer[] featureLayersToDelete = ((DynamicLayerOverlay)overlay).Layers.OfType<InMemoryFeatureLayer>().Where(l => l.InternalFeatures.Count == 0).ToArray();
                    foreach (var item in featureLayersToDelete)
                    {
                        ((DynamicLayerOverlay)overlay).Layers.Remove(item);
                    }

                    //overlayListItem = null;
                }
                else if (overlayListItem.ConcreteObject is MeasureTrackInteractiveOverlay)
                {
                    if (Singleton<MeasureSetting>.Instance.AllowCollectFixedElements)
                    {
                        overlayListItem = LayerListHelper.CreateLayerListItemForMeasureOverlay(overlayListItem);
                    }
                    else overlayListItem = null;
                }
                else if (overlayListItem.ConcreteObject is LayerOverlay)
                {
                    var layerOverlay = (LayerOverlay)overlay;

                    //e.LayerListItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/LayerOverlay.png", UriKind.Relative)) };
                    overlayListItem.Name = string.IsNullOrEmpty(layerOverlay.Name) ? "Layer Group" : layerOverlay.Name;
                    overlayListItem.ChildrenContainerVisibility = Visibility.Visible;
                    overlayListItem.SideImage = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/up.png", UriKind.Relative)) };

                    //Dictionary<Layer, bool> elementVisibleDictionary = new Dictionary<Layer, bool>();
                    //layerOverlay.Layers.ForEach(layer => { elementVisibleDictionary.Add(layer, layer.IsVisible); });
                    overlayListItem.IsChecked = layerOverlay.Layers.Any(layer => layer.IsVisible);

                    //entity.SubEntities.ForEach(mapEntity => { mapEntity.IsVisible = elementVisibleDictionary[mapEntity]; });
                }
                else
                {
                    overlayListItem = null;
                }
                return overlayListItem;
            }
            else if (concreteObject is Layer)
            {
                GdiPlusRasterLayerPlugin gdiPlusRasterLayerPlugin = new GdiPlusRasterLayerPlugin();
                LayerListItem layerListItem = gdiPlusRasterLayerPlugin.GetLayerListItem(concreteObject as Layer);
                if (string.IsNullOrEmpty(layerListItem.Name))
                {
                    layerListItem.Name = concreteObject.GetType().Name;
                }
                layerListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetMovementMenuItem(MovementAction.ToTop));
                layerListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetMovementMenuItem(MovementAction.ToBottom));
                layerListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetMovementMenuItem(MovementAction.Up));
                layerListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetMovementMenuItem(MovementAction.Down));
                layerListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetZoomToExtentMenuItem());
                layerListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetRenameMenuItem());
                layerListItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetRemoveLayerMenuItem());
                return layerListItem;
            }
            else return null;
        }

        private void StartEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FeatureLayer featurelayer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as FeatureLayer;
            var selectionItem = EditingToolsViewModel.Instance.AvailableLayers.FirstOrDefault(t => t.Value == featurelayer);
            if (selectionItem != null) EditingToolsViewModel.Instance.EditingLayerChangedCommand.Execute(selectionItem);
        }

        private void OverlayItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var currentLayerListItem = sender as LayerListItem;
            if (e.PropertyName == "IsChecked")
            {
                if (GisEditor.LayerListManager.SelectedLayerListItems.Contains(currentLayerListItem))
                {
                    foreach (var item in GisEditor.LayerListManager.SelectedLayerListItems)
                    {
                        if (item.IsChecked != currentLayerListItem.IsChecked)
                        {
                            item.IsChecked = currentLayerListItem.IsChecked;
                        }
                    }
                }
                if (!currentLayerListItem.IsChecked)
                    LayerListHelper.RefreshInteractiveOverlays(currentLayerListItem);
            }
        }

        private static LinearGradientBrush GetDefaultLayerGroupBackground()
        {
            if (defaultBackground == null)
            {
                GradientStopCollection gradientStopCollection = new GradientStopCollection();
                gradientStopCollection.Add(new GradientStop((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFCECED4"), 0));
                gradientStopCollection.Add(new GradientStop((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFEDEEF4"), 0.8));

                defaultBackground = new LinearGradientBrush(gradientStopCollection, new Point(0, 0), new Point(0, 1));
            }
            return defaultBackground;
        }

        private void UpdateNewEntityVisibility(LayerListItem oldEntity, LayerListItem newEntity)
        {
            foreach (var layerGroupEntity in newEntity.Children)
            {
                var matchedLayerGroupEntity = oldEntity.Children
                    .FirstOrDefault(entity => entity.ConcreteObject == layerGroupEntity.ConcreteObject);
                if (matchedLayerGroupEntity != null)
                {
                    layerGroupEntity.ChildrenContainerVisibility = matchedLayerGroupEntity.ChildrenContainerVisibility;
                    var imageName = layerGroupEntity.ChildrenContainerVisibility == System.Windows.Visibility.Visible ? "up.png" : "down.png";
                    layerGroupEntity.SideImage = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/" + imageName, UriKind.Relative)) };
                    foreach (var layerEntity in layerGroupEntity.Children)
                    {
                        var matchedLayerEntity = matchedLayerGroupEntity.Children
                            .FirstOrDefault(tmpEntity => tmpEntity.ConcreteObject == layerEntity.ConcreteObject);
                        if (matchedLayerEntity != null)
                        {
                            layerEntity.ChildrenContainerVisibility = matchedLayerEntity.ChildrenContainerVisibility;
                            var layerImageName = layerEntity.ChildrenContainerVisibility == System.Windows.Visibility.Visible ? "arrowUp.png" : "arrowDown.png";
                            layerEntity.SideImage = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/" + layerImageName, UriKind.Relative)) };
                            UpdateTreeViewExpandStatus(matchedLayerEntity, layerEntity);
                        }
                    }
                }
            }
        }

        private void UpdateTreeViewExpandStatus(LayerListItem oldEntity, LayerListItem newEntity)
        {
            foreach (var zoomLevelEntity in newEntity.Children)
            {
                var matchedZoomLevelEntity = oldEntity.Children
                    .FirstOrDefault(subEntity => subEntity.Name.Equals(zoomLevelEntity.Name));
                if (matchedZoomLevelEntity != null)
                {
                    zoomLevelEntity.IsExpanded = matchedZoomLevelEntity.IsExpanded;
                    foreach (var styleEntity in zoomLevelEntity.Children)
                    {
                        var styleItem = styleEntity as StyleLayerListItem;
                        if (styleItem != null)
                        {
                            var matchedStyleEntity = matchedZoomLevelEntity.Children
                                .FirstOrDefault(subEntity =>
                                {
                                    var tmpStyleItem = subEntity as StyleLayerListItem;
                                    if (tmpStyleItem != null)
                                        return tmpStyleItem.ConcreteObject == styleItem.ConcreteObject;
                                    else return false;
                                });
                            if (matchedStyleEntity != null)
                                styleEntity.IsExpanded = matchedStyleEntity.IsExpanded;
                        }
                    }
                }
            }
        }

        private IEnumerable<Func<RefreshArgs, bool>> GetRefreshArgsToSkip()
        {
            yield return args => args is IsVisibleRefreshArgs;
            yield return args => args is NavigationRefreshArgs;
        }

        private void LoadProjectExpandSettingInternal(XElement projectSettings)
        {
            foreach (var mapXElement in projectSettings.Descendants("Map"))
            {
                Dictionary<string, bool> mapElementsExpands = new Dictionary<string, bool>();
                foreach (var overlayXElement in mapXElement.Descendants("Overlay"))
                {
                    AddExpandState(mapElementsExpands, overlayXElement);
                    foreach (var layerXElement in overlayXElement.Descendants("Layer"))
                    {
                        AddExpandState(mapElementsExpands, layerXElement);
                    }
                }
                expandStates[mapXElement.Attribute("Name").Value] = mapElementsExpands;
            }
        }

        private void LoadProjectSettingInternal(XElement projectSettings)
        {
            states = new Dictionary<string, Dictionary<string, Visibility>>();
            foreach (var mapXElement in projectSettings.Descendants("Map"))
            {
                Dictionary<string, Visibility> mapElementsVisibilities = new Dictionary<string, Visibility>();
                foreach (var overlayXElement in mapXElement.Descendants("Overlay"))
                {
                    AddState(mapElementsVisibilities, overlayXElement);
                    foreach (var layerXElement in overlayXElement.Descendants("Layer"))
                    {
                        AddState(mapElementsVisibilities, layerXElement);
                    }
                }
                states.Add(mapXElement.Attribute("Name").Value, mapElementsVisibilities);
            }
        }

        private XElement SaveProjectSettingInternal()
        {
            XElement expandStatusElement = new XElement("ExpandStatus");
            foreach (var item in allMapElementsEntity)
            {
                XElement mapXElement = new XElement("Map", new XAttribute("Name", item.Key));
                foreach (var overlayEntity in item.Value.Children)
                {
                    XElement overlayXElement = new XElement("Overlay", new XAttribute("Name", overlayEntity.Name), overlayEntity.ChildrenContainerVisibility.ToString());
                    foreach (var layerEntity in overlayEntity.Children)
                    {
                        XElement layerXElement = new XElement("Layer", new XAttribute("Name", layerEntity.Name), layerEntity.ChildrenContainerVisibility.ToString());
                        overlayXElement.Add(layerXElement);
                    }
                    mapXElement.Add(overlayXElement);
                }
                if (mapXElement.HasElements)
                {
                    expandStatusElement.Add(mapXElement);
                }
            }
            return expandStatusElement;
        }

        private XElement SaveProjectExpandSettingInternal()
        {
            XElement expandStatusElement = new XElement("ExpandStatus");
            var maps = GisEditor.GetMaps();
            foreach (var map in maps)
            {
                var item = GisEditor.LayerListManager.GetRootLayerListItem(map);
                RefreshExpandedStates(map.Name, item);
                XElement mapXElement = new XElement("Map", new XAttribute("Name", map.Name));
                foreach (var overlayEntity in item.Children)
                {
                    XElement overlayXElement = new XElement("Overlay", new XAttribute("Name", overlayEntity.Name), new XAttribute("IsExpanded", overlayEntity.IsExpanded.ToString()));
                    foreach (var layerEntity in overlayEntity.Children)
                    {
                        XElement layerXElement = new XElement("Layer", new XAttribute("Name", layerEntity.Name), new XAttribute("IsExpanded", layerEntity.IsExpanded.ToString()));
                        overlayXElement.Add(layerXElement);
                    }
                    mapXElement.Add(overlayXElement);
                }
                if (mapXElement.HasElements)
                {
                    expandStatusElement.Add(mapXElement);
                }
            }
            return expandStatusElement;
        }

        private void MapCurrentScaleChanged(object sender, CurrentScaleChangedWpfMapEventArgs e)
        {
            GisEditorWpfMap map = sender as GisEditorWpfMap;
            LayerListItem viewModel = null;
            if (map != null && (viewModel = layerListUserControl.DataContext as LayerListItem) != null)
            {
                var layerEntities = viewModel.Children.SelectMany(overlayEntity =>
                    overlayEntity.Children.ToDictionary(layerEntity => layerEntity, layerEntity =>
                    {
                        var featureLayer = layerEntity.ConcreteObject as FeatureLayer;
                        if (featureLayer != null)
                        {
                            e.CurrentExtent = GisEditor.ActiveMap.GetSnappedExtent(e.CurrentExtent);
                            var zoomLevel = featureLayer.ZoomLevelSet.GetZoomLevelForDrawing(e.CurrentExtent, map.ActualWidth, map.MapUnit);
                            return zoomLevel == null;
                        }
                        else return false;
                    }));
                BitmapImage bitmapImage = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Unavailable.png", UriKind.RelativeOrAbsolute));
                foreach (var item in layerEntities)
                {
                    //No style available
                    if (item.Value)
                    {
                        Image image = new Image();
                        image.BeginInit();
                        image.Source = bitmapImage;
                        image.EndInit();
                        item.Key.PreviewImage = image;
                        foreach (var styleItem in item.Key.Children)
                        {
                            styleItem.FontWeight = FontWeights.Normal;
                        }

                        //item.Key.TextStyleLabelVisibility = Visibility.Collapsed;
                    }
                    else
                    {
                        var currentZoomLevelIndex = GisEditor.ActiveMap.GetSnappedZoomLevelIndex(GisEditor.ActiveMap.CurrentScale, false) + 1;
                        foreach (var styleItemEntity in item.Key.Children.OfType<StyleLayerListItem>())
                        {
                            //var match = Regex.Match(styleItem.Text, MapElementViewModel.ZoomLevelPattern);
                            if (!string.IsNullOrEmpty(styleItemEntity.ZoomLevelRange))
                            {
                                int from = 0;
                                int to = 0;
                                var array = styleItemEntity.ZoomLevelRange.Split(" to ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                if (array.Length == 2)
                                {
                                    int.TryParse(array[0].Replace("(", "").Trim(), out from);
                                    int.TryParse(array[1].Replace(")", "").Trim(), out to);
                                    if (from <= currentZoomLevelIndex && to >= currentZoomLevelIndex)
                                        styleItemEntity.FontWeight = FontWeights.Bold;
                                    else styleItemEntity.FontWeight = FontWeights.Normal;
                                }
                            }
                        }

                        FeatureLayer featureLayer = item.Key.ConcreteObject as FeatureLayer;
                        if (featureLayer != null)
                        {
                            var zoomLevel = featureLayer.ZoomLevelSet.GetZoomLevelForDrawing(e.CurrentExtent, map.ActualWidth, map.MapUnit);
                            if (zoomLevel == null) break;
                            if (zoomLevel.CustomStyles.Count > 0)
                            {
                                BitmapSource bitmapSource = new BitmapImage();
                                var styleItem = GisEditor.StyleManager.GetStyleLayerListItem(zoomLevel.CustomStyles.LastOrDefault());
                                if (styleItem != null) bitmapSource = styleItem.GetPreviewSource(23, 23);
                                var img = new Image();
                                img.Source = bitmapSource;
                                item.Key.PreviewImage = img;
                            }
                            else
                            {
                                var disableIconSource = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/Unavailable.png", UriKind.RelativeOrAbsolute));
                                viewModel.PreviewImage = new Image() { Source = disableIconSource };
                            }
                            var textStyleCount = zoomLevel.CustomStyles
                                .Count(style => style is IconTextStyle);
                        }
                    }
                }
            }
        }

        private static void AddExpandState(Dictionary<string, bool> mapElementsExpands, XElement xElement)
        {
            var nameAttribute = xElement.Attribute("Name");
            if (nameAttribute != null)
            {
                var isExpandedAttribute = xElement.Attribute("IsExpanded");
                var isExpand = true;
                if (bool.TryParse(isExpandedAttribute.Value, out isExpand))
                {
                    mapElementsExpands[nameAttribute.Value] = isExpand;
                }
            }
        }

        private static void AddState(Dictionary<string, Visibility> mapElementsVisibilities, XElement xElement)
        {
            var nameAttribute = xElement.Attribute("Name");
            if (nameAttribute != null)
            {
                var visibility = Visibility.Collapsed;
                if (Enum.TryParse<Visibility>(xElement.Value, out visibility))
                {
                    mapElementsVisibilities[nameAttribute.Value] = visibility;
                }
            }
        }
    }
}