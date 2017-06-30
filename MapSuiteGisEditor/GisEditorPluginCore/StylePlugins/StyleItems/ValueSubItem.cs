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
using System.Reflection;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ValueSubItem : StyleLayerListItem
    {
        [Obfuscation]
        private string columnName;
        [NonSerialized]
        private ValueItem valueItem;

        public ValueSubItem(ValueItem valueItem, string columnName)
            : base(valueItem)
        {
            this.CanAddInnerStyle = true;
            this.valueItem = valueItem;
            this.columnName = columnName;

            foreach (var customStyle in valueItem.CustomStyles.Reverse())
            {
                StyleLayerListItem styleItem = GisEditor.StyleManager.GetStyleLayerListItem(customStyle);
                Children.Add(styleItem);
            }
        }

        protected override string NameCore
        {
            get
            {
                base.NameCore = ValueStyleViewModel.GetValueItemStyleName(columnName, valueItem.Value);
                return base.NameCore;
            }
            set
            {
                base.NameCore = value;
            }
        }

        protected override bool CanContainStyleItemCore(StyleLayerListItem styleItem)
        {
            return styleItem.ConcreteObject is Style;
        }

        protected override void UpdateConcreteObjectCore()
        {
            base.UpdateConcreteObjectCore();

            valueItem.CustomStyles.Clear();
            foreach (var style in Children.Select(i => i.ConcreteObject).OfType<Style>().Reverse())
            {
                valueItem.CustomStyles.Add(style);
            }
        }

        protected override void UpdateUICore(UserControl styleItemUI)
        {
            var viewModel = styleItemUI.DataContext as ValueStyleViewModel;
            if (viewModel != null)
            {
                var currentValueItem = viewModel.ValueItems.FirstOrDefault(c => c.ValueItem == ConcreteObject);
                if (currentValueItem != null)
                {
                    currentValueItem.Image = StyleHelper.GetImageFromStyle(currentValueItem.ValueItem.CustomStyles);
                }

                viewModel.SelectedValueItem = viewModel.ValueItems.Where(v => v.ValueItem == (ValueItem)ConcreteObject).FirstOrDefault();
            }
        }

        protected override StyleUserControl GetUICore(StyleBuilderArguments styleArguments)
        {
            var parent = Parent as StyleLayerListItem;
            if (parent != null) return parent.GetUI(styleArguments);
            else return null;
        }
    }
}
