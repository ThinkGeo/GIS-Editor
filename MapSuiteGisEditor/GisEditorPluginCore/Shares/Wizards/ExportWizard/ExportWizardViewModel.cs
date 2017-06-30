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
using System.IO;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ExportWizardViewModel : WizardViewModel<ExportWizardShareObject>
    {
        private ExportMode mode;

        public ExportWizardViewModel(ExportMode mode, TrackInteractiveOverlay trackOverlay)
        {
            this.mode = mode;
            this.Title = GisEditor.LanguageManager.GetStringResource("ExportWizardViewModelTitle");
            HelpKey = "ExportWizardHelp";
            this.TargetObject = new ExportWizardShareObject(mode, trackOverlay);

            if (mode == ExportMode.ExportSelectedFeatures)
            {
                Add(new ExportWizardChooseLayerStep());
                Add(new ExportWizardCustomizeColumnsStep());
                Add(new ExportWizardSaveResultsStep());
            }
            else if (mode == ExportMode.ExportMeasuredFeatures)
            {
                Add(new ChooseMeasurementTypeStep());
                Add(new ExportWizardSaveResultsStep() { Title = GisEditor.LanguageManager.GetStringResource("GeneralStepTwo") });
            }
        }

        public override string TaskName
        {
            get
            {
                if (mode == ExportMode.ExportMeasuredFeatures)
                {
                    return string.Format(CultureInfo.InvariantCulture, "Export measured features to {0}.", Path.GetFileName(TargetObject.OutputPathFileName));
                }
                else
                {
                    return string.Format(CultureInfo.InvariantCulture, "Export selected features to {0}.", Path.GetFileName(TargetObject.OutputPathFileName));
                }
            }
        }
    }
}