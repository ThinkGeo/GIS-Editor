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
    public class BlendWizardSaveResultsStep : WizardStep<BlendWizardShareObject>
    {
        [NonSerialized]
        private BlendWizardSaveResultsStepUserControl content;

        public BlendWizardSaveResultsStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepFour");
            Header = GisEditor.LanguageManager.GetStringResource("ToolsWizardSaveResults");
            Description = GisEditor.LanguageManager.GetStringResource("ToolsBlendWizardStepFourHeaderDescription");

            content = new BlendWizardSaveResultsStepUserControl();
            Content = content;
        }

        protected override void EnterCore(BlendWizardShareObject parameter)
        {
            base.EnterCore(parameter);
            content.DataContext = parameter;
            string layerName = parameter.SelectedAreaLayers[0].Name;
            //parameter.TempFileName = FolderHelper.GetWizardTempFileName(layerName, parameter.WizardName);
        }

        protected override bool CanMoveToNextCore()
        {
            var viewModel = content.DataContext as BlendWizardShareObject;
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

        protected override bool LeaveCore(BlendWizardShareObject parameter)
        {
            var result = (GisEditor.ActiveMap.ActiveOverlay as LayerOverlay).Layers.Any(l => l.Name == parameter.TempFileName);
            if (parameter.OutputMode == OutputMode.ToTemporary && result)
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GeneralNameExistsMessage"), "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
