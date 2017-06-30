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
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal partial class LayerListMenuItemHelper
    {
        private static void RemoveSubStyle()
        {
            if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
            Style style = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as Style;
            object parentActualObject = GisEditor.LayerListManager.SelectedLayerListItem.Parent.ConcreteObject;
            if (style != null)
            {
                if (parentActualObject is ValueStyle)
                {
                    var valueStyle = (ValueStyle)parentActualObject;
                    var resultValueItem = valueStyle.ValueItems.Where(valueItem => valueItem.CustomStyles.Contains(style)).FirstOrDefault();
                    valueStyle.ValueItems.Remove(resultValueItem);
                }
                else if (parentActualObject is ClassBreakStyle)
                {
                    var classBreakStyle = (ClassBreakStyle)parentActualObject;
                    var resultClassBreak = classBreakStyle.ClassBreaks.Where(classBreak => classBreak.CustomStyles.Contains(style)).FirstOrDefault();
                    classBreakStyle.ClassBreaks.Remove(resultClassBreak);
                }
                else if (parentActualObject is RegexStyle)
                {
                    var regexStyle = (RegexStyle)parentActualObject;
                    var resultRegexItem = regexStyle.RegexItems.Where(regexItem => regexItem.CustomStyles.Contains(style)).FirstOrDefault();
                    regexStyle.RegexItems.Remove(resultRegexItem);
                }

                LayerListHelper.RefreshCache();
                GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(GisEditor.LayerListManager.SelectedLayerListItem, RefreshArgsDescription.RemoveSubStyleDescription));
            }
        }
    }
}