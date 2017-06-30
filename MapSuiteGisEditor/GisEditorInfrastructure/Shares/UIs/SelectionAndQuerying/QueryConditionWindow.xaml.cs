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
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for QueryBuilder.xaml
    /// </summary>
    [Obfuscation]
    internal partial class QueryConditionWindow : Window
    {
        private QueryConditionViewModel queryCondition;

        public QueryConditionWindow(QueryConditionViewModel viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException();
            ApplyWindowStyle(this);

            InitializeComponent();
            if (GisEditor.ActiveMap != null)
            {
                queryCondition = viewModel.CloneDeep();
                if (viewModel.Layer != null)
                {
                    layersComboBox.IsEnabled = false;
                }

                DataContext = QueryCondition;
            }

            Messenger.Default.Register<bool>(this, queryCondition, (message) =>
            {
                if (IsActive)
                {
                    DialogResult = message;
                    if (message)
                    {
                        viewModel.Layer = queryCondition.Layer;
                        viewModel.QueryOperator = queryCondition.QueryOperator;
                        viewModel.ColumnName = queryCondition.ColumnName;
                        viewModel.MatchValue = queryCondition.MatchValue;
                    }
                    Close();
                }
            });
            Closing += (s, e) =>
            {
                Messenger.Default.Unregister(this);
            };
        }

        public QueryConditionViewModel QueryCondition
        {
            get { return queryCondition; }
        }

        [Conditional("GISEditorUnitTest")]
        private static void ApplyWindowStyle(Window window)
        {
            ResourceDictionary resourceDic = new ResourceDictionary();
            resourceDic.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/MapSuiteGisEditor;component/Resources/General.xaml", UriKind.RelativeOrAbsolute) });
            window.Resources = resourceDic;
        }
    }
}