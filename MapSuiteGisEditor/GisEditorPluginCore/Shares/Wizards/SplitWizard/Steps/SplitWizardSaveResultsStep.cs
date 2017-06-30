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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class SplitWizardSaveResultsStep : WizardStep<SplitWizardShareObject>
    {
        [NonSerialized]
        private SplitWizardSaveResultsUserControl content;

        public SplitWizardSaveResultsStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepFour");
            Header = GisEditor.LanguageManager.GetStringResource("ToolsWizardSaveResults");
            Description = GisEditor.LanguageManager.GetStringResource("ToolsSplitWizardStepFourSaveThResults");

            content = new SplitWizardSaveResultsUserControl();
            Content = content;
        }

        protected override bool CanMoveToNextCore()
        {
            var viewModel = content.DataContext as SplitWizardShareObject;
            bool result = false;
            if (viewModel != null)
            {
                if (viewModel.OutputMode == OutputMode.ToTemporary && viewModel.IsTempFileChecked)
                    result = true;
                else if (!string.IsNullOrEmpty(viewModel.OutputPath)
                    && viewModel.OutputMode == OutputMode.ToFile && viewModel.IsOutputChecked)
                    result = true;
            }
            return result;
        }

        protected override void EnterCore(SplitWizardShareObject parameter)
        {
            content.DataContext = parameter;
        }
    }
}