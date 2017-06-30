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
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ChooseColumnStep : WizardStep<SplitWizardShareObject>
    {
        [NonSerialized]
        private ChooseColumnUserControl content;

        public ChooseColumnStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepTwo");
            Header = GisEditor.LanguageManager.GetStringResource("ToolsSplitWizardStepTwoSelecDataColumn");
            Description = GisEditor.LanguageManager.GetStringResource("ToolsSplitWizardStepTwoSelecDataColumnDescription");

            content = new ChooseColumnUserControl();
            Content = content;
        }

        protected override void EnterCore(SplitWizardShareObject parameter)
        {
            base.EnterCore(parameter);
            content.DataContext = parameter;

            if (parameter.SelectedLayerToSplit != null)
            {
                parameter.SelectedLayerToSplit.SafeProcess(() =>
                {
                    parameter.ColumnsInSelectedLayer.Clear();
                    foreach (var column in parameter.SelectedLayerToSplit.QueryTools.GetColumns())
                    {
                        string alias = parameter.SelectedLayerToSplit.FeatureSource.GetColumnAlias(column.ColumnName);
                        parameter.ColumnsInSelectedLayer.Add(column);
                        parameter.ColumnNamesInSelectedLayer[column.ColumnName] = alias;
                    }

                    parameter.SelectedFeatureSourceColumn = parameter.SelectedFeatureSourceColumn ?? parameter.ColumnsInSelectedLayer.FirstOrDefault();
                });
            }
        }

        protected override bool LeaveCore(SplitWizardShareObject parameter)
        {
            base.LeaveCore(parameter);
            if (Parent.MoveDirection == MoveDirection.Next)
            {
                parameter.GenerateExportConfigAsync();
            }
            return true;
        }
    }
}