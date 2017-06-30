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
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ExplodeWizardSaveResultsStep : WizardStep<ExplodeWizardShareObject>
    {
        [NonSerialized]
        private ExplodeWizardSaveResultsUserControl content;

        public ExplodeWizardSaveResultsStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepTwo");
            Header = GisEditor.LanguageManager.GetStringResource("ToolsWizardSaveResults");
            Description = GisEditor.LanguageManager.GetStringResource("ToolsWizardSaveResults");

            content = new ExplodeWizardSaveResultsUserControl();
            Content = content;
        }

        protected override bool CanMoveToNextCore()
        {
            var viewModel = content.DataContext as ExplodeWizardShareObject;
            bool result = false;
            if (viewModel != null)
            {
                if ((!string.IsNullOrEmpty(viewModel.OutputPathFileName)
                        && viewModel.OutputMode == OutputMode.ToFile)
                    || (!string.IsNullOrEmpty(viewModel.TempFileName)
                        && viewModel.OutputMode == OutputMode.ToTemporary))
                    result = true;
            }
            return result;
        }

        protected override void EnterCore(ExplodeWizardShareObject parameter)
        {
            content.DataContext = parameter;
        }

        protected override bool LeaveCore(ExplodeWizardShareObject parameter)
        {
            LayerOverlay activeOverlay = GisEditor.ActiveMap.ActiveOverlay as LayerOverlay;
            if (activeOverlay != null)
            {
                var result = activeOverlay.Layers.Any(l => l.Name.Equals(parameter.TempFileName, StringComparison.Ordinal));
                if (parameter.OutputMode == OutputMode.ToTemporary && result)
                {
                    System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GeneralNameExistsMessage"), "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                    return false;
                }
            }

            return true;
        }
    }
}
