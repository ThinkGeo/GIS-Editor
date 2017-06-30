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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;
using Style = ThinkGeo.MapSuite.Styles.Style;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for NewTreeViewUserControl.xaml
    /// </summary>
    public partial class LayerListUserControl : System.Windows.Controls.UserControl
    {
        private static readonly string imageFormat = "/GisEditorPluginCore;component/{0}";

        private int doubleClickTime;
        private DispatcherTimer timer;
        private int mouseDownCount;
        private int mouseUpCount;
        private object eventSender;
        private Action singleClick;
        private Action doubleClick;

        public LayerListUserControl()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            doubleClickTime = System.Windows.Forms.SystemInformation.DoubleClickTime;
            timer.Interval = TimeSpan.FromMilliseconds(doubleClickTime);
            timer.Tick += new EventHandler(timer_Tick);

            if (GisEditor.LayerManager != null)
            {
                MenuItem newMenuItem = new MenuItem();
                newMenuItem.Header = GisEditor.LanguageManager.GetStringResource("LayerListUserControlNewLayerLabel");
                newMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/addNewLayer.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };

                foreach (var plugin in GisEditor.LayerManager.GetActiveLayerPlugins<FeatureLayerPlugin>()
                    .Where(p => p.CanCreateFeatureLayer))
                {
                    MenuItem menuItem = new MenuItem();
                    menuItem.Header = plugin.Name;
                    var bitmap = plugin.SmallIcon as BitmapImage;
                    if (bitmap != null)
                    {
                        menuItem.Icon = new Image
                        {
                            Source = new BitmapImage(bitmap.UriSource),
                            Width = 16,
                            Height = 16
                        };
                    }
                    menuItem.Command = CommandHelper.CreateNewLayerCommand;
                    menuItem.CommandParameter = plugin.Name;
                    newMenuItem.Items.Add(menuItem);
                }
                if (newMenuItem.Items.Count > 0)
                {
                    mainGrid.ContextMenu.Items.Add(newMenuItem);
                }
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (timer.IsEnabled)
            {
                timer.Stop();
                if (mouseDownCount == 1 && mouseUpCount == 1)
                {
                    OnSingleClick();
                }
                else if (mouseUpCount > 1 && mouseDownCount > 1)
                {
                    OnDoubleClick();
                }
                mouseDownCount = 0;
                mouseUpCount = 0;
                singleClick = null;
                doubleClick = null;
            }
        }

        private void OnSingleClick()
        {
            if (singleClick != null)
            {
                singleClick();
            }
        }

        private void OnDoubleClick()
        {
            if (doubleClick != null)
            {
                doubleClick();
            }
        }

        #region LayerGroup StackPanel SingleClick, DoubleClick

        [System.Reflection.Obfuscation]
        private void LayerGroupStackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var entity = sender.GetDataContext<LayerListItem>();
            HighlightStackPanel(entity);
            if (doubleClick == null)
            {
                doubleClick = new Action(() => { CollapseExpandLayerGroup(sender, e); });
                eventSender = sender;
            }
            mouseDownCount++;
            timer.Start();
        }

        [System.Reflection.Obfuscation]
        private void LayerGroupStackPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (timer.IsEnabled)
            {
                mouseUpCount++;
                if (eventSender == sender)
                {
                    if (mouseUpCount > 1)
                    {
                        timer.Stop();
                        doubleClick();
                        mouseDownCount = 0;
                        mouseUpCount = 0;
                        doubleClick = null;
                        eventSender = null;
                    }
                }
                else
                {
                    timer.Stop();
                    mouseDownCount = 0;
                    mouseUpCount = 0;
                    doubleClick = null;
                    eventSender = null;
                }
            }
            else
            {
                mouseDownCount = 0;
                mouseUpCount = 0;
            }
        }

        #endregion LayerGroup StackPanel SingleClick, DoubleClick

        #region Layer StackPanel SingleClick, DoubleClick

        [System.Reflection.Obfuscation]
        private void LayerStackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            var entity = sender.GetDataContext<LayerListItem>();

            if (entity != null)
            {
                HighlightStackPanel(entity);

                if (entity.ConcreteObject is Feature) return;

                if (doubleClick == null)
                {
                    doubleClick = new Action(() => { CollapseExpandLayer(sender, e); });
                    eventSender = sender;
                }
                mouseDownCount++;
                timer.Start();
            }
        }

        [System.Reflection.Obfuscation]
        private void LayerStackPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (timer.IsEnabled)
            {
                mouseUpCount++;
                if (eventSender == sender)
                {
                    if (mouseUpCount > 1)
                    {
                        timer.Stop();
                        doubleClick();
                        mouseUpCount = 0;
                        mouseDownCount = 0;
                        doubleClick = null;
                        eventSender = null;
                    }
                }
                else
                {
                    timer.Stop();
                    mouseUpCount = 0;
                    mouseDownCount = 0;
                    doubleClick = null;
                    eventSender = null;
                }
            }
            else
            {
                mouseUpCount = 0;
                mouseDownCount = 0;
            }
        }

        #endregion Layer StackPanel SingleClick, DoubleClick

        #region StyleSampleImage DoubleClick

        [System.Reflection.Obfuscation]
        private void StyleSampeImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (doubleClick == null)
            {
                doubleClick = new Action(() => { ShowStyleEditWindow(sender.GetDataContext<LayerListItem>()); });
                eventSender = sender;
            }
            mouseDownCount++;
            timer.Start();
        }

        [System.Reflection.Obfuscation]
        private void StyleSampeImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (timer.IsEnabled)
            {
                mouseUpCount++;
                if (eventSender == sender)
                {
                    if (mouseUpCount > 1)
                    {
                        timer.Stop();
                        doubleClick();
                        mouseUpCount = 0;
                        mouseDownCount = 0;
                        doubleClick = null;
                        eventSender = null;
                    }
                }
                else
                {
                    timer.Stop();
                    mouseUpCount = 0;
                    mouseDownCount = 0;
                    doubleClick = null;
                    eventSender = null;
                }
            }
            else
            {
                mouseUpCount = 0;
                mouseDownCount = 0;
            }
        }

        #endregion StyleSampleImage DoubleClick

        #region Text Style Image DoubleClick

        [System.Reflection.Obfuscation]
        private void ImageTextStyleLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (doubleClick == null)
            {
                doubleClick = new Action(() => { ShowTextStyleEditWindow(sender.GetDataContext<LayerListItem>()); });
                eventSender = sender;
            }
            mouseDownCount++;
            timer.Start();
        }

        [System.Reflection.Obfuscation]
        private void ImageTextStyleLabel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (timer.IsEnabled)
            {
                mouseUpCount++;
                if (eventSender == sender)
                {
                    if (mouseUpCount > 1)
                    {
                        timer.Stop();
                        doubleClick();
                        mouseUpCount = 0;
                        mouseDownCount = 0;
                        doubleClick = null;
                        eventSender = null;
                    }
                }
                else
                {
                    timer.Stop();
                    mouseUpCount = 0;
                    mouseDownCount = 0;
                    doubleClick = null;
                    eventSender = null;
                }
            }
            else
            {
                mouseUpCount = 0;
                mouseDownCount = 0;
            }
        }

        #endregion Text Style Image DoubleClick

        private void HighlightStackPanel(LayerListItem entity)
        {
            ResetAllBackground();
            bool isRemoved = false;
            if ((Keyboard.Modifiers == ModifierKeys.Shift) || (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)))
            {
                bool isIgnoreSelect = false;
                if (GisEditor.LayerListManager.SelectedLayerListItem != null)
                {
                    bool firstElementIsLayer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is Layer;
                    bool currentElementIsLayer = entity.ConcreteObject is Layer;
                    if (firstElementIsLayer && currentElementIsLayer)
                    {
                        Collection<LayerListItem> layerGroupOfLastItemItems = new Collection<LayerListItem>();
                        layerGroupOfLastItemItems = GisEditor.LayerListManager.SelectedLayerListItem.Parent.Children;
                        bool doesGroupOfLastItemContainEntity = layerGroupOfLastItemItems.Contains(entity);

                        if (doesGroupOfLastItemContainEntity)
                        {
                            if (!(Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)))
                            {
                                GisEditor.LayerListManager.SelectedLayerListItems.Clear();
                            }
                            int indexOfLastItem = layerGroupOfLastItemItems.IndexOf(GisEditor.LayerListManager.SelectedLayerListItem);
                            int indexOfEntity = layerGroupOfLastItemItems.IndexOf(entity);
                            int startingIndex;
                            int endingIndex;
                            if (indexOfEntity > indexOfLastItem)
                            {
                                startingIndex = indexOfLastItem;
                                endingIndex = indexOfEntity;
                            }
                            else
                            {
                                startingIndex = indexOfEntity;
                                endingIndex = indexOfLastItem;
                            }
                            for (int i = startingIndex; i <= endingIndex; i++)
                            {
                                if (!GisEditor.LayerListManager.SelectedLayerListItems.Contains(layerGroupOfLastItemItems[i]))
                                {
                                    GisEditor.LayerListManager.SelectedLayerListItems.Add(layerGroupOfLastItemItems[i]);
                                }
                            }
                        }
                        else { isIgnoreSelect = true; }
                    }
                    else { isIgnoreSelect = true; }
                }
                else
                {
                    GisEditor.LayerListManager.SelectedLayerListItem = entity;
                    entity.HighlightBackgroundBrush = new SolidColorBrush(Colors.LightBlue);
                }
                if (isIgnoreSelect)
                {
                    GisEditor.LayerListManager.SelectedLayerListItem.HighlightBackgroundBrush = new SolidColorBrush(Colors.LightBlue);
                    foreach (var item in GisEditor.LayerListManager.SelectedLayerListItems)
                    {
                        item.HighlightBackgroundBrush = new SolidColorBrush(Colors.LightBlue);
                    }
                    return;
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (GisEditor.LayerListManager.SelectedLayerListItems.Count > 0)
                {
                    bool firstElementIsLayer = GisEditor.LayerListManager.SelectedLayerListItems.FirstOrDefault().ConcreteObject is Layer;
                    bool currentElementIsLayer = entity.ConcreteObject is Layer;
                    if (!(firstElementIsLayer && currentElementIsLayer) && (firstElementIsLayer || currentElementIsLayer))
                    {
                        GisEditor.LayerListManager.SelectedLayerListItem = GisEditor.LayerListManager.SelectedLayerListItems.LastOrDefault();
                        foreach (var item in GisEditor.LayerListManager.SelectedLayerListItems)
                        {
                            item.HighlightBackgroundBrush = new SolidColorBrush(Colors.LightBlue);
                        }
                        return;
                    }
                }
                else if (GisEditor.LayerListManager.SelectedLayerListItem != null)
                {
                    if (!GisEditor.LayerListManager.SelectedLayerListItems.Contains(GisEditor.LayerListManager.SelectedLayerListItem))
                        GisEditor.LayerListManager.SelectedLayerListItems.Add(GisEditor.LayerListManager.SelectedLayerListItem);
                }
                if (!GisEditor.LayerListManager.SelectedLayerListItems.Contains(entity))
                {
                    if (GisEditor.LayerListManager.SelectedLayerListItems.LastOrDefault() == null)
                        GisEditor.LayerListManager.SelectedLayerListItems.Add(entity);
                    else
                    {
                        bool firstElementIsLayer = GisEditor.LayerListManager.SelectedLayerListItems.LastOrDefault().ConcreteObject is Layer;
                        bool currentElementIsLayer = entity.ConcreteObject is Layer;
                        if ((firstElementIsLayer && currentElementIsLayer) || !(firstElementIsLayer || currentElementIsLayer))
                            GisEditor.LayerListManager.SelectedLayerListItems.Add(entity);
                    }
                }
                else
                {
                    GisEditor.LayerListManager.SelectedLayerListItems.Remove(entity);
                    isRemoved = true;
                }
            }
            else
            {
                GisEditor.LayerListManager.SelectedLayerListItems.Clear();
                entity.HighlightBackgroundBrush = new SolidColorBrush(Colors.LightBlue);
            }

            if (!isRemoved)
                GisEditor.LayerListManager.SelectedLayerListItem = entity;
            else
                GisEditor.LayerListManager.SelectedLayerListItem = GisEditor.LayerListManager.SelectedLayerListItems.LastOrDefault();
            if (GisEditor.LayerListManager.SelectedLayerListItems.Count > 0)
            {
                foreach (var item in GisEditor.LayerListManager.SelectedLayerListItems)
                {
                    item.HighlightBackgroundBrush = new SolidColorBrush(Colors.LightBlue);
                }
            }
            var layerOverlay = entity.ConcreteObject as LayerOverlay;
            var layer = entity.ConcreteObject as Layer;
            if (layerOverlay != null && GisEditor.ActiveMap != null)
            {
                GisEditor.ActiveMap.ActiveOverlay = layerOverlay;
            }
            if (layer != null && GisEditor.ActiveMap != null)
            {
                GisEditor.ActiveMap.ActiveLayer = layer;
            }
        }

        [Obfuscation]
        private void LayerGroupStackPanel_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var currentViewModel = sender.GetDataContext<LayerListItem>();
            if (!GisEditor.LayerListManager.SelectedLayerListItems.Contains(currentViewModel))
            {
                HighlightStackPanel(currentViewModel);
            }
        }

        [Obfuscation]
        private void LayerGroupStackPanel_Click(object sender, RoutedEventArgs e)
        {
            CollapseExpandLayerGroup(sender, e);
        }

        [Obfuscation]
        private void LayerStackPanel_ButtonClick(object sender, RoutedEventArgs e)
        {
            CollapseExpandLayer(sender, e);
        }

        private static void CollapseExpandLayerGroup(object sender, RoutedEventArgs e)
        {
            if (!(e.OriginalSource is ToggleButton))
            {
                StackPanel sp = sender as StackPanel;
                if (sp != null)
                {
                    LayerListItem entity = sp.DataContext as LayerListItem;
                    if (entity != null)
                    {
                        string logoUri = entity.SideImage == null ? string.Empty : entity.SideImage.Source.ToString();
                        if (logoUri.EndsWith("down.png") || logoUri.EndsWith("up.png"))
                        {
                            var visibility = ToggleControlVisibility(entity);
                            bool isExpanded = visibility == Visibility.Visible ? true : false;
                            var layerListUIPlugin = GisEditor.UIManager.GetActiveUIPlugins<LayerListUIPlugin>().FirstOrDefault();
                            if (layerListUIPlugin != null)
                            {
                                var mapName = GisEditor.ActiveMap.Name;
                                if (!layerListUIPlugin.ExpandStates.ContainsKey(mapName))
                                {
                                    layerListUIPlugin.ExpandStates[mapName] = new Dictionary<string, bool>();
                                }
                                layerListUIPlugin.ExpandStates[mapName][entity.Name] = isExpanded;
                            }
                            string imageName = isExpanded ? "up" : "down";
                            string imageUri = string.Format(CultureInfo.InvariantCulture, imageFormat, "Images/" + imageName + ".png");
                            ((LayerListItem)sp.DataContext).SideImage = new Image
                            {
                                Source = new BitmapImage(new Uri(imageUri, UriKind.RelativeOrAbsolute))
                            };
                        }
                    }
                }
            }
        }

        private static void CollapseExpandLayer(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (!(e.OriginalSource is ToggleButton))
            {
                LayerListItem entity = sender.GetDataContext<LayerListItem>();
                if (entity != null)
                {
                    Visibility visibility = ToggleControlVisibility(entity);
                    bool isExpanded = visibility == Visibility.Visible ? true : false;
                    var layerListUIPlugin = GisEditor.UIManager.GetActiveUIPlugins<LayerListUIPlugin>().FirstOrDefault();
                    if (layerListUIPlugin != null)
                    {
                        var mapName = GisEditor.ActiveMap.Name;
                        if (layerListUIPlugin.ExpandStates.ContainsKey(mapName))
                        {
                            var result = layerListUIPlugin.ExpandStates[mapName];
                            if (result.ContainsKey(entity.Name))
                            {
                                result[entity.Name] = isExpanded;
                            }
                        }
                    }
                    string imageName = isExpanded ? "arrowUp" : "arrowDown";
                    string imageUri = string.Format(CultureInfo.InvariantCulture, imageFormat, "Images/" + imageName + ".png");
                    entity.SideImage = new Image { Source = new BitmapImage(new Uri(imageUri, UriKind.RelativeOrAbsolute)) };
                }
            }
        }

        [Obfuscation]
        private void LayerStackPanel_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            var currentViewModel = sender.GetDataContext<LayerListItem>();
            if (currentViewModel != null && !GisEditor.LayerListManager.SelectedLayerListItems.Contains(currentViewModel))
            {
                HighlightStackPanel(currentViewModel);
            }
        }

        private static Visibility ToggleControlVisibility(LayerListItem entity)
        {
            entity.ChildrenContainerVisibility = entity.ChildrenContainerVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            return entity.ChildrenContainerVisibility;
        }

        private void ShowTextStyleEditWindow(LayerListItem entity)
        {
            LoadLayerListItemOnDemand(entity);
            LayerListItem firstStyleEntity = (from zoomLeveleEntity in entity.Children
                                              from styleEntity in zoomLeveleEntity.Children
                                              where GetStyleFromObject(styleEntity.ConcreteObject) is IconTextStyle
                                              || GetStyleFromObject(styleEntity.ConcreteObject) is TextFilterStyle
                                              select styleEntity).FirstOrDefault();
            if (firstStyleEntity != null && firstStyleEntity.DoubleClicked != null)
            {
                firstStyleEntity.DoubleClicked();
            }
        }

        private static Style GetStyleFromObject(object styleItemObject)
        {
            var styleItem = styleItemObject as StyleLayerListItem;
            if (styleItem != null) return styleItem.ConcreteObject as Style;
            else return null;
        }

        private void ShowStyleEditWindow(LayerListItem entity)
        {
            LoadLayerListItemOnDemand(entity);
            var lastStyleEntity = (from styleEntity in entity.Children
                                   where !(GetStyleFromObject(styleEntity.ConcreteObject) is IconTextStyle
                                   || GetStyleFromObject(styleEntity.ConcreteObject) is TextFilterStyle)
                                   select styleEntity).FirstOrDefault();
            if (lastStyleEntity != null && lastStyleEntity.DoubleClicked != null)
            {
                lastStyleEntity.DoubleClicked();
            }
        }

        private static void LoadLayerListItemOnDemand(LayerListItem entity)
        {
            if (entity.Load != null)
            {
                entity.Load();
                entity.Load = null;
            }
        }

        [Obfuscation]
        private void RenameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                System.Windows.Controls.TextBox textBox = (System.Windows.Controls.TextBox)sender;
                textBox.Focus();
                textBox.SelectAll();
            }
        }

        #region Dragging Feature

        [Obfuscation]
        private void LayerGroup_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            LayerListItem currentLayerListItem = sender.GetDataContext<LayerListItem>();
            if (e.LeftButton == MouseButtonState.Pressed && currentLayerListItem != null)
            {
                if (GisEditor.LayerListManager.SelectedLayerListItem == currentLayerListItem)
                {
                    if (currentLayerListItem == null || (currentLayerListItem != null && !currentLayerListItem.IsRenaming))
                    {
                        DragDrop.DoDragDrop(itemsList, currentLayerListItem, System.Windows.DragDropEffects.Move);
                    }
                }
            }
        }

        private bool stopDragLeave;

        [Obfuscation]
        private void LayerGroup_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Handled = true;
            string[] dropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (!(dropFiles != null && dropFiles.Length > 0))
            {
                bool showLowerLine = false;
                FrameworkElement panelControl = sender as StackPanel;
                if (panelControl == null)
                {
                    Border border = sender as Border;
                    if (border != null)
                    {
                        StackPanel stackPanel = border.Child as StackPanel;
                        if (stackPanel != null)
                        {
                            StackPanel childStackPanel = stackPanel.Children[0] as StackPanel;
                            if (childStackPanel != null)
                            {
                                panelControl = childStackPanel.Children[1] as Grid;
                            }
                        }
                    }
                }
                if (panelControl != null)
                {
                    var point = e.GetPosition(panelControl);
                    showLowerLine = point.Y >= panelControl.ActualHeight * 0.5;
                    stopDragLeave = point.Y <= 5 || point.Y >= 34;
                }

                LayerListItem targetEntity = sender.GetDataContext<LayerListItem>();
                var draggedEntity = (LayerListItem)e.Data.GetData(typeof(LayerListItem));
                AutoAdjustScrollViewer(e);
                if (lastHighlightItem != null)
                {
                    lastHighlightItem.UpperLineVisibility = System.Windows.Visibility.Hidden;
                    lastHighlightItem.LowerLineVisibility = System.Windows.Visibility.Hidden;
                }

                if (draggedEntity != null && targetEntity != draggedEntity && draggedEntity.Parent != targetEntity && !(targetEntity.ConcreteObject is DynamicLayerOverlay) && !(draggedEntity.ConcreteObject is DynamicLayerOverlay))
                {
                    if (draggedEntity.ConcreteObject is Layer)
                    {
                        int subEntitiesCount = targetEntity.Children.Count;
                        if (targetEntity.ConcreteObject is LayerOverlay)
                        {
                            if (targetEntity.Children.Count > 0)
                            {
                                SetUpperIndicatLineForLayer(targetEntity.Children[0]);
                            }
                            else
                            {
                                SetLowerIndicatLineForLayer(targetEntity);
                            }
                        }
                        else if (targetEntity.ConcreteObject is Layer)
                        {
                            if (showLowerLine)
                            {
                                SetLowerIndicatLineForLayer(targetEntity);
                            }
                            else
                            {
                                SetUpperIndicatLineForLayer(targetEntity);
                            }
                        }
                    }
                    else if (draggedEntity.ConcreteObject is LayerOverlay && targetEntity.ConcreteObject is LayerOverlay)
                    {
                        if (showLowerLine)
                        {
                            SetLowerIndicatLineForLayer(targetEntity);
                        }
                        else
                        {
                            SetUpperIndicatLineForLayer(targetEntity);
                        }
                    }
                    else
                    {
                        e.Effects = DragDropEffects.None;
                    }
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
        }

        private static bool GetYPosition(object sender, System.Windows.DragEventArgs e)
        {
            FrameworkElement panelControl = sender as StackPanel;
            if (panelControl == null)
            {
                panelControl = sender as Border;
            }
            bool showLowerLine = false;
            if (panelControl != null)
            {
                var point = e.GetPosition(panelControl);
                var half = panelControl.ActualHeight * 0.5;
                showLowerLine = point.Y >= half;
            }
            return showLowerLine;
        }

        private void AutoAdjustScrollViewer(System.Windows.DragEventArgs e)
        {
            var mousePosition = e.GetPosition(this);
            if (mousePosition.Y <= 5)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 20);
            }
            else if (mousePosition.Y >= ActualHeight - 5)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 20);
            }
        }

        [Obfuscation]
        private void LayerGroup_Drop(object sender, System.Windows.DragEventArgs e)
        {
            e.Handled = true;
            string[] dropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            LayerListItem targetEntity = sender.GetDataContext<LayerListItem>();
            if (dropFiles != null && dropFiles.Length > 0)
            {
                Collection<Layer> layers = LayerListHelper.AddDropFilesToActiveMap(e, false);
                if (targetEntity != null && layers.Count > 0)
                {
                    LayerOverlay targetLayerOverlay = LayerListHelper.FindMapElementInLayerList<LayerOverlay>(targetEntity);
                    if (targetLayerOverlay != null && !targetLayerOverlay.Layers.Contains(layers[0]))
                    {
                        foreach (LayerOverlay overlay in GisEditor.ActiveMap.GetOverlaysContaining(layers[0]))
                        {
                            foreach (var item in layers)
                            {
                                if (overlay.Layers.Contains(item)) overlay.Layers.Remove(item);
                            }
                            overlay.Invalidate();
                        }

                        foreach (var item in layers)
                        {
                            targetLayerOverlay.Layers.Add(item);
                        }
                        targetLayerOverlay.Invalidate();
                    }
                    GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(null, RefreshArgsDescription.MapDropDescription));
                }
            }
            else
            {
                bool showLowerLine = GetYPosition(sender, e);
                ResetIndicateLine();
                var draggedEntity = (LayerListItem)e.Data.GetData(typeof(LayerListItem));
                if (draggedEntity != null && targetEntity != null && draggedEntity != targetEntity && !(targetEntity.ConcreteObject is DynamicLayerOverlay))
                {
                    if (targetEntity.Parent == draggedEntity.Parent)
                    {
                        ExchangeElement(targetEntity, draggedEntity, showLowerLine);
                        UpdateLayout();
                    }
                    else if (draggedEntity.Parent != null && targetEntity.Parent.ConcreteObject is LayerOverlay
                        && draggedEntity.Parent.ConcreteObject is LayerOverlay)
                    {
                        int targetIndex = targetEntity.Parent.Children.IndexOf(targetEntity);
                        draggedEntity.Parent.Children.Remove(draggedEntity);
                        if (showLowerLine)
                        {
                            targetEntity.Parent.Children.Insert(targetIndex + 1, draggedEntity);
                        }
                        else
                        {
                            targetEntity.Parent.Children.Insert(targetIndex, draggedEntity);
                        }

                        var targetOverlay = targetEntity.Parent.ConcreteObject as LayerOverlay;
                        var draggedOverlay = draggedEntity.Parent.ConcreteObject as LayerOverlay;
                        var targetLayer = targetEntity.ConcreteObject as Layer;
                        var draggedLayer = draggedEntity.ConcreteObject as Layer;
                        if (targetOverlay != null && draggedOverlay != null && targetLayer != null && draggedLayer != null)
                        {
                            lock (targetOverlay.Layers)
                            {
                                int targetLayerIndex = targetOverlay.Layers.IndexOf(targetLayer);
                                if (showLowerLine)
                                {
                                    targetOverlay.Layers.Insert(targetLayerIndex, draggedLayer);
                                }
                                else
                                {
                                    targetOverlay.Layers.Insert(targetLayerIndex + 1, draggedLayer);
                                }
                            }

                            lock (draggedOverlay.Layers) draggedOverlay.Layers.Remove(draggedLayer);

                            targetOverlay.Invalidate();
                            draggedOverlay.Invalidate();
                        }

                        draggedEntity.Parent = targetEntity.Parent;
                        UpdateLayout();
                    }
                    else if (draggedEntity.ConcreteObject is Layer
                        && targetEntity.ConcreteObject is LayerOverlay
                        && draggedEntity.Parent != targetEntity
                        && !(draggedEntity.Parent.ConcreteObject is Layer)
                        )
                    {
                        draggedEntity.Parent.Children.Remove(draggedEntity);

                        if (draggedEntity.Parent.Children.Count == 0)
                        {
                            draggedEntity.Parent.IsChecked = false;
                        }

                        targetEntity.Children.Insert(0, draggedEntity);

                        if (targetEntity.Children.Count == 1)
                        {
                            targetEntity.IsChecked = draggedEntity.IsChecked;
                        }

                        var featureLayer = draggedEntity.ConcreteObject as Layer;
                        var layerOverlay = draggedEntity.Parent.ConcreteObject as LayerOverlay;
                        var targetOverlay = targetEntity.ConcreteObject as LayerOverlay;

                        if (layerOverlay != null)
                        {
                            lock (layerOverlay.Layers) layerOverlay.Layers.Remove(featureLayer);
                            lock (targetOverlay.Layers) targetOverlay.Layers.Add(featureLayer);

                            layerOverlay.RefreshCache(RefreshCacheMode.ApplyNewCache);
                            targetOverlay.RefreshCache(RefreshCacheMode.ApplyNewCache);

                            GisEditor.ActiveMap.Refresh(new Collection<Overlay> { layerOverlay, targetOverlay });
                        }
                        draggedEntity.Parent = targetEntity;
                        UpdateLayout();
                    }
                }
            }
        }

        private void ExchangeElement(LayerListItem target, LayerListItem source, bool insertLower)
        {
            var parentEntity = target.Parent;
            int targetIndex = parentEntity.Children.IndexOf(target);
            int sourceIndex = parentEntity.Children.IndexOf(source);
            parentEntity.Children.Remove(source);
            if (insertLower)
            {
                if (targetIndex > sourceIndex)
                {
                    parentEntity.Children.Insert(targetIndex, source);
                }
                else
                {
                    parentEntity.Children.Insert(targetIndex + 1, source);
                }
            }
            else
            {
                if (targetIndex > sourceIndex)
                {
                    parentEntity.Children.Insert(targetIndex - 1 < 0 ? 0 : targetIndex - 1, source);
                }
                else
                {
                    parentEntity.Children.Insert(targetIndex, source);
                }
            }

            var layerOverlay = parentEntity.ConcreteObject as LayerOverlay;
            var targetLayer = target.ConcreteObject as Layer;
            var draggedLayer = source.ConcreteObject as Layer;

            var targetOverlay = target.ConcreteObject as LayerOverlay;
            var draggedOverlay = source.ConcreteObject as LayerOverlay;

            if (layerOverlay != null && targetLayer != null && draggedLayer != null)
            {
                lock (layerOverlay.Layers)
                {
                    int targetLayerIndex = layerOverlay.Layers.IndexOf(targetLayer);
                    int sourceLayerIndex = layerOverlay.Layers.IndexOf(draggedLayer);
                    layerOverlay.Layers.Remove(draggedLayer);
                    if (insertLower)
                    {
                        if (targetLayerIndex > sourceLayerIndex)
                        {
                            layerOverlay.Layers.Insert(targetLayerIndex - 1, draggedLayer);
                        }
                        else
                        {
                            layerOverlay.Layers.Insert(targetLayerIndex, draggedLayer);
                        }
                    }
                    else
                    {
                        if (targetLayerIndex > sourceLayerIndex)
                        {
                            layerOverlay.Layers.Insert(targetLayerIndex, draggedLayer);
                        }
                        else
                        {
                            layerOverlay.Layers.Insert(targetLayerIndex + 1, draggedLayer);
                        }
                    }

                }

                layerOverlay.Invalidate();
            }
            else if (targetOverlay != null && draggedOverlay != null)
            {
                int targetOverlayIndex = GisEditor.ActiveMap.Overlays.IndexOf(targetOverlay);
                int sourceOverlayIndex = GisEditor.ActiveMap.Overlays.IndexOf(draggedOverlay);

                GisEditor.ActiveMap.Overlays.Remove(draggedOverlay);
                if (insertLower)
                {
                    if (targetOverlayIndex > sourceOverlayIndex)
                    {
                        GisEditor.ActiveMap.Overlays.Insert(targetOverlayIndex - 1, draggedOverlay);
                    }
                    else
                    {
                        GisEditor.ActiveMap.Overlays.Insert(targetOverlayIndex, draggedOverlay);
                    }
                }
                else
                {
                    if (targetOverlayIndex > sourceOverlayIndex)
                    {
                        GisEditor.ActiveMap.Overlays.Insert(targetOverlayIndex, draggedOverlay);
                    }
                    else
                    {
                        GisEditor.ActiveMap.Overlays.Insert(targetOverlayIndex + 1, draggedOverlay);
                    }
                }

                GisEditor.ActiveMap.Refresh();
            }
        }

        #endregion Dragging Feature

        private void ResetAllBackground()
        {
            LayerListItem mainEntity = DataContext as LayerListItem;
            if (mainEntity != null)
            {
                foreach (var layerGroupEntity in mainEntity.Children)
                {
                    layerGroupEntity.HighlightBackgroundBrush = layerGroupEntity.BackgroundBrush;
                    foreach (var layerEntity in layerGroupEntity.Children)
                    {
                        layerEntity.HighlightBackgroundBrush = layerEntity.BackgroundBrush;
                    }
                }
            }
        }

        private void ResetIndicateLine()
        {
            LayerListItem mainEntity = DataContext as LayerListItem;
            if (mainEntity != null)
            {
                foreach (var layerGroupEntity in mainEntity.Children)
                {
                    layerGroupEntity.UpperLineVisibility = Visibility.Hidden;
                    layerGroupEntity.LowerLineVisibility = Visibility.Hidden;
                    foreach (var layerEntity in layerGroupEntity.Children)
                    {
                        layerEntity.UpperLineVisibility = Visibility.Hidden;
                        layerEntity.LowerLineVisibility = Visibility.Hidden;
                    }
                }
            }
        }

        [Obfuscation]
        private void GridPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            int layerStackPanelWidthExcludeText = 5 + 13 + 5 + 22 + 23 + 12;
            double actualWidth = e.NewSize.Width - layerStackPanelWidthExcludeText;
            var panel = sender as Grid;
            if (panel != null)
            {
                var tb = panel.Children[2] as RenameTextBlock;
                LayerListItem mapElement = panel.DataContext as LayerListItem;
                if (tb != null && mapElement != null)
                {
                    if (mapElement.Name.Length > 18 && tb.DesiredSize.Width > actualWidth)
                    {
                        tb.SetBinding(TextBlock.TextProperty, "Name");
                        tb.DisplayText = mapElement.Name.Substring(0, 15) + "...";
                    }
                }
            }
        }

        private LayerListItem lastHighlightItem;

        private void SetLowerIndicatLineForLayer(LayerListItem entity)
        {
            entity.LowerLineVisibility = Visibility.Visible;
            lastHighlightItem = entity;
        }

        private void SetUpperIndicatLineForLayer(LayerListItem entity)
        {
            entity.UpperLineVisibility = Visibility.Visible;
            lastHighlightItem = entity;
        }

        private void SetUpperIndicatLineForLayerGroup(LayerListItem entity)
        {
            entity.UpperLineVisibility = Visibility.Visible;
            lastHighlightItem = entity;
        }

        [Obfuscation]
        private void LayerGroup_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            if (!stopDragLeave)
            {
                var frameworkElement = sender as FrameworkElement;
                if (frameworkElement != null)
                {
                    var point = e.GetPosition(frameworkElement);
                    var entity = frameworkElement.DataContext as LayerListItem;

                    if (entity != null)
                    {
                        if (entity.ConcreteObject is LayerOverlay)
                        {
                            entity.LowerLineVisibility = Visibility.Hidden;
                            entity.UpperLineVisibility = Visibility.Hidden;
                            foreach (var item in entity.Children)
                            {
                                item.LowerLineVisibility = Visibility.Hidden;
                                item.UpperLineVisibility = Visibility.Hidden;
                            }
                        }
                        else if (entity.ConcreteObject is Layer)
                        {
                            entity.LowerLineVisibility = Visibility.Hidden;
                            entity.UpperLineVisibility = Visibility.Hidden;
                        }
                    }
                }
            }
        }

        [Obfuscation]
        private void RenameControl_TextRenamed(object sender, TextRenamedEventArgs e)
        {
            LayerListItem currentEntity = sender.GetDataContext<LayerListItem>();
            if (currentEntity != null)
            {
                currentEntity.Name = e.NewText;
                if (currentEntity.ConcreteObject is MapShape)
                {
                    ((MapShape)currentEntity.ConcreteObject).Feature.ColumnValues["DisplayName"] = e.NewText;
                }
                else
                {
                    if (!e.OldText.Equals(e.NewText))
                        GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.RenameControlTextRenamedDescription));
                }
            }
        }

        [Obfuscation]
        private void TreeViewPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        [Obfuscation]
        private void LayerListContextMenuOpening(object sender, MouseButtonEventArgs e)
        {
            var selectedLayerListItem = sender.GetDataContext<LayerListItem>();
            var frameworkElement = sender as FrameworkElement;
            if (selectedLayerListItem != null && frameworkElement != null)
            {
                if (frameworkElement.ContextMenu == null)
                {
                    frameworkElement.ContextMenu = new ContextMenu();
                }

                foreach (var item in frameworkElement.ContextMenu.Items.OfType<MenuItem>())
                {
                    item.Tag = null;
                }
                frameworkElement.ContextMenu.Items.Clear();
                bool isDataSourceAvailable = true;
                if (selectedLayerListItem.ConcreteObject is Layer)
                {
                    Layer layer = (Layer)selectedLayerListItem.ConcreteObject;
                    LayerPlugin layerPlugin = GisEditor.LayerManager.GetLayerPlugins(layer.GetType()).FirstOrDefault();
                    if (layerPlugin != null)
                    {
                        isDataSourceAvailable = layerPlugin.DataSourceResolveTool.IsDataSourceAvailable(layer);
                    }
                }
                if (isDataSourceAvailable)
                {
                    foreach (var item in GisEditor.LayerListManager.GetLayerListContextMenuItems(selectedLayerListItem))
                    {
                        if (item.Header.Equals("--"))
                            frameworkElement.ContextMenu.Items.Add(new Separator());
                        else
                            frameworkElement.ContextMenu.Items.Add(item);
                    }
                }
                else
                {
                    var removeItem = LayerListMenuItemHelper.GetRemoveLayerMenuItem();
                    frameworkElement.ContextMenu.Items.Add(removeItem);

                    MenuItem resolveItem = new MenuItem();
                    resolveItem.Tag = selectedLayerListItem;
                    resolveItem.Header = "Resolve";
                    resolveItem.Click += ResolveItem_Click;
                    frameworkElement.ContextMenu.Items.Add(resolveItem);
                }
            }
        }

        private void ResolveItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            LayerListItem layerItem = (LayerListItem)item.Tag;
            Layer layer = (Layer)layerItem.ConcreteObject;
            LayerPlugin layerPlugin = GisEditor.LayerManager.GetLayerPlugins(layer.GetType()).FirstOrDefault();
            if (layerPlugin != null)
            {
                layerPlugin.DataSourceResolveTool.ResolveDataSource(layer);
                bool isDataSourceAvailable = layerPlugin.DataSourceResolveTool.IsDataSourceAvailable(layer);
                if (isDataSourceAvailable)
                {
                    layer.IsVisible = true;
                    layerItem.WarningImages.Clear();
                }
            }
        }

        [Obfuscation]
        private void LayerListUserControl_Drop(object sender, DragEventArgs e)
        {
            LayerListHelper.AddDropFilesToActiveMap(e);
        }

        private int GetShapeFileFeatureCount(ShapeFileFeatureLayer shapeFileFeatureLayer)
        {
            if (!shapeFileFeatureLayer.IsOpen) shapeFileFeatureLayer.Open();
            var count = shapeFileFeatureLayer.GetRecordCount();
            shapeFileFeatureLayer.Close();
            return count;
        }

        private int GetInMemoryFeatureCount(InMemoryFeatureLayer inMemoryFeatureLayer)
        {
            if (!inMemoryFeatureLayer.IsOpen) inMemoryFeatureLayer.Open();
            var count = inMemoryFeatureLayer.QueryTools.GetCount();
            count += inMemoryFeatureLayer.FeatureIdsToExclude.Count;
            inMemoryFeatureLayer.Close();
            return count;
        }
    }
}