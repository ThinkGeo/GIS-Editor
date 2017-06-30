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
using System.Collections.ObjectModel;
using System.Linq;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ChooseDataStep : WizardStep<DissolveWizardShareObject>
    {
        [NonSerialized]
        private ChooseDataUserControl content;

        private ChooseDataViewModel dataContext;

        public ChooseDataStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepThree");
            Header = GisEditor.LanguageManager.GetStringResource("ChooseDataStepHeader");
            Description = GisEditor.LanguageManager.GetStringResource("ChooseDataStepHeader");
        }

        protected override void EnterCore(DissolveWizardShareObject parameter)
        {
            base.EnterCore(parameter);
            if (Parent.MoveDirection == MoveDirection.Next)
            {
                dataContext = new ChooseDataViewModel(parameter.SelectedFeatureLayer, parameter.DissolveSelectedFeaturesOnly);
                content = new ChooseDataUserControl();
                content.DataContext = dataContext;
                Content = content;

                dataContext.ExtraColumns.Clear();

                if (parameter.ExtraColumns.Count == 0)
                {
                    if (Parent.MoveDirection == MoveDirection.Next)
                        Parent.MoveNext();
                    else
                        Parent.MoveBack();
                }
                else
                {
                    FeatureSourceColumn addAllStringColumn = new FeatureSourceColumn(ChooseDataViewModel.AddAllString);
                    dataContext.ExtraColumns.Add(new CheckableItemViewModel<FeatureSourceColumn>(addAllStringColumn));

                    Collection<FeatureSourceColumn> featureSourceColumns = new Collection<FeatureSourceColumn>();
                    lock (parameter.SelectedFeatureLayer)
                    {
                        if (!parameter.SelectedFeatureLayer.IsOpen) parameter.SelectedFeatureLayer.Open();
                        featureSourceColumns = parameter.SelectedFeatureLayer.QueryTools.GetColumns();
                    }

                    foreach (var extraColumn in parameter.ExtraColumns)
                    {
                        FeatureSourceColumn extraFeatureSourceColumn = featureSourceColumns.FirstOrDefault(tmpColumn => tmpColumn.ColumnName.Equals(extraColumn, StringComparison.Ordinal));
                        var column = new CheckableItemViewModel<FeatureSourceColumn>(extraFeatureSourceColumn);
                        column.AliasName = parameter.SelectedFeatureLayer.FeatureSource.GetColumnAlias(extraFeatureSourceColumn.ColumnName);
                        dataContext.ExtraColumns.Add(column);
                    }

                    dataContext.SelectedColumn = dataContext.ExtraColumns[0];
                    dataContext.SelectedOperator = DissolveOperatorMode.First;
                }
            }
        }

        protected override bool LeaveCore(DissolveWizardShareObject parameter)
        {
            base.LeaveCore(parameter);
            foreach (var pair in dataContext.OperatorPairs)
            {
                parameter.OperatorPairs.Add(pair);
            }
            return true;
        }
    }
}