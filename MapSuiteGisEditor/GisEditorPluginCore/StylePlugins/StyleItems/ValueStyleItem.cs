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
using System.Windows.Controls;
using System.ComponentModel;
using System.Collections.Generic;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ValueStyleItem : InternalStyleItem
    {
        [NonSerialized]
        private ValueStyleUserControl userControl;

        [NonSerialized]
        private ValueStyle valueStyle;

        public ValueStyleItem(ValueStyle style)
            : base(style)
        {
            valueStyle = style;
            if (valueStyle != null)
            {
                foreach (var valueItem in valueStyle.ValueItems)
                {
                    ValueSubItem valueStyleItem = new ValueSubItem(valueItem, valueStyle.ColumnName);
                    Children.Add(valueStyleItem);
                }
            }
        }

        protected override StyleUserControl CreateUI(StyleBuilderArguments styleArguments)
        {
            if (userControl == null)
            {
                StylePluginHelper.FillRequiredValueForStyleArguments(styleArguments);
                userControl = new ValueStyleUserControl(ConcreteObject as ValueStyle, styleArguments);
            }
            return userControl;
        }

        protected override void UpdateStyleItemCore()
        {
            base.UpdateStyleItemCore();
            if (valueStyle != null)
            {
                var deletedValueItems = Children.Where(i => !valueStyle.ValueItems.Any(c => c == i.ConcreteObject)).ToList();
                foreach (var deletedValueItem in deletedValueItems)
                {
                    if (Children.Contains(deletedValueItem))
                    {
                        Children.Remove(deletedValueItem);
                    }
                }

                var addedValueItems = valueStyle.ValueItems.Where(c => !Children.Any(i => i.ConcreteObject == c)).ToList();
                foreach (var addedValueItem in addedValueItems)
                {
                    StyleLayerListItem valueItem = new ValueSubItem(addedValueItem, valueStyle.ColumnName);
                    Children.Add(valueItem);
                }
            }
        }

        protected override void UpdateConcreteObjectCore()
        {
            base.UpdateConcreteObjectCore();

            valueStyle.ValueItems.Clear();
            foreach (var item in Children.Select(i => i.ConcreteObject).OfType<ValueItem>())
            {
                valueStyle.ValueItems.Add(item);
            }
        }

        protected override void UpdateUICore(UserControl styleItemUI)
        {
            base.UpdateUICore(styleItemUI);

            var viewModel = styleItemUI.DataContext as ValueStyleViewModel;

            if (viewModel != null && viewModel.ValueItems.Count != Children.Count)
            {
                List<ValueItemEntity> temps = viewModel.ValueItems.ToList();
                viewModel.ValueItems.Clear();
                foreach (var subItem in Children.OfType<StyleLayerListItem>())
                {
                    var valueItem = subItem.ConcreteObject as ValueItem;
                    var item = new ValueItemEntity();
                    item.PropertyChanged += Item_PropertyChanged;
                    item.ValueItem = valueItem;
                    item.MatchedValue = valueItem.Value;
                    item.Image = StyleHelper.GetImageFromStyle(valueItem.CustomStyles);
                    ValueItemEntity temp = temps.FirstOrDefault(v => v.ValueItem == valueItem);
                    if (temp != null)
                    {
                        item.Count = temp.Count;
                    }
                    viewModel.ValueItems.Add(item);
                }
            }
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("MatchedValue"))
            {
                UpdateStyleItem();
            }
        }
    }
}