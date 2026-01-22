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


using System.Linq;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class DataJoinSelectColumnsStep : WizardStep<DataJoinWizardShareObject>
    {
        private DataJoinSelectColumnsUserControl content;

        public DataJoinSelectColumnsStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepThree");
            Header = GisEditor.LanguageManager.GetStringResource("DataJoinSelectColumnsStepSelectHeader");
            Description = GisEditor.LanguageManager.GetStringResource("DataJoinSelectColumnsStepSelectHeader");
            content = new DataJoinSelectColumnsUserControl();
            Content = content;
        }

        protected override void EnterCore(DataJoinWizardShareObject parameter)
        {
            content.DataContext = parameter;
        }

        protected override bool CanMoveToNextCore()
        {
            var data = content.DataContext as DataJoinWizardShareObject;
            if (data == null || data.MatchConditions == null || data.MatchConditions.Count == 0)
            {
                return false;
            }

            return data.MatchConditions.All(condition =>
                condition.SelectedLayerColumn != null && condition.SelectedDelimitedColumn != null);
        }
    }
}
