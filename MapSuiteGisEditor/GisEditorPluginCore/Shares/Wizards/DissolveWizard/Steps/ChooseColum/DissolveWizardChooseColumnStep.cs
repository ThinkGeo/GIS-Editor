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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DissolveWizardChooseColumnStep : WizardStep<DissolveWizardShareObject>
    {
        [NonSerialized]
        private DissolveWizardChooseColumnUserControl content;
        private DissolveWizardChooseColumnViewModel dataContext;

        public DissolveWizardChooseColumnStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepTwo");
            Header = GisEditor.LanguageManager.GetStringResource("ToolsDissolveWizardStepTwoHeader");
            Description = GisEditor.LanguageManager.GetStringResource("ToolsDissolveWizardStepTwoHeaderDescription");
            content = new DissolveWizardChooseColumnUserControl();
            Content = content;
        }

        protected override void EnterCore(DissolveWizardShareObject parameter)
        {
            base.EnterCore(parameter);

            dataContext = new DissolveWizardChooseColumnViewModel(parameter.SelectedFeatureLayer, parameter.DissolveSelectedFeaturesOnly);
            content.DataContext = dataContext;
        }

        protected override bool LeaveCore(DissolveWizardShareObject parameter)
        {
            base.LeaveCore(parameter);
            parameter.ExtraColumns.Clear();
            parameter.MatchColumns.Clear();
            foreach (var columnName in dataContext.MatchColumns)
            {
                if (columnName.IsChecked)
                {
                    parameter.MatchColumns.Add(columnName.Value);
                }
                else
                {
                    parameter.ExtraColumns.Add(columnName.Value);
                }
            }
            return true;
        }

        protected override bool CanMoveToNextCore()
        {
            return dataContext.MatchColumns.Count(item => item.IsChecked) != 0;
        }
    }
}