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
using System.Collections.Generic;
using System.Data;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

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

        protected override bool LeaveCore(DataJoinWizardShareObject parameter)
        {
            return CheckHasMatchedValue();
        }

        private bool CheckHasMatchedValue()
        {
            var entity = content.DataContext as DataJoinWizardShareObject;
            if (entity != null)
            {
                List<Feature> features = new List<Feature>();

                entity.SelectedFeatureLayer.SafeProcess(() =>
                 {
                     if (entity.HasSelectedFeatures && entity.OnlyUseSelectedFeatures && GisEditor.SelectionManager.GetSelectionOverlay() != null)
                     {
                         features = GisEditor.SelectionManager.GetSelectionOverlay().HighlightFeatureLayer.InternalFeatures
                                      .Where(f => f.Tag != null && f.Tag == entity.SelectedFeatureLayer).ToList();
                     }
                     else
                     {
                         features = entity.SelectedFeatureLayer.FeatureSource.GetAllFeatures(entity.SelectedFeatureLayer.FeatureSource.GetDistinctColumnNames()).ToList();
                     }
                 });

                var csvDataTable = entity.ReadDataToDataGrid(entity.SelectedDataFilePath, entity.SelectedDelimiter.Value);
                var csvFeatureRows = csvDataTable.Rows;

                bool hasMatchedValue = false;
                foreach (var condition in entity.MatchConditions)
                {
                    foreach (var feature in features)
                    {
                        foreach (var dataRow in csvFeatureRows.Cast<DataRow>())
                        {
                            if (dataRow[condition.SelectedDelimitedColumn.ColumnName].ToString() == feature.ColumnValues[condition.SelectedLayerColumn.ColumnName])
                            {
                                hasMatchedValue = true;
                                break;
                            }
                        }
                    }
                }
                if (!hasMatchedValue)
                {
                    if (System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("NoMatchValueJoinText"),
                        GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"), System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    {
                        return true;
                    }
                    else return false;
                }
                else return true;
            }
            else return false;
        }
    }
}
