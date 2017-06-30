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
using System.Reflection;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for WindowWithTextBox.xaml
    /// </summary>
    public partial class DynamicLanguageBoardUserControl : UserControl
    {
        private DynamicLanguageBoardViewModel viewModel;

        public DynamicLanguageBoardUserControl()
        {
            InitializeComponent();
            viewModel = new DynamicLanguageBoardViewModel();
            DataContext = viewModel;
            textBox.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
        }

        public DlrLanguage CurrentLanguage
        {
            get { return viewModel.CurrentLanguage; }
            set { viewModel.CurrentLanguage = value; }
        }

        public string ScriptText
        {
            get { return viewModel.ScriptText; }
            set { viewModel.ScriptText = value; }
        }

        //this method works around the fact that the TextEditor's Text property is not bindable.
        //the TextEditorExtender makes the changes go from source to UI
        //this method makes the changes go from UI to source
        [Obfuscation]
        private void textBox_TextChanged(object sender, EventArgs e)
        {
            TextEditor editor = (TextEditor)sender;
            if (viewModel != null)
            {
                ScriptText = editor.Text;
            }
        }
    }
}