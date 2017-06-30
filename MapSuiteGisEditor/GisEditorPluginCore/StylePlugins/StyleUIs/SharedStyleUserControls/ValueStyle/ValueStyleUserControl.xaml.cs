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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ClassBreakAreaStyleUserControl.xaml
    /// </summary>
    public partial class ValueStyleUserControl : StyleUserControl
    {
        private bool isDesending;
        private ValueStyleViewModel viewModel;
        private ValueStyle valueStyle;

        public ValueStyleUserControl(ValueStyle style, StyleBuilderArguments requiredValues)
        {
            InitializeComponent();
            StyleBuilderArguments = requiredValues;
            valueStyle = style;
            viewModel = new ValueStyleViewModel(style, requiredValues);
            DataContext = viewModel;

            string helpUri = GisEditor.LanguageManager.GetStringResource("ValueStyleHelp");
            if (!string.IsNullOrEmpty(helpUri))
            {
                HelpUri = new Uri(helpUri);
            }
        }

        protected override bool ValidateCore()
        {
            string errorMessage = GetErrorMessage();
            if (!string.IsNullOrEmpty(errorMessage))
            {
                System.Windows.Forms.MessageBox.Show(errorMessage, "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                return false;
            }
            else return true;
        }

        private string GetErrorMessage()
        {
            StringBuilder errorMessage = new StringBuilder();

            if (string.IsNullOrEmpty(valueStyle.ColumnName))
            {
                errorMessage.AppendLine("Column name cannot be empty.");
            }

            return errorMessage.ToString();
        }

        [Obfuscation]
        private void ListViewItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
            var item = e.Source as ListViewItem;
            if (item != null)
            {
                var valueItemEntity = item.Content as ValueItemEntity;
                if (valueItemEntity != null) viewModel.EditCommand.Execute(valueItemEntity.Id);
            }
        }

        [Obfuscation]
        private void ViewDataClick(object sender, RoutedEventArgs e)
        {
            DataViewerUserControl content = new DataViewerUserControl(StyleBuilderArguments.FeatureLayer);
            content.ShowDialog();
        }

        [Obfuscation]
        private void RenameTextBlock_TextRenamed(object sender, TextRenamedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.NewText) || e.NewText.Equals(e.OldText) || string.IsNullOrEmpty(e.NewText.Trim()))
            {
                e.IsCancelled = true;
            }
            else
            {
                var valueItemEntity = sender.GetDataContext<ValueItemEntity>();
                if (valueItemEntity != null)
                {
                    valueItemEntity.MatchedValue = e.NewText;
                    valueItemEntity.ValueItem.Value = e.NewText;
                    viewModel.AnalyzeCount(StyleBuilderArguments.FeatureLayer);
                }
            }
        }

        [Obfuscation]
        private void ListViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader)
            {
                GridViewColumn clickedColumn = (e.OriginalSource as GridViewColumnHeader).Column;
                if (clickedColumn != null && clickedColumn.Header != null)
                {
                    ObservableCollection<ValueItemEntity> valueItems = null;

                    var viewModel = DataContext as ValueStyleViewModel;
                    if (viewModel != null)
                        valueItems = viewModel.ValueItems;

                    List<ValueItemEntity> results = new List<ValueItemEntity>();
                    var headerText = clickedColumn.Header;
                    if (headerText.Equals("Matching Value"))
                    {
                        results = !isDesending ? valueItems.OrderBy(v => v.MatchedValue).ToList() : valueItems.OrderByDescending(v => v.MatchedValue).ToList();
                    }
                    else if (headerText.Equals("Count"))
                    {
                        results = !isDesending ? valueItems.OrderBy(v => v.Count).ToList() : valueItems.OrderByDescending(v => v.Count).ToList();
                    }

                    isDesending = !isDesending;
                    valueItems.Clear();
                    foreach (var item in results)
                    {
                        valueItems.Add(item);
                    }
                }
            }
        }
    }
}