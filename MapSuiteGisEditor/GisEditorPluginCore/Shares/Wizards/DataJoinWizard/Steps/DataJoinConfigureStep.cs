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
using System.Collections.ObjectModel;
using System.Text;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class DataJoinConfigureStep : WizardStep<DataJoinWizardShareObject>
    {
        private DataJoinConfigureUserControl content;

        public DataJoinConfigureStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepFour");
            Header = GisEditor.LanguageManager.GetStringResource("DataJoinConfigureStepConfigureHeader");
            Description = GisEditor.LanguageManager.GetStringResource("DataJoinConfigureStepConfigureHeader"); 
            content = new DataJoinConfigureUserControl();
            Content = content;
        }

        protected override void EnterCore(DataJoinWizardShareObject parameter)
        {
            content.DataContext = parameter;
        }

        protected override bool CanMoveToNextCore()
        {
            return ((content.DataContext as DataJoinWizardShareObject).IncludedColumnsList.Count != 0);
        }

        protected override bool LeaveCore(DataJoinWizardShareObject parameter)
        {
            if (parameter != null)
            {
                Collection<FeatureSourceColumn> tmpColumns = new Collection<FeatureSourceColumn>();
                Collection<FeatureSourceColumn> duplicateColumns = new Collection<FeatureSourceColumn>();
                Collection<FeatureSourceColumn> invalidColumns = new Collection<FeatureSourceColumn>();
                foreach (var item in parameter.IncludedColumnsList)
                {
                    if (!tmpColumns.Select(f => f.ColumnName).Contains(item.ColumnName))
                        tmpColumns.Add(item);
                    else
                    {
                        duplicateColumns.Add(item);
                    }
                    if (item.ColumnName.Length > 10)
                    {
                        invalidColumns.Add(item);
                    }
                }

                StringBuilder stringBuilder = new StringBuilder();
                if (invalidColumns.Count > 0)
                {
                    stringBuilder.AppendLine("Length > 10:");
                    int i = 0;
                    foreach (var item in invalidColumns)
                    {
                        if (i < 5)
                            stringBuilder.AppendLine(item.ColumnName);
                        if (i == 5)
                            stringBuilder.AppendLine("...");
                        i++;
                    }
                }
                if (duplicateColumns.Count > 0)
                {
                    stringBuilder.AppendLine("Duplicate:");
                    int i = 0;
                    foreach (var item in duplicateColumns)
                    {
                        if (i < 5)
                            stringBuilder.AppendLine(item.ColumnName);
                        if (i == 5)
                            stringBuilder.AppendLine("...");
                        i++;
                    }
                }

                if ((invalidColumns.Count > 0 && duplicateColumns.Count == 0) || (invalidColumns.Count > 0 && duplicateColumns.Count > 0))
                {
                    bool promptResponseIsYes;
                    if (invalidColumns.Count > 0 && duplicateColumns.Count == 0)
                    {
                        promptResponseIsYes = System.Windows.Forms.MessageBox.Show("The following column names are invalid. \n" + stringBuilder.ToString() + "\nDo you want to shorten the columns to be equal to the first ten characters?",
                            "Warning!", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes;
                    }
                    else
                    {
                        promptResponseIsYes = System.Windows.Forms.MessageBox.Show("The following column names are invalid. \n" + stringBuilder.ToString() + "\nDo you want to remove the column(s) and shorten the columns to be equal to the first ten characters?",
                            "Warning!", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes;
                    }
                        
                    if (promptResponseIsYes)
                    {
                        Collection<FeatureSourceColumn> duplicateColumnsAfterTrim = new Collection<FeatureSourceColumn>();
                        foreach (var item in invalidColumns)
                        {
                            string trimmedColumnName = item.ColumnName.Substring(0, 10);
                            if (!parameter.IncludedColumnsList.Select(f => f.ColumnName).Contains(trimmedColumnName))
                            {
                                int index = parameter.IncludedColumnsList.IndexOf(item);
                                parameter.IncludedColumnsList.Remove(item);
                                parameter.IncludedColumnsList.Insert(index, new DataJoinFeatureSourceColumn(trimmedColumnName, item.TypeName, item.MaxLength));
                                parameter.InvalidColumns.Add(trimmedColumnName, item.ColumnName);
                            }
                            else
                            {
                                duplicateColumnsAfterTrim.Add(item);
                            }
                        }
                        if (duplicateColumnsAfterTrim.Count != 0)
                        {
                            StringBuilder duplicateMessage = new StringBuilder();
                            duplicateMessage.AppendLine("Duplicate:");
                            int i = 0;
                            foreach (var item in duplicateColumnsAfterTrim)
                            {
                                duplicateColumns.Add(item);
                                if (i < 5)
                                    duplicateMessage.AppendLine(item.ColumnName);
                                if (i == 5)
                                    duplicateMessage.AppendLine("...");
                                i++;
                            }
                            if (System.Windows.Forms.MessageBox.Show("After shortening the column name(s), the following names were repeated. \n" + duplicateMessage.ToString() + "\nIt is recommended that you inspect the column names and change them manually. Would you like to keep the results of this data join? (Warning: Some columns will not be included)",
                                "Warning", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                            {
                                return false;
                            }
                        }
                        foreach (var item in duplicateColumns)
                        {
                            parameter.IncludedColumnsList.Remove(item);
                        }
                        return true;
                    }
                    else
                        return false;
                }
                else if (invalidColumns.Count == 0 && duplicateColumns.Count > 0)
                {
                    if (System.Windows.Forms.MessageBox.Show("The following column names are duplicated. \n" + stringBuilder.ToString() + "\nDo you want to remove the column(s)?",
                        "Warning!", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    {
                        foreach (var item in duplicateColumns)
                        {
                            parameter.IncludedColumnsList.Remove(item);
                        }
                        return true;
                    }
                    else return false;
                }                
                else
                    return true;
            }
            else
                return false;
        }
    }
}
