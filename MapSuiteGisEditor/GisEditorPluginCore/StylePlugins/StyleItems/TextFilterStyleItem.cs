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
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class TextFilterStyleItem : InternalStyleItem
    {
        [NonSerialized]
        private FilterStyleUserControl userControl;

        [NonSerialized]
        private TextFilterStyle textFilterStyle;

        public TextFilterStyleItem(Style style)
            : base(style)
        {
            CanRename = true;
            CanAddInnerStyle = true;
            textFilterStyle = style as TextFilterStyle;

            if (textFilterStyle != null)
            {
                foreach (var innerStyle in textFilterStyle.Styles.Reverse())
                {
                    StyleLayerListItem styleItem = GisEditor.StyleManager.GetStyleLayerListItem(innerStyle);
                    Children.Add(styleItem);
                }
            }
        }

        protected override bool CanContainStyleItemCore(StyleLayerListItem styleItem)
        {
            return styleItem.ConcreteObject is IconTextStyle || styleItem.ConcreteObject is TextFilterStyle;
        }

        protected override StyleCategories GetRestrictStyleCategoriesCore()
        {
            return StyleCategories.Label;
        }

        protected override StyleUserControl CreateUI(StyleBuilderArguments styleArguments)
        {
            if (userControl == null)
            {
                StylePluginHelper.FillRequiredValueForStyleArguments(styleArguments);
                userControl = new FilterStyleUserControl(ConcreteObject as TextFilterStyle, styleArguments);

                if (Children.Count == 0)
                {
                    var textStylePlugin = GisEditor.StyleManager.GetDefaultStylePlugin(StyleCategories.Label);
                    if (textStylePlugin != null)
                    {
                        var textStyle = textStylePlugin.GetDefaultStyle() as TextStyle;
                        if (textStyle != null)
                        {
                            textStyle.TextColumnName = styleArguments.ColumnNames.FirstOrDefault();
                            var styleItem = GisEditor.StyleManager.GetStyleLayerListItem(textStyle);
                            Children.Add(styleItem);
                            UpdateConcreteObject();
                        }
                    }
                }
            }
            return userControl;
        }

        protected override void UpdateConcreteObjectCore()
        {
            base.UpdateConcreteObjectCore();

            textFilterStyle.Styles.Clear();
            foreach (var style in Children.Select(i => i.ConcreteObject).OfType<Style>().Reverse())
            {
                textFilterStyle.Styles.Add(style);
            }
        }
    }
}