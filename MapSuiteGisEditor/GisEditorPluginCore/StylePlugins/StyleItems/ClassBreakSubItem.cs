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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ClassBreakSubItem : StyleLayerListItem
    {
        private static readonly string classBreakNameTemplate = "Starting Value is \"{0}\"";

        [Obfuscation]
        private string columnName;

        [NonSerialized]
        private ClassBreak classBreak;

        public ClassBreakSubItem(ClassBreak classBreak, string columnName)
            : base(classBreak)
        {
            this.CanAddInnerStyle = true;
            this.classBreak = classBreak;
            this.columnName = columnName;
            this.InitializeStyleItems(classBreak);
        }

        private void InitializeStyleItems(ClassBreak classBreak)
        {
            foreach (var customStyle in classBreak.CustomStyles.Reverse())
            {
                var styleItem = GisEditor.StyleManager.GetStyleLayerListItem(customStyle);
                Children.Add(styleItem);
            }
        }

        protected override bool CanContainStyleItemCore(StyleLayerListItem styleItem)
        {
            return styleItem.ConcreteObject is Style;
        }

        protected override void UpdateConcreteObjectCore()
        {
            base.UpdateConcreteObjectCore();

            classBreak.CustomStyles.Clear();
            foreach (var style in Children.Select(i => i.ConcreteObject).OfType<Style>().Reverse())
            {
                classBreak.CustomStyles.Add(style);
            }
        }

        protected override void UpdateUICore(UserControl styleItemUI)
        {
            var viewModel = styleItemUI.DataContext as ClassBreakStyleViewModel;
            if (viewModel != null)
            {
                var currentClassBreakItem = viewModel.ClassBreakItems.FirstOrDefault(c => c.ClassBreak == ConcreteObject);
                viewModel.SelectedClassBreakItem = currentClassBreakItem;

                currentClassBreakItem.Image = StyleHelper.GetImageFromStyle(currentClassBreakItem.ClassBreak.CustomStyles);
            }
        }

        protected override string NameCore
        {
            get
            {
                base.NameCore = GetClassBreakStyleName(classBreak.Value);
                return base.NameCore;
            }
            set
            {
                base.NameCore = value;
            }
        }

        public static string GetClassBreakStyleName(double value)
        {
            return string.Format(CultureInfo.InvariantCulture, classBreakNameTemplate, value);
        }

        protected override StyleUserControl GetUICore(StyleBuilderArguments styleArguments)
        {
            var parent = Parent as StyleLayerListItem;
            return parent?.GetUI(styleArguments);
        }
    }
}