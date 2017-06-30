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


using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class OutputUserControl : UserControl
    {
        private OutputUserControlViewModel viewModel;

        public OutputUserControl()
        {
            InitializeComponent();

            viewModel = DataContext as OutputUserControlViewModel;
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            viewModel = DataContext as OutputUserControlViewModel;
            if (viewModel != null && string.IsNullOrEmpty(viewModel.TempFileName))
            {
                viewModel.TempFileName = GetTempFileName(viewModel.DefaultPrefix);
            }
        }

        private string GetTempFileName(string tempFileName)
        {
            string tempProjectFolder = FolderHelper.GetCurrentProjectTaskResultFolder();
            string name = FileExportHelper.GetExportFileName(tempFileName);
            string suffix = string.Empty;

            int index = 0;

            while (File.Exists(Path.Combine(tempProjectFolder, name + "_" + index.ToString())))
            {
                index++;
                suffix = string.Concat(new object[] { "_(", index.ToString(), ")" });
            }

            return name += suffix;
        }

        [Obfuscation]
        private void BrowseClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = viewModel.ExtensionFilter;
            dialog.FileName = viewModel.TempFileName;
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                string noExtentionPath = Path.Combine(Path.GetDirectoryName(dialog.FileName), Path.GetFileNameWithoutExtension(dialog.FileName));
                string pathFileName = dialog.FileName;
                string extension = Path.GetExtension(dialog.FileName);
                int i = 0;
                bool isExist = false;
                while (File.Exists(pathFileName))
                {
                    i++;
                    pathFileName = noExtentionPath + i.ToString() + extension;
                    isExist = true;
                }
                if (isExist) MessageBox.Show(string.Format(GisEditor.LanguageManager.GetStringResource("TheOutPutfilehasrenamedText"), Path.GetFileName(pathFileName)));
                viewModel.OutputPathFileName = pathFileName;
                autoCompleteTextBox.Text = pathFileName;
            }
        }

        [Obfuscation]
        private void Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            viewModel.OutputPathFileName = autoCompleteTextBox.Text;
        }
    }
}