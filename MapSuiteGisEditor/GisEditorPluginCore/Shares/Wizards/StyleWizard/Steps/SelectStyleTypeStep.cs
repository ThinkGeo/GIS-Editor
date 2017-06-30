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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class SelectStyleTypeStep : WizardStep<StyleWizardSharedObject>
    {
        [NonSerialized]
        private UserControl content;
        private StyleWizardSharedObject sharedObject;

        public SelectStyleTypeStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepTwo");
            Description = GisEditor.LanguageManager.GetStringResource("SelectStyleTypeStepDesc");
            Header = GisEditor.LanguageManager.GetStringResource("SelectStyleTypeStepHeader");
        }

        protected override void EnterCore(StyleWizardSharedObject parameter)
        {
            sharedObject = parameter;
            base.EnterCore(parameter);

            if (parameter.SelectedStyleCategory is LibraryStyleCheckableItemModel)
            {
                if (Parent.MoveDirection == MoveDirection.Next)
                {
                    Parent.MoveNext();
                    Parent.FinishCommand.Execute(null);
                }
                else if (Parent.MoveDirection == MoveDirection.Back)
                {
                    Parent.MoveBack();
                }
            }
            else if (parameter.SelectedStyleCategory.Name.Equals("Import a Style"))
            {
                content = new SelectStyleFileUserControl();
            }
            else
            {
                if (parameter.SelectedStyleCategory.Name.Contains("Label"))
                {
                    parameter.AllStyleSources[parameter.SelectedStyleCategory.Name] = new TextStylesModel();
                }

                parameter.StyleSources = parameter.AllStyleSources[parameter.SelectedStyleCategory.Name];
                parameter.SelectedStyleCategory = parameter.StyleSources.FirstOrDefault();
                content = new SelectStyleTypeUserControl();
            }

            if (content != null)
            {
                content.DataContext = parameter;
                Content = content;
            }
        }

        protected override bool LeaveCore(StyleWizardSharedObject parameter)
        {
            base.LeaveCore(parameter);
            if (content is SelectStyleTypeUserControl)
            {
                string providerName = parameter.SelectedStyleCategory.ProviderName;

                string providerTypeFullName = String.Format(CultureInfo.InvariantCulture, "ThinkGeo.MapSuite.GisEditor.Plugins.{0}", providerName);

                parameter.ResultStylePlugin = GisEditor.StyleManager.GetActiveStylePlugins<StylePlugin>()
                        .FirstOrDefault(p => p.GetType().FullName
                            .Equals(providerTypeFullName, StringComparison.Ordinal));
            }
            return true;
        }

        protected override bool CanMoveToNextCore()
        {
            bool canMoveToNext = base.CanMoveToNextCore();
            if (content is SelectStyleFileUserControl)
            {
                SelectStyleFileUserControl selectStyleFileUserControl = (SelectStyleFileUserControl)content;
                if (String.IsNullOrEmpty(sharedObject.StyleFileFullName))
                {
                    canMoveToNext = false;
                }
            }
            return canMoveToNext;
        }
    }
}
