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


using System.Linq;
using System.Reflection;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    internal class ComponentStyleItem : StyleLayerListItem
    {
        public ComponentStyleItem(CompositeStyle componentStyle)
            : base(componentStyle)
        {
            CanRename = true;
            CanAddInnerStyle = true;
            foreach (var innerStyle in componentStyle.Styles.Reverse())
            {
                var innerStyleItem = GisEditor.StyleManager.GetStyleLayerListItem(innerStyle);
                Children.Add(innerStyleItem);
            }
        }

        protected override bool CanContainStyleItemCore(StyleLayerListItem styleItem)
        {
            return styleItem.ConcreteObject is Style;
        }

        protected override void UpdateConcreteObjectCore()
        {
            base.UpdateConcreteObjectCore();
            CompositeStyle componentStyle = (CompositeStyle)ConcreteObject;
            componentStyle.Styles.Clear();
            foreach (var style in Children.Select(i => i.ConcreteObject).OfType<Style>().Reverse())
            {
                componentStyle.Styles.Add(style);
            }
        }
    }
}