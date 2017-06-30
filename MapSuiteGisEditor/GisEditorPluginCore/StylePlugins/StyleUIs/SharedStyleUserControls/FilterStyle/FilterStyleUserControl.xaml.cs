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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for NewFilterStyleUserControl.xaml
    /// </summary>
    public partial class FilterStyleUserControl : StyleUserControl
    {
        private static readonly string syntaxKeyword = "feature.";
        private bool isDesending;
        private FilterStyleViewModel viewModel;
        private FilterStyle filterStyle;
        private CompletionWindow completionWindow;

        public FilterStyleUserControl(FilterStyle style, StyleBuilderArguments requiredValues)
        {
            StyleBuilderArguments = requiredValues;
            InitializeComponent();

            filterStyle = style;

            viewModel = new FilterStyleViewModel(style, requiredValues);
            DataContext = viewModel;
            textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");

            string helpUri = GisEditor.LanguageManager.GetStringResource("FilterStyleHelp");
            if (!string.IsNullOrEmpty(helpUri))
            {
                HelpUri = new Uri(helpUri);
            }


            ScriptComboBox.ItemsSource = Enum.GetNames(typeof(FilterStyleScriptType)).Where(e => e != "None");
            textEditor.TextArea.TextEntering += new TextCompositionEventHandler(TextArea_TextEntering);
            textEditor.TextArea.TextEntered += new TextCompositionEventHandler(TextArea_TextEntered);
        }

        private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == ".")
            {
                int offset = textEditor.CaretOffset - syntaxKeyword.Length;
                if (offset < 0) return;
                if (offset + syntaxKeyword.Length > textEditor.TextArea.Document.TextLength) return;

                string lastInputText = textEditor.TextArea.Document.GetText(offset, syntaxKeyword.Length);
                if (lastInputText.Equals(syntaxKeyword, StringComparison.Ordinal))
                {
                    completionWindow = new CompletionWindow(textEditor.TextArea);
                    completionWindow.SizeToContent = SizeToContent.WidthAndHeight;
                    IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;

                    foreach (var columnName in viewModel.RequiredValues.ColumnNames)
                    {
                        data.Add(new ColumnCompletionData(string.Format(CultureInfo.InvariantCulture, "ColumnValues[\"{0}\"]", columnName)));
                    }

                    completionWindow.Show();
                    completionWindow.Closed += delegate
                    {
                        completionWindow = null;
                    };
                }
            }
        }

        private void TextArea_TextEntering(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    completionWindow.CompletionList.RequestInsertion(e);
                }
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

            if (viewModel.FilterConditions.Where(f => f.MatchType.Key != FilterConditionType.DynamicLanguage).Any(c => string.IsNullOrEmpty(c.ColumnName)))
            {
                errorMessage.AppendLine("Column name cannot be empty.");
            }

            int bracketIndentify = 0;
            foreach (var condition in viewModel.FilterConditions)
            {
                if (condition.IsLeftBracket)
                    bracketIndentify += 1;
                if (condition.IsRightBracket)
                    bracketIndentify -= 1;

                if (bracketIndentify < 0) break;
            }
            if (bracketIndentify != 0) errorMessage.AppendLine("The closing bracket must pair with the nearest preceding opening bracket.");

            FilterStyle style = viewModel.ActualObject as FilterStyle;

            if (style != null)
            {
                try
                {
                    var result = style.GetRequiredColumnNames().All(c => viewModel.RequiredValues.ColumnNames.Contains(c));
                    if (!result) errorMessage.AppendLine(GisEditor.LanguageManager.GetStringResource("FilterStyleUserControlColumnMessage"));
                    else
                    {
                        Collection<Feature> features = new Collection<Feature>();
                        ScriptFilterCondition condition = style.Conditions.OfType<ScriptFilterCondition>().FirstOrDefault();
                        if (condition != null)
                        {
                            condition.GetMatchingFeatures(features);
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorMessage.AppendLine(ex.Message);
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
            }

            return errorMessage.ToString();
        }

        [Obfuscation]
        private void ListViewItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = e.Source as ListViewItem;
            if (item != null)
            {
                var model = item.Content as FilterConditionViewModel;
                if (model != null) viewModel.EditConditionCommand.Execute(model);
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
                    ObservableCollection<FilterConditionViewModel> filterConditions = null;

                    var viewModel = DataContext as FilterStyleViewModel;
                    if (viewModel != null)
                        filterConditions = viewModel.FilterConditions;

                    List<FilterConditionViewModel> results = new List<FilterConditionViewModel>();
                    var headerText = clickedColumn.Header;
                    if (headerText.Equals("Match Value"))
                    {
                        results = !isDesending ? filterConditions.OrderBy(v => v.FilterCondition.ColumnName).ToList() : filterConditions.OrderByDescending(v => v.FilterCondition.ColumnName).ToList();
                    }

                    isDesending = !isDesending;
                    filterConditions.Clear();

                    foreach (var item in results)
                    {
                        filterConditions.Add(item);
                    }
                }
            }
        }

        [Obfuscation]
        private void TextEditor_TextChanged(object sender, System.EventArgs e)
        {
            TextEditor editor = (TextEditor)sender;
            if (viewModel != null)
            {
                viewModel.FilterScript = editor.Text;
            }
        }

        [Obfuscation]
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            string functionString = string.Empty;

            switch (viewModel.FilterStyleScriptType)
            {
                case FilterStyleScriptType.CSharp:
                    functionString = "ThinkGeo.MapSuite.GisEditor.Plugins.StylePlugins.StyleUIs.SharedStyleUserControls.FilterStyle.FilterCodeTemplate.CSharp.cs";
                    break;

                case FilterStyleScriptType.Ruby:
                    functionString = "ThinkGeo.MapSuite.GisEditor.Plugins.StylePlugins.StyleUIs.SharedStyleUserControls.FilterStyle.FilterCodeTemplate.Ruby.rb";
                    break;

                case FilterStyleScriptType.Python:
                    functionString = "ThinkGeo.MapSuite.GisEditor.Plugins.StylePlugins.StyleUIs.SharedStyleUserControls.FilterStyle.FilterCodeTemplate.Python.py";
                    break;

                default:
                    break;
            }

            var resouceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(functionString);
            using (StreamReader streamReader = new StreamReader(resouceStream))
            {
                functionString = streamReader.ReadToEnd();
            }
            viewModel.FilterScript = functionString;
        }

        [Obfuscation]
        public class ColumnCompletionData : ICompletionData
        {
            private ImageSource image;

            public ColumnCompletionData(string text) :
                this(text, string.Format(CultureInfo.InvariantCulture, "Description for {0}.", text))
            { }

            public ColumnCompletionData(string text, string description)
            {
                this.Text = text;
                this.Description = description;
                this.image = new BitmapImage(new Uri("/GisEditorPluginCore;component/images/public_property.png", UriKind.Relative));
            }

            public System.Windows.Media.ImageSource Image
            {
                get { return image; }
            }

            public string Text { get; private set; }

            public object Content
            {
                get { return this.Text; }
            }

            public object Description
            {
                get;
                private set;
            }

            public void Complete(TextArea textArea, ISegment completionSegment,
                EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, this.Text);
            }
        }
    }
}