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
    public class DissolveWizardSaveResultStep : WizardStep<DissolveWizardShareObject>
    {
        [NonSerialized]
        private DissolveWizardSaveResultUserControl content;
        private DissolveWizardShareObject shareObject;

        public DissolveWizardSaveResultStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepFour");
            Header = GisEditor.LanguageManager.GetStringResource("ToolsWizardSaveResults");
            Description = GisEditor.LanguageManager.GetStringResource("ToolsDissolveWizardStepFourSaveResults");
            content = new DissolveWizardSaveResultUserControl();
            Content = content;
        }

        protected override void EnterCore(DissolveWizardShareObject parameter)
        {
            content.DataContext = parameter;
            base.EnterCore(parameter);
            shareObject = parameter;
            //parameter.TempFileName = FolderHelper.GetWizardTempFileName(parameter.SelectedFeatureLayer.Name, parameter.WizardName);
        }

        protected override bool CanMoveToNextCore()
        {
            var viewModel = content.DataContext as DissolveWizardShareObject;
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

        protected override bool LeaveCore(DissolveWizardShareObject parameter)
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
