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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ClassBreakAreaStyleUserControl.xaml
    /// </summary>
    public partial class ClassBreakStyleUserControl : StyleUserControl
    {
        private bool isDesending;
        private ClassBreakStyle classBreakStyle;
        private ClassBreakStyleViewModel viewModel;

        public ClassBreakStyleUserControl(ClassBreakStyle style, StyleBuilderArguments requiredValues)
        {
            InitializeComponent();
            StyleBuilderArguments = requiredValues;
            classBreakStyle = style;
            viewModel = new ClassBreakStyleViewModel(style, requiredValues);
            DataContext = viewModel;

            string helpUri = GisEditor.LanguageManager.GetStringResource("ClassBreakStyleHelp");
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

            if (string.IsNullOrEmpty(classBreakStyle.ColumnName))
            {
                errorMessage.AppendLine("Column name is invalid.");
            }

            return errorMessage.ToString();
        }

        [Obfuscation]
        private void ViewDataClick(object sender, RoutedEventArgs e)
        {
            if (GisEditor.ActiveMap.ActiveLayer is FeatureLayer)
            {
                DataViewerUserControl content = new DataViewerUserControl();
                content.ShowDialog();
            }
        }

        [Obfuscation]
        private void ListViewItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
            var item = e.Source as ListViewItem;
            if (item != null)
            {
                var model = item.Content as ClassBreakItem;
                if (model != null) viewModel.EditCommand.Execute(model.Id);
            }
        }

        [Obfuscation]
        private void RenameTextBlock_TextRenamed(object sender, TextRenamedEventArgs e)
        {
            double newValue;
            if (e.OldText.Equals(e.NewText) || string.IsNullOrEmpty(e.NewText) || !double.TryParse(e.NewText, out newValue))
            {
                e.IsCancelled = true;
            }
            else
            {
                var classBreakItem = sender.GetDataContext<ClassBreakItem>();
                if (viewModel.ClassBreakItems.Any(c => c.ClassBreak.Value == newValue))
                {
                    System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ClassBreakStyleUserControlvalueexsistMessage"), "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                    e.IsCancelled = true;
                }
                else
                {
                    if (classBreakItem != null)
                    {
                        classBreakItem.StartingValue = e.NewText;
                        classBreakItem.ClassBreak.Value = newValue;
                    }
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
                    ObservableCollection<ClassBreakItem> classBreakItems = null;
                    var viewModel = DataContext as ClassBreakStyleViewModel;
                    if (viewModel != null)
                        classBreakItems = viewModel.ClassBreakItems;

                    List<ClassBreakItem> results = new List<ClassBreakItem>();
                    string headerText = (String)clickedColumn.Header;
                    if (headerText.Equals("Starting Value"))
                    {
                        results = !isDesending ? classBreakItems.OrderBy(v => GetSize(v)).ToList() : classBreakItems.OrderByDescending(v => GetSize(v)).ToList();
                    }

                    isDesending = !isDesending;
                    classBreakItems.Clear();
                    foreach (var item in results)
                    {
                        classBreakItems.Add(item);
                    }
                }
            }
        }

        private double GetSize(ClassBreakItem itemViewModel)
        {
            return itemViewModel.ClassBreak.Value;
        }
    }
}