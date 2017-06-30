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
using System.Windows.Controls;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ClassBreakStyleItem : InternalStyleItem
    {
        [NonSerialized]
        private ClassBreakStyleUserControl userControl;

        [NonSerialized]
        private ClassBreakStyle classBreakStyle;

        public ClassBreakStyleItem(ClassBreakStyle style)
            : base(style)
        {
            classBreakStyle = style;
            if (classBreakStyle != null)
            {
                foreach (var classBreak in classBreakStyle.ClassBreaks)
                {
                    StyleLayerListItem classBreakItem = new ClassBreakSubItem(classBreak, classBreakStyle.ColumnName);
                    Children.Add(classBreakItem);
                }
            }
        }

        protected override StyleUserControl CreateUI(StyleBuilderArguments styleArguments)
        {
            if (userControl == null)
            {
                StylePluginHelper.FillRequiredValueForStyleArguments(styleArguments);
                userControl = new ClassBreakStyleUserControl(ConcreteObject as ClassBreakStyle, styleArguments);
            }
            return userControl;
        }

        protected override void UpdateStyleItemCore()
        {
            base.UpdateStyleItemCore();
            if (classBreakStyle != null)
            {
                var deletedClassBreaks = Children.Where(i => !classBreakStyle.ClassBreaks.Any(c => c == i.ConcreteObject)).ToList();
                foreach (var deletedClassBreak in deletedClassBreaks)
                {
                    if (Children.Contains(deletedClassBreak))
                    {
                        Children.Remove(deletedClassBreak);
                    }
                }

                var addedClassBreaks = classBreakStyle.ClassBreaks.Where(c => !Children.Any(i => i.ConcreteObject == c)).ToList();
                foreach (var addedClassBreak in addedClassBreaks)
                {
                    StyleLayerListItem classBreakItem = new ClassBreakSubItem(addedClassBreak, classBreakStyle.ColumnName);
                    Children.Add(classBreakItem);
                }
            }
        }

        protected override void UpdateUICore(UserControl styleItemUI)
        {
            base.UpdateUICore(styleItemUI);

            var viewModel = styleItemUI.DataContext as ClassBreakStyleViewModel;
            if (viewModel != null)
            {
                viewModel.ClassBreakItems.Clear();
                foreach (var classBreak in Children.Select(i => i.ConcreteObject).OfType<ClassBreak>())
                {
                    ClassBreakItem classBreakItem = new ClassBreakItem();
                    classBreakItem.PropertyChanged += (s, e) => { if (e.PropertyName.Equals("StartingValue")) UpdateStyleItem(); };
                    classBreakItem.ClassBreak = classBreak;
                    classBreakItem.StartingValue = classBreak.Value.ToString(CultureInfo.InvariantCulture);
                    classBreakItem.Image = StyleHelper.GetImageFromStyle(classBreak.CustomStyles);
                    viewModel.ClassBreakItems.Add(classBreakItem);
                }
            }
        }

        protected override void UpdateConcreteObjectCore()
        {
            base.UpdateConcreteObjectCore();

            classBreakStyle.ClassBreaks.Clear();
            foreach (var classBreak in Children.Select(i => i.ConcreteObject).OfType<ClassBreak>())
            {
                classBreakStyle.ClassBreaks.Add(classBreak);
            }
        }
    }
}