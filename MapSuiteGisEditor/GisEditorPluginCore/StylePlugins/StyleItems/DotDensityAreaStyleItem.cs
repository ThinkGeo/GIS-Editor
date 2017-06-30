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
    public class DotDensityAreaStyleItem : InternalStyleItem
    {
        [NonSerialized]
        private DotDensityAreaStyleUserControl userControl;

        [NonSerialized]
        private DotDensityStyle dotDensityStyle;

        public DotDensityAreaStyleItem(Style style)
            : base(style)
        {
            CanAddInnerStyle = true;
            dotDensityStyle = style as DotDensityStyle;

            if (dotDensityStyle != null && dotDensityStyle.CustomPointStyle != null)
            {
                foreach (var pointStyle in dotDensityStyle.CustomPointStyle.CustomPointStyles.Reverse())
                {
                    pointStyle.Name = GisEditor.StyleManager.GetStylePluginByStyle(pointStyle).Name;
                    var styleItem = GisEditor.StyleManager.GetStyleLayerListItem(pointStyle);
                    Children.Add(styleItem);
                }
            }
        }

        protected override StyleUserControl CreateUI(StyleBuilderArguments styleArguments)
        {
            return userControl ?? (userControl = new DotDensityAreaStyleUserControl(ConcreteObject as DotDensityStyle, styleArguments));
        }

        protected override StyleCategories GetRestrictStyleCategoriesCore()
        {
            return StyleCategories.Point;
        }

        protected override void UpdateConcreteObjectCore()
        {
            base.UpdateConcreteObjectCore();

            dotDensityStyle.CustomPointStyle.CustomPointStyles.Clear();
            foreach (var pointStyle in Children.Select(i => i.ConcreteObject).OfType<PointStyle>().Reverse())
            {
                pointStyle.Name = GisEditor.StyleManager.GetStylePluginByStyle(pointStyle).Name;
                dotDensityStyle.CustomPointStyle.CustomPointStyles.Add(pointStyle);
            }
        }
    }
}
