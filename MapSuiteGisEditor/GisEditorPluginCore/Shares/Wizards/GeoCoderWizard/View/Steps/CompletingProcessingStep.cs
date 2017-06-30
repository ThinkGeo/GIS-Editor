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
using System.ComponentModel;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class CompletingProcessingStep : WizardStep<GeocoderWizardSharedObject>
    {
        [NonSerialized]
        private StepOfCompletingProcessing content;
        private GeocoderWizardSharedObject sharedObject;
        public CompletingProcessingStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepThree");
            Header = GisEditor.LanguageManager.GetStringResource("CompletingProcessingStepHeader");
            Description = GisEditor.LanguageManager.GetStringResource("CompletingProcessingStepHeader"); 
        }

        protected override bool CanMoveToNextCore()
        {
            return sharedObject.ErrorTableVisibility == Visibility.Visible;
        }

        protected override void EnterCore(GeocoderWizardSharedObject parameter)
        {
            sharedObject = parameter;
            base.EnterCore(parameter);
            if (content == null)
            {
                content = new StepOfCompletingProcessing(parameter);
                content.ProcessCompleted += new AsyncCompletedEventHandler(content_ProcessCompleted);
                Content = content;
            }
            if (Parent.MoveDirection == MoveDirection.Back)
            {
                Parent.MoveBack();
            }
        }

        private void content_ProcessCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Parent.MoveNext();
        }
    }
}
