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
    public class SelectingAddressFileStep : WizardStep<GeocoderWizardSharedObject>
    {
        [NonSerialized]
        private StepOfSelectingAddressFile content;
        private GeocoderWizardSharedObject sharedObject;

        public SelectingAddressFileStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepOne");
            Header = GisEditor.LanguageManager.GetStringResource("SelectingAddressFileStepHeader");
            Description = GisEditor.LanguageManager.GetStringResource("SelectingAddressFileStepHeader");
        }

        protected override void EnterCore(GeocoderWizardSharedObject parameter)
        {
            base.EnterCore(parameter);
            sharedObject = parameter;
            if (content == null)
            {
                content = new StepOfSelectingAddressFile(parameter);
                Content = content;
            }
        }

        protected override bool CanMoveToNextCore()
        {
            return !String.IsNullOrEmpty(sharedObject.InputFilePath);
        }
    }
}
