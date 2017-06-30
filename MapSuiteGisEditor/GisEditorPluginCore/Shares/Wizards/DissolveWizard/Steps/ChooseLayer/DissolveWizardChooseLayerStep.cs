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
    public class DissolveWizardChooseLayerStep : WizardStep<DissolveWizardShareObject>
    {
        [NonSerialized]
        private DissolveWizardChooseLayerUserControl content;
        private DissolveWizardChooseLayerViewModel dataContext;

        public DissolveWizardChooseLayerStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepOne");
            Header = GisEditor.LanguageManager.GetStringResource("ToolsDissolveWizardStepOneHeader");
            Description = GisEditor.LanguageManager.GetStringResource("ToolsDissolveWizardStepOneHeaderDescription");
            content = new DissolveWizardChooseLayerUserControl();
        }

        protected override void EnterCore(DissolveWizardShareObject parameter)
        {
            base.EnterCore(parameter);

            dataContext = new DissolveWizardChooseLayerViewModel();
            content.DataContext = dataContext;
            Content = content;

            if (!dataContext.HasFeatureSelected)
            {
                dataContext.DissolveSelectedFeaturesOnly = false;
            }

            foreach (var layer in GisEditor.ActiveMap.GetFeatureLayers(true))
            {
                dataContext.FeatureLayers.Add(layer);
            }

            if (dataContext.FeatureLayers.Count > 0)
            {
                dataContext.SelectedFeatureLayer = dataContext.FeatureLayers[0];
            }
        }

        protected override bool LeaveCore(DissolveWizardShareObject parameter)
        {
            base.LeaveCore(parameter);
            parameter.SelectedFeatureLayer = dataContext.SelectedFeatureLayer;
            parameter.DissolveSelectedFeaturesOnly = dataContext.DissolveSelectedFeaturesOnly;
            return true;
        }

        protected override bool CanMoveToNextCore()
        {
            return dataContext.SelectedFeatureLayer != null;
        }
    }
}