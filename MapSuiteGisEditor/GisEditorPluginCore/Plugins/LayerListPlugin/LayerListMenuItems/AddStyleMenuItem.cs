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


using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    //public class AddStyleMenuItemViewModel : LayerListMenuItem
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetAddStyleMenuItem(AddStyleTypes addStyleType, FeatureLayer featureLayer)
        {
            var command = new ObservedCommand(() => { }, () => !(GisEditor.LayerListManager.SelectedLayerListItems.Count > 0));

            var menuItem = GetMenuItem(GisEditor.LanguageManager.GetStringResource("StyleBuilderWindowAddStyleLabel"), "/GisEditorPluginCore;component/Images/addstyle.png", null);
            menuItem.Items.Add(GetLoadFromLibraryMenuItem());
            menuItem.Items.Add(new Separator());
            AddSubMenuItems(addStyleType, menuItem);

            ShapeFileFeatureLayer shpLayer = featureLayer as ShapeFileFeatureLayer;
            if (shpLayer != null)
            {
                if (menuItem.Items.Count > 0) menuItem.Items.Add(new Separator());
                menuItem.Items.Add(LayerListMenuItemHelper.GetAddStyleWizardMenuItem(shpLayer));
            }
            return menuItem;
        }

        private static void AddSubMenuItems(AddStyleTypes addStyleType, MenuItem menuItem)
        {
            if ((addStyleType & AddStyleTypes.AddAreaStyle) == AddStyleTypes.AddAreaStyle)
            {
                bool hasItems = false;
                foreach (StylePlugin stylePlugin in GisEditor.StyleManager
                    .GetStylePlugins(StyleCategories.Area | StyleCategories.Composite))
                {
                    hasItems = true;
                    menuItem.Items.Add(LayerListMenuItemHelper.GetAddSpecifiedStyleByPluginMenuItem(stylePlugin));
                }

                if (hasItems) menuItem.Items.Add(new Separator());
            }

            if ((addStyleType & AddStyleTypes.AddLineStyle) == AddStyleTypes.AddLineStyle)
            {
                bool hasItems = false;
                foreach (StylePlugin stylePlugin in GisEditor.StyleManager
                    .GetStylePlugins(StyleCategories.Line | StyleCategories.Composite))
                {
                    hasItems = true;
                    menuItem.Items.Add(LayerListMenuItemHelper.GetAddSpecifiedStyleByPluginMenuItem(stylePlugin));
                }

                if (hasItems) menuItem.Items.Add(new Separator());
            }

            if ((addStyleType & AddStyleTypes.AddPointStyle) == AddStyleTypes.AddPointStyle)
            {
                bool hasItems = false;
                foreach (StylePlugin stylePlugin in GisEditor.StyleManager
                    .GetStylePlugins(StyleCategories.Point | StyleCategories.Composite))
                {
                    hasItems = true;
                    menuItem.Items.Add(LayerListMenuItemHelper.GetAddSpecifiedStyleByPluginMenuItem(stylePlugin));
                }

                if (hasItems) menuItem.Items.Add(new Separator());
            }

            if ((addStyleType & AddStyleTypes.AddTextStyle) == AddStyleTypes.AddTextStyle)
            {
                foreach (StylePlugin stylePlugin in GisEditor.StyleManager
                    .GetStylePlugins(StyleCategories.Label))
                {
                    menuItem.Items.Add(LayerListMenuItemHelper.GetAddSpecifiedStyleByPluginMenuItem(stylePlugin));
                }
            }

            var lastItem = menuItem.Items[menuItem.Items.Count - 1] as Separator;
            if (lastItem != null)
            {
                menuItem.Items.Remove(lastItem);
            }
        }
    }
}