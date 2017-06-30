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


using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for LegendImporterItemUserControl.xaml
    /// </summary>
    public partial class LegendImporterItemUserControl : UserControl
    {
        //the code that deals with IsInEditing is not move to the viewmodel, because it has something to do with the rename text box
        //it deals with ui directly.

        public LegendImporterItemUserControl()
        {
            InitializeComponent();
        }

        [Obfuscation]
        private void RenameTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as RenameTextBlock).IsEdit = true;
        }

        [Obfuscation]
        private void RenameTextBlock_TextRenamed(object sender, TextRenamedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.NewText.Trim()))
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("LegendImporterItemUserControlNameBlankText"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxAlertCaption"));
                e.IsCancelled = true;
            }
            else if (e.OldText.Equals(e.NewText))
            {
                e.IsCancelled = true;
            }
            else
                (DataContext as LegendImporterItemViewModel).Text = e.NewText;
        }
    }
}