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
using System.Windows;
using System.Windows.Media;
using ThinkGeo.MapSuite.Drawing;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ProjectPathElementWindow.xaml
    /// </summary>
    public partial class ProjectPathElementWindow : Window
    {
        private ProjectPathElementViewModel viewModel;

        public ProjectPathElementWindow(string projectPath)
        {
            InitializeComponent();

            contentPresenter.Content = new FontUserControl();
            viewModel = new ProjectPathElementViewModel(projectPath);
            DataContext = viewModel;

            HelpContainer.Content = HelpResourceHelper.GetHelpButton("PrintMapTextHelp", HelpButtonMode.NormalButton);
        }

        [Obfuscation]
        private void OKClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        internal void SetProperties(ProjectPathPrinterLayer printerLayer)
        {
            viewModel.ProjectPath = printerLayer.ProjectPath;
            viewModel.FontName = new FontFamily(printerLayer.Font.FontName);
            viewModel.FontSize = printerLayer.Font.Size;
            viewModel.IsBold = (printerLayer.Font.Style & DrawingFontStyles.Bold) == DrawingFontStyles.Bold;
            viewModel.IsItalic = (printerLayer.Font.Style & DrawingFontStyles.Italic) == DrawingFontStyles.Italic;
            viewModel.IsStrikeout = (printerLayer.Font.Style & DrawingFontStyles.Strikeout) == DrawingFontStyles.Strikeout;
            viewModel.IsUnderline = (printerLayer.Font.Style & DrawingFontStyles.Underline) == DrawingFontStyles.Underline;
            viewModel.FontColor = printerLayer.TextBrush;
            viewModel.DragMode = printerLayer.DragMode;
            viewModel.ResizeMode = printerLayer.ResizeMode;
        }
    }
}
