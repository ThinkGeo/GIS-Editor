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
    public class BuildIndexWizardViewModel : WizardViewModel<BuildIndexViewModel>
    {
        public BuildIndexWizardViewModel()
        {
            Title = GisEditor.LanguageManager.GetStringResource("BuildIndexWizardViewModelBuildindexTitle");
            HelpKey = "BuildIndexWizardHelp";
            TargetObject = new BuildIndexViewModel();
            Add(new SelectFilesStep());
            Add(new BuildingIndexStep());
        }

        protected override void ExecuteCore()
        {
            base.ExecuteCore();
            try
            {
                TargetObject.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(TargetObject_PropertyChanged);
                TargetObject.Execute();
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            finally
            {
                State = BatchTaskState.Finished;
            }
        }

        private void TargetObject_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsBusy") && TargetObject.IsBusy)
            {
                State = BatchTaskState.Running;
            }
        }
    }
}