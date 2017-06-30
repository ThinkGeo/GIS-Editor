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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ExportWizardCustomizeColumnsStep : WizardStep<ExportWizardShareObject>
    {
        [NonSerialized]
        private ExportWizardCustomizeColumnsUserControl content;

        public ExportWizardCustomizeColumnsStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepTwo");
            Header = "Customize Columns";
            Description = "Customize columns to export";

            content = new ExportWizardCustomizeColumnsUserControl();
            Content = content;
        }

        protected override bool CanMoveToNextCore()
        {
            var entity = (ExportWizardShareObject)content.DataContext;
            bool hasColumn = entity.ColumnEntities.Any(c => c.IsChecked);
            return hasColumn;
        }

        protected override void EnterCore(ExportWizardShareObject parameter)
        {
            if (parameter.ColumnEntities.Count == 0)
            {
                parameter.InitColumns();
            }
            content.DataContext = parameter;
        }

        protected override bool LeaveCore(ExportWizardShareObject parameter)
        {
            bool hasColumn = parameter.ColumnEntities.Any(c => c.IsChecked);

            return hasColumn;
        }
    }
}
