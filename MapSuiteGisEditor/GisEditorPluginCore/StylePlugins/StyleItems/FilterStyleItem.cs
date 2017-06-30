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
using System.Linq;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class FilterStyleItem : InternalStyleItem
    {
        [NonSerialized]
        private FilterStyleUserControl userControl;
        [NonSerialized]
        private FilterStyle filterStyle;

        public FilterStyleItem(FilterStyle style)
            : base(style)
        {
            CanRename = true;
            CanAddInnerStyle = true;
            filterStyle = style;
            if (filterStyle != null)
            {
                foreach (var innerStyle in filterStyle.Styles.Reverse())
                {
                    StyleLayerListItem styleItem = GisEditor.StyleManager.GetStyleLayerListItem(innerStyle);
                    Children.Add(styleItem);
                }
            }
        }

        protected override bool CanContainStyleItemCore(StyleLayerListItem styleItem)
        {
            return styleItem.ConcreteObject is Style;
        }

        protected override StyleUserControl CreateUI(StyleBuilderArguments styleArguments)
        {
            if (userControl == null)
            {
                StylePluginHelper.FillRequiredValueForStyleArguments(styleArguments);
                userControl = new FilterStyleUserControl(ConcreteObject as FilterStyle, styleArguments);

                if (Children.Count == 0)
                {
                    var pointStylePlugin = GisEditor.StyleManager.GetDefaultStylePlugin(styleArguments.AvailableStyleCategories);
                    if (pointStylePlugin != null)
                    {
                        var style = pointStylePlugin.GetDefaultStyle();
                        style.Name = pointStylePlugin.Name;
                        var styleItem = GisEditor.StyleManager.GetStyleLayerListItem(style);
                        Children.Add(styleItem);
                        UpdateConcreteObject();
                    }
                }
            }
            return userControl;
        }

        protected override void UpdateStyleItemCore()
        {
            base.UpdateStyleItemCore();
            if (filterStyle != null)
            {
                var deletedStyles = Children.Where(i => !filterStyle.Styles.Any(s => s == i.ConcreteObject)).ToList();
                foreach (var deletedStyle in deletedStyles)
                {
                    if (Children.Contains(deletedStyle))
                    {
                        Children.Remove(deletedStyle);
                    }
                }

                var styles = filterStyle.Styles.Where(s => !Children.Any(i => i.ConcreteObject == s)).Reverse().ToList();
                foreach (var style in styles)
                {
                    var styleItem = GisEditor.StyleManager.GetStyleLayerListItem(style);
                    Children.Add(styleItem);
                }
            }
        }

        protected override void UpdateConcreteObjectCore()
        {
            base.UpdateConcreteObjectCore();

            filterStyle.Styles.Clear();
            foreach (var style in Children.Select(i => i.ConcreteObject).OfType<Style>().Reverse())
            {
                filterStyle.Styles.Add(style);
            }
        }
    }
}
