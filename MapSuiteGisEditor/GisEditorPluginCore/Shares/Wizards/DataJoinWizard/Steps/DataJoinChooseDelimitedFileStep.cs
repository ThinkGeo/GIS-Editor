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



using System.IO;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class DataJoinChooseDelimitedFileStep : WizardStep<DataJoinWizardShareObject>
    {
        private DataJoinChooseDelimitedFileUserControl content;

        public DataJoinChooseDelimitedFileStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepTwo");
            Header = GisEditor.LanguageManager.GetStringResource("DataJoinChooseDelimitedFileStepChooseHeader");
            Description = GisEditor.LanguageManager.GetStringResource("DataJoinChooseDelimitedFileStepChooseHeader"); 
            content = new DataJoinChooseDelimitedFileUserControl();
            Content = content;
        }

        protected override void EnterCore(DataJoinWizardShareObject parameter)
        {
            content.DataContext = parameter;
        }

        protected override bool CanMoveToNextCore()
        {
            return !string.IsNullOrEmpty((content.DataContext as DataJoinWizardShareObject).SelectedDataFilePath);
        }

        protected override bool LeaveCore(DataJoinWizardShareObject parameter)
        {
            var entity = content.DataContext as DataJoinWizardShareObject;
            if (entity != null)
            {
                if (!string.IsNullOrEmpty(entity.SelectedDataFilePath))
                {
                    if (entity.DataJoinAdapter is CsvDataJoinAdapter)
                    {
                        using (StreamReader sr = new StreamReader(entity.SelectedDataFilePath))
                        {
                            string line = sr.ReadLine();
                            if (!line.Contains(entity.SelectedDelimiter.Value))
                            {
                                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("DataJoinChooseDelimitedFileStepdelimiterInvalidMessage"), "Invalid delimiter");
                                return false;
                            }
                            else
                                return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
