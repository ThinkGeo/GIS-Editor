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


using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for LabelFunctionsWindow.xaml
    /// </summary>
    public partial class LabelFunctionsWindow : Window
    {
        private static readonly string syntaxKeyword1 = "column1.";
        private static readonly string syntaxKeyword2 = "column2.";
        private static readonly string syntaxKeyword3 = "column3.";
        private static readonly string syntaxKeyword4 = "string.";
        private static readonly string syntaxKeyword5 = "int.";
        private static readonly string propertyImageUri = "/GisEditorPluginCore;component/images/public_property.png";
        private static readonly string methodImageUri = "/GisEditorPluginCore;component/images/public_method.png";
        private CompletionWindow completionWindow;
        private LabelFunctionsViewModel viewModel;
        private Collection<LabelCompletionData> stringItems;
        private Collection<LabelCompletionData> intItems;
        private Collection<LabelCompletionData> stringStaticItems;

        public LabelFunctionsWindow()
            : this(new Dictionary<string, string>(), string.Empty, new Dictionary<string, string>())
        {
        }

        public LabelFunctionsWindow(Dictionary<string, string> columnNames, string functionText, Dictionary<string, string> nameColumnDic)
        {
            InitializeComponent();
            Initialize();

            viewModel = new LabelFunctionsViewModel(columnNames, nameColumnDic);
            DataContext = viewModel;
            textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
            textEditor.TextArea.TextEntering += new TextCompositionEventHandler(TextArea_TextEntering);
            textEditor.TextArea.TextEntered += new TextCompositionEventHandler(TextArea_TextEntered);
            textEditor.Text = functionText;
        }

        private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == ".")
            {
                int offset1 = textEditor.CaretOffset - syntaxKeyword1.Length;
                if (offset1 < 0) return;
                if (offset1 + syntaxKeyword1.Length > textEditor.TextArea.Document.TextLength) return;

                int offset2 = textEditor.CaretOffset - syntaxKeyword4.Length;
                if (offset2 < 0) return;
                if (offset2 + syntaxKeyword4.Length > textEditor.TextArea.Document.TextLength) return;

                int offset3 = textEditor.CaretOffset - syntaxKeyword5.Length;
                if (offset3 < 0) return;
                if (offset3 + syntaxKeyword5.Length > textEditor.TextArea.Document.TextLength) return;

                string lastInputText1 = textEditor.TextArea.Document.GetText(offset1, syntaxKeyword1.Length);
                string lastInputText2 = textEditor.TextArea.Document.GetText(offset2, syntaxKeyword4.Length);
                string lastInputText3 = textEditor.TextArea.Document.GetText(offset3, syntaxKeyword5.Length);
                if (lastInputText1.Equals(syntaxKeyword1, StringComparison.Ordinal)
                    || lastInputText1.Equals(syntaxKeyword2, StringComparison.Ordinal)
                    || lastInputText1.Equals(syntaxKeyword3, StringComparison.Ordinal))
                {
                    completionWindow = new CompletionWindow(textEditor.TextArea);
                    completionWindow.SizeToContent = SizeToContent.WidthAndHeight;
                    IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;

                    foreach (var item in stringItems)
                    {
                        data.Add(item);
                    }

                    completionWindow.Show();
                    completionWindow.Closed += delegate
                    {
                        completionWindow = null;
                    };
                }
                else if (lastInputText2.Equals(syntaxKeyword4, StringComparison.Ordinal))
                {
                    completionWindow = new CompletionWindow(textEditor.TextArea);
                    completionWindow.SizeToContent = SizeToContent.WidthAndHeight;
                    IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;

                    foreach (var item in stringStaticItems)
                    {
                        data.Add(item);
                    }

                    completionWindow.Show();
                    completionWindow.Closed += delegate
                    {
                        completionWindow = null;
                    };
                }
                else if (lastInputText3.Equals(syntaxKeyword5, StringComparison.Ordinal))
                {
                    completionWindow = new CompletionWindow(textEditor.TextArea);
                    completionWindow.SizeToContent = SizeToContent.WidthAndHeight;
                    IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;

                    foreach (var item in intItems)
                    {
                        data.Add(item);
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

        public string CodeText
        {
            get { return viewModel.ScriptText; }
        }

        public Dictionary<string, string> LabelFunctionColumnNames
        {
            get { return viewModel.LabelFunctionColumnNames; }
        }

        [Obfuscation]
        private void TextEditor_TextChanged(object sender, EventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.ScriptText = textEditor.Text;
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            string functionString = "ThinkGeo.MapSuite.GisEditor.Plugins.StylePlugins.StyleUIs.SharedStyleUserControls.LabelFunctions.LabelFunctionCSharpTemplate.cs";

            var resouceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(functionString);
            using (StreamReader streamReader = new StreamReader(resouceStream))
            {
                functionString = streamReader.ReadToEnd();
            }
            textEditor.Text = functionString;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            string message = viewModel.Test();
            if (string.IsNullOrEmpty(message))
            {
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Test failed, please check your input. \r\n Message:  " + message, "Info", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Initialize()
        {
            stringItems = new Collection<LabelCompletionData>();
            stringItems.Add(new LabelCompletionData("IndexOf", "Reports the index of the first occurrence of the specified Unicode character in this string.", methodImageUri));
            stringItems.Add(new LabelCompletionData("Insert", "string Insert(int startIndex, string value)\r\nInserts a specified instance of System.String at a specified index position in this instance.", methodImageUri));
            stringItems.Add(new LabelCompletionData("LastIndexOf", "Reports the index position of the last occurrence of a specified Unicode character within this instance.", methodImageUri));
            stringItems.Add(new LabelCompletionData("LastIndexOfAny", "Reports the index position of the last occurrence in this instance of one or more characters specified in a Unicode array.", methodImageUri));
            stringItems.Add(new LabelCompletionData("Length", "int string.Length \r\nGets the number of characters in the current System.String object.", propertyImageUri));
            stringItems.Add(new LabelCompletionData("PadLeft", "Returns a new string that right-aligns the characters in this instance by padding them with spaces on the left, for a specified total length.", methodImageUri));
            stringItems.Add(new LabelCompletionData("PadRight", "Returns a new string that left-aligns the characters in this string by padding them with spaces on the right, for a specified total length.", methodImageUri));
            stringItems.Add(new LabelCompletionData("Remove", "Deletes all the characters from this string beginning at a specified position and continuing through the last position.", methodImageUri));
            stringItems.Add(new LabelCompletionData("Replace", "Returns a new string in which all occurrences of a specified Unicode character in this instance are replaced with another specified Unicode character.", methodImageUri));
            stringItems.Add(new LabelCompletionData("StartsWith", "Determines whether the beginning of this string instance matches the specified string.", methodImageUri));
            stringItems.Add(new LabelCompletionData("Substring", "Retrieves a substring from this instance. The substring starts at a specified character position.", methodImageUri));
            stringItems.Add(new LabelCompletionData("ToLower", "string string.ToLower() \r\nReturns a copy of this string converted to lowercase.", methodImageUri));
            stringItems.Add(new LabelCompletionData("ToLowerInvariant", "string string.ToLowerInvariant() \r\nReturns a copy of this System.String object converted to lowercase using the casing rules of the invariant culture.", methodImageUri));
            stringItems.Add(new LabelCompletionData("ToString", "string string.ToString() \r\nReturns this instance of System.String; no actual conversion is performed.", methodImageUri));
            stringItems.Add(new LabelCompletionData("ToUpper", "string string.ToUpper() \r\nReturns a copy of this string converted to uppercase.", methodImageUri));
            stringItems.Add(new LabelCompletionData("ToUpperInvariant", "string string.ToUpperInvariant() \r\nReturns a copy of this System.String object converted to uppercase using the casing rules of the invariant culture.", methodImageUri));
            stringItems.Add(new LabelCompletionData("Trim", "Removes all leading and trailing white-space characters from the current System.String object.", methodImageUri));
            stringItems.Add(new LabelCompletionData("TrimEnd", "string string.TrimEnd(params char[] trimChars) \r\nRemoves all trailing occurrences of a set of characters specified in an array from the current System.String object.", methodImageUri));
            stringItems.Add(new LabelCompletionData("TrimStart", "string string.TrimStart(params char[] trimChars) \r\nRemoves all leading occurrences of a set of characters specified in an array from the current System.String object.", methodImageUri));
            stringItems.Add(new LabelCompletionData("IsNormalized", "bool string.IsNormalized() \r\nIndicates whether this string is in Unicode normalization form C.", methodImageUri));
            stringItems.Add(new LabelCompletionData("CompareTo", "Compares this instance with a specified System.Object and indicates whether this instance precedes, follows, or appears in the same position in the sort order as the specified System.Object.", methodImageUri));
            stringItems.Add(new LabelCompletionData("Equals", "Determines whether this instance and a specified object, which must also be a System.String object, have the same value.", methodImageUri));
            stringItems.Add(new LabelCompletionData("EndsWith", "Determines whether the end of this string instance matches the specified string.", methodImageUri));

            stringStaticItems = new Collection<LabelCompletionData>();
            stringStaticItems.Add(new LabelCompletionData("Format", "Replaces the format item in a specified string with the string representation of a corresponding object in a specified array. A specified parameter supplies culture-specific formatting information.", methodImageUri));
            stringStaticItems.Add(new LabelCompletionData("Empty", "string string.Empty \r\nRepresents the empty string. This field is read-only.", propertyImageUri));
            stringStaticItems.Add(new LabelCompletionData("Intern", "string string.Intern(string str) \r\nRetrieves the system's reference to the specified System.String.", methodImageUri));
            stringStaticItems.Add(new LabelCompletionData("Copy", "string string.Copy(string str) \r\nCreates a new instance of System.String with the same value as a specified System.String.", methodImageUri));
            stringStaticItems.Add(new LabelCompletionData("IsNullOrEmpty", "bool string.IsNullOrEmpty(string value) \r\nIndicates whether the specified string is null or an System.String.Empty string.", methodImageUri));
            stringStaticItems.Add(new LabelCompletionData("IsNullOrWhiteSpace", "bool string.IsNullOrWhiteSpace(string value) \r\nIndicates whether a specified string is null, empty, or consists only of white-space characters.", methodImageUri));

            intItems = new Collection<LabelCompletionData>();
            intItems.Add(new LabelCompletionData("ToString", "string int.ToString() \r\nConverts the numeric value of this instance to its equivalent string representation.", methodImageUri));
        }
    }

    [Obfuscation]
    public class LabelCompletionData : ICompletionData
    {
        private ImageSource image;

        public LabelCompletionData(string text) :
            this(text, string.Format(CultureInfo.InvariantCulture, "Description for {0}.", text))
        { }

        public LabelCompletionData(string text, string description)
        { }

        public LabelCompletionData(string text, string description, string imageUri)
        {
            this.Text = text;
            this.Description = description;
            this.image = new BitmapImage(new Uri(imageUri, UriKind.Relative));
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
