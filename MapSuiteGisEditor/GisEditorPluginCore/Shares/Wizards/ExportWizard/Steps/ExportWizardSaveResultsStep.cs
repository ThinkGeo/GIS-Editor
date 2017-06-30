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
    public class ExportWizardSaveResultsStep : WizardStep<ExportWizardShareObject>
    {
        private ExportWizardShareObject shareObject;
        [NonSerialized]
        private ExportWizardSaveResultsUserControl content;

        public ExportWizardSaveResultsStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepThree");
            Header = GisEditor.LanguageManager.GetStringResource("DataJoinSaveResultsStepSaveHeader");
            Description = GisEditor.LanguageManager.GetStringResource("DataJoinSaveResultsStepSaveHeader");

            content = new ExportWizardSaveResultsUserControl();
            Content = content;
        }

        protected override bool CanMoveToNextCore()
        {
            var viewModel = content.DataContext as ExportWizardShareObject;
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

        protected override void EnterCore(ExportWizardShareObject parameter)
        {
            content.DataContext = parameter;
            shareObject = parameter;
            switch (parameter.ExportMode)
            {
                case ExportMode.ExportSelectedFeatures:
                default:
                    //parameter.TempFileName = FolderHelper.GetWizardTempFileName(parameter.SelectedFeatureLayer.Name, parameter.WizardName);
                    break;
                case ExportMode.ExportMeasuredFeatures:
                    //parameter.TempFileName = FolderHelper.GetWizardTempFileName(parameter.SelectedMeasurementType.ToString(), parameter.WizardName);
                    break;
            }
        }

        protected override bool LeaveCore(ExportWizardShareObject parameter)
        {
            var result = GisEditor.ActiveMap.ActiveOverlay != null && GisEditor.ActiveMap.ActiveOverlay is LayerOverlay;
            result = result && (GisEditor.ActiveMap.ActiveOverlay as LayerOverlay).Layers.Any(l => l.Name == parameter.TempFileName);
            if (parameter.OutputMode == OutputMode.ToTemporary && result)
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GeneralNameExistsMessage"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
