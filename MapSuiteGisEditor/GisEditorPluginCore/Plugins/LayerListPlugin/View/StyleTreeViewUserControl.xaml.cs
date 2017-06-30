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


using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for TreeViewUserControl.xaml
    /// </summary>
    [Obfuscation]
    internal partial class StyleTreeViewUserControl : UserControl
    {
        public StyleTreeViewUserControl()
        {
            InitializeComponent();
            Visibility = Visibility.Visible;
        }

        [Obfuscation]
        private void TreeViewItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedLayerListItem = sender.GetDataContext<LayerListItem>();
            if (selectedLayerListItem != null
                && GisEditor.LayerListManager.SelectedLayerListItem == selectedLayerListItem
                && GisEditor.LayerListManager.SelectedLayerListItem.DoubleClicked != null)
            {
                GisEditor.LayerListManager.SelectedLayerListItem.DoubleClicked();
            }
        }

        [Obfuscation]
        private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            LayerListItem selectedItem = tree.SelectedItem as LayerListItem;
            if (selectedItem != null)
            {
                GisEditor.LayerListManager.SelectedLayerListItem = selectedItem;
                var overlay = LayerListHelper.FindMapElementInLayerList<Overlay>(selectedItem);
                var layer = LayerListHelper.FindMapElementInLayerList<Layer>(selectedItem);

                if (GisEditor.ActiveMap != null)
                {
                    if (overlay != null) GisEditor.ActiveMap.ActiveOverlay = overlay;
                    if (layer != null) GisEditor.ActiveMap.ActiveLayer = layer;
                }
            }
        }

        [Obfuscation]
        private void TreeNode_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var currentLayerListItem = sender.GetDataContext<LayerListItem>();
            if (currentLayerListItem != null)
            {
                currentLayerListItem.IsSelected = true;
                GisEditor.LayerListManager.SelectedLayerListItem = currentLayerListItem;
            }
            e.Handled = true;
        }

        [Obfuscation]
        private void StyleTextRenamed(object sender, TextRenamedEventArgs e)
        {
            LayerListItem currentEntity = sender.GetDataContext<LayerListItem>();
            if (!string.IsNullOrEmpty(e.NewText) && !string.IsNullOrEmpty(e.NewText.TrimEnd()))
            {
                if (currentEntity != null && !e.OldText.Equals(e.NewText))
                {
                    currentEntity.Name = e.NewText;
                    var styleItem = currentEntity as StyleLayerListItem;
                    if (styleItem != null && styleItem.CanRename)
                    {
                        styleItem.Name = e.NewText;
                        styleItem.UpdateConcreteObject();
                    }
                }
                else
                {
                    e.IsCancelled = true;
                }
            }
            else
            {
                MessageBox.Show(GisEditor.LanguageManager.GetStringResource("NewNameCannotEmptyText"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"));
                e.IsCancelled = true;
            }
        }

        [Obfuscation]
        private void LayerListContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectedLayerListItem = sender.GetDataContext<LayerListItem>();
            var frameworkElement = sender as FrameworkElement;
            if (selectedLayerListItem != null && frameworkElement != null)
            {
                frameworkElement.ContextMenu.Items.Clear();
                foreach (var item in GisEditor.LayerListManager.GetLayerListContextMenuItems(selectedLayerListItem))
                {
                    if (item.Header.Equals("--"))
                        frameworkElement.ContextMenu.Items.Add(new Separator());
                    else
                        frameworkElement.ContextMenu.Items.Add(item);
                }
            }
        }
    }
}
