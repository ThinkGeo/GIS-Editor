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
    public class BlendWizardChooseLayersStep : WizardStep<BlendWizardShareObject>
    {
        [NonSerialized]
        private BlendWizardChooseLayersUserControl content;

        public BlendWizardChooseLayersStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepOne");
            Header = GisEditor.LanguageManager.GetStringResource("ToolsBlendWizardStepOneHeader");
            Description = GisEditor.LanguageManager.GetStringResource("ToolsBlendWizardStepOneHeaderDescription");
        }

        protected override void EnterCore(BlendWizardShareObject parameter)
        {
            base.EnterCore(parameter);
            content = new BlendWizardChooseLayersUserControl(parameter);
            Content = content;
            Content.DataContext = parameter;
            parameter.ColumnsToInclude.Clear();
        }

        protected override bool LeaveCore(BlendWizardShareObject parameter)
        {
            base.LeaveCore(parameter);
            parameter.SelectedAreaLayers.Clear();

            var selectedLayers = content.SelectedLayers.ToArray();
            foreach (var layer in selectedLayers)
            {
                parameter.SelectedAreaLayers.Add(layer);
            }
            return true;
        }

        protected override bool CanMoveToNextCore()
        {
            return content.SelectedLayers.Count() >= 2;
        }
    }
}
