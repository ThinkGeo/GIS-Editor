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
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ClassBreakStylePlugin : StylePlugin
    {
        public ClassBreakStylePlugin()
            : base()
        {
            Name = GisEditor.LanguageManager.GetStringResource("ClassBreakStylePluginName");
            Description = GisEditor.LanguageManager.GetStringResource("ClassBreakStylePluginDescription");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_classbreakarea.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_classbreakarea.png", UriKind.RelativeOrAbsolute));
            Index = StylePluginOrder.ClassBreakStyle;
            StyleCategories = StyleCategories.Composite;
            RequireColumnNames = true;
        }

        protected override Style GetDefaultStyleCore()
        {
            return new ClassBreakStyle("", BreakValueInclusion.ExcludeValue);
        }

        //protected override StyleEditResult EditStyleCore(Style style, StyleArguments arguments)
        //{
        //    return StylePluginHelper.CustomizeStyle<ClassBreakStyle>(style, arguments);
        //}

        protected override StyleLayerListItem GetStyleLayerListItemCore(Style style)
        {
            var classBreakStyle = style as ClassBreakStyle;
            if (classBreakStyle != null) return new ClassBreakStyleItem(classBreakStyle);
            return null;
        }
    }
}