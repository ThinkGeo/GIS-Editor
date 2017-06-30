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
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;
using Style = ThinkGeo.MapSuite.Styles.Style;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Obfuscation]
    internal partial class StyleTreeViewUserControl
    {
        [Obfuscation]
        private void TreeNode_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var draggedEntity = sender.GetDataContext<LayerListItem>();
                if (GisEditor.LayerListManager.SelectedLayerListItem == draggedEntity
                    && !draggedEntity.IsRenaming && !(draggedEntity.Parent.ConcreteObject is FeatureLayer))
                {
                    var dragObejct = new DragWrapper() { Object = draggedEntity };
                    DragDrop.DoDragDrop(tree, dragObejct, DragDropEffects.Move);
                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        [Obfuscation]
        private void TreeNode_DragOver(object sender, DragEventArgs e)
        {
            LayerListItem targetEntity = sender.GetDataContext<LayerListItem>();
            var dragWrapper = e.Data.GetData(typeof(DragWrapper)) as DragWrapper;
            if (dragWrapper != null)
            {
                LayerListItem dragedEntity = dragWrapper.Object;
                if (targetEntity == null)
                {
                    e.Effects = DragDropEffects.None;
                }
                else if (targetEntity.Parent != dragedEntity.Parent)
                {
                    e.Effects = DragDropEffects.None;
                }
            }
        }

        [Obfuscation]
        private void TreeNode_Drop(object sender, DragEventArgs e)
        {
            var dragWrapper = e.Data.GetData(typeof(DragWrapper)) as DragWrapper;
            if (dragWrapper != null)
            {
                LayerListItem dragedEntity = dragWrapper.Object;
                LayerListItem targetEntity = sender.GetDataContext<LayerListItem>();
                if (dragedEntity != null && targetEntity != null && dragedEntity.ConcreteObject is Style && !(dragedEntity.Parent.ConcreteObject is FeatureLayer))
                {
                    ExchangeElement(dragedEntity, targetEntity);
                }
            }
        }

        private void ExchangeElement(LayerListItem dragedEntity, LayerListItem targetEntity)
        {
            var dragEntityParent = dragedEntity.Parent;
            var targetEntityParent = targetEntity.Parent;
            if (dragEntityParent == targetEntityParent)
            {
                var targetStyleItem = targetEntityParent as StyleLayerListItem;
                if (targetStyleItem != null)
                {
                    var targetStyle = ((StyleLayerListItem)targetEntity);
                    var dragedStyle = ((StyleLayerListItem)dragedEntity);
                    if (targetStyle != null && dragedStyle != null)
                    {
                        int targetStyleIndex = targetStyleItem.Children.IndexOf(targetStyle);
                        int dragedStyleIndex = targetStyleItem.Children.IndexOf(dragedStyle);

                        targetStyleItem.Children[targetStyleIndex] = dragedStyle;
                        targetStyleItem.Children[dragedStyleIndex] = targetStyle;
                        targetStyleItem.UpdateConcreteObject();
                    }
                }
                RearrangeStylesInZoomLevel(dragedEntity, targetEntity);

                var componentStyleEntity = LayerListHelper.FindItemInLayerList<CompositeStyle>(dragedEntity);

                if (componentStyleEntity != null)
                {
                    var bitmapSource = new BitmapImage();
                    bitmapSource = ((StyleLayerListItem)componentStyleEntity).GetPreviewSource(23, 23) as BitmapImage;
                    componentStyleEntity.PreviewImage = new Image { Source = bitmapSource };
                    var featureLayer = componentStyleEntity.Parent.ConcreteObject as FeatureLayer;
                }
                dragedEntity.IsSelected = true;
                TileOverlay overlay = LayerListHelper.FindMapElementInLayerList<TileOverlay>(dragedEntity);
                if (overlay != null)
                {
                    overlay.Invalidate();
                }
            }
        }

        private static void RearrangeStylesInZoomLevel(LayerListItem dragedEntity, LayerListItem targetEntity)
        {
            var featureLayer = dragedEntity.Parent.Parent.ConcreteObject as FeatureLayer;
            var currentZoomLevel = dragedEntity.Parent.ConcreteObject as ZoomLevel;
            if (currentZoomLevel != null && featureLayer != null)
            {
                var from = GisEditor.ActiveMap.GetSnappedZoomLevelIndex(currentZoomLevel.Scale, false) + 1;
                var to = (int)currentZoomLevel.ApplyUntilZoomLevel;
                for (int i = from - 1; i < to; i++)
                {
                    var tmpZoomLevel = featureLayer.ZoomLevelSet.CustomZoomLevels[i];
                    int index = tmpZoomLevel.CustomStyles.IndexOf(GetStyleFromObject(targetEntity.ConcreteObject));
                    tmpZoomLevel.CustomStyles.Remove(GetStyleFromObject(dragedEntity.ConcreteObject));
                    tmpZoomLevel.CustomStyles.Insert(index, GetStyleFromObject(dragedEntity.ConcreteObject));
                }
            }
        }

        private static Style GetStyleFromObject(object styleItemObject)
        {
            var styleItem = styleItemObject as StyleLayerListItem;
            if (styleItem != null) return styleItem.ConcreteObject as Style;
            else return null;
        }

        class DragWrapper
        {
            public LayerListItem Object { get; set; }
        }
    }
}
