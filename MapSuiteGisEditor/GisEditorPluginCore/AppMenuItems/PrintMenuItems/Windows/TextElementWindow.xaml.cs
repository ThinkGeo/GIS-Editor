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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for TextElementWindow.xaml
    /// </summary>
    public partial class TextElementWindow : Window
    {
        private TextElementViewModel textViewModel;

        public TextElementWindow(LabelMode labelMode = LabelMode.Label)
        {
            InitializeComponent();
            contentPresenter.Content = new FontUserControl();
            textViewModel = new TextElementViewModel();
            DataContext = textViewModel;
            HelpContainer.Content = HelpResourceHelper.GetHelpButton("PrintMapTextHelp", HelpButtonMode.NormalButton);
            if (labelMode == LabelMode.Signature)
            {
                SignatureNameGroupBox.Visibility = Visibility.Visible;
            }
        }

        public string SignatureName
        {
            get { return SignatureNameTextBox.Text; }
        }

        internal void SetProperties(PrinterLayer printerLayer)
        {
            LabelPrinterLayer labelPrinterLayer = printerLayer as LabelPrinterLayer;
            if (labelPrinterLayer != null)
            {
                textViewModel.WrapText = labelPrinterLayer.PrinterWrapMode == PrinterWrapMode.WrapText;
                textViewModel.Text = labelPrinterLayer.Text;
                textViewModel.FontName = new FontFamily(labelPrinterLayer.Font.FontName);
                textViewModel.FontSize = labelPrinterLayer.Font.Size;
                textViewModel.IsBold = (labelPrinterLayer.Font.Style & DrawingFontStyles.Bold) == DrawingFontStyles.Bold;
                textViewModel.IsItalic = (labelPrinterLayer.Font.Style & DrawingFontStyles.Italic) == DrawingFontStyles.Italic;
                textViewModel.IsStrikeout = (labelPrinterLayer.Font.Style & DrawingFontStyles.Strikeout) == DrawingFontStyles.Strikeout;
                textViewModel.IsUnderline = (labelPrinterLayer.Font.Style & DrawingFontStyles.Underline) == DrawingFontStyles.Underline;
                textViewModel.FontColor = labelPrinterLayer.TextBrush;

                textViewModel.DragMode = labelPrinterLayer.DragMode;
                textViewModel.ResizeMode = labelPrinterLayer.ResizeMode;
            }
        }

        [Obfuscation]
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        [Obfuscation]
        private void OKClick(object sender, RoutedEventArgs e)
        {
            if (SignatureNameGroupBox.Visibility == Visibility.Visible)
            {
                if (string.IsNullOrEmpty(SignatureNameTextBox.Text))
                {
                    System.Windows.Forms.MessageBox.Show("Signature name cannot be empty.");
                }
                else if (string.IsNullOrEmpty(textViewModel.Text))
                {
                    System.Windows.Forms.MessageBox.Show("Signature cannot be empty.");
                }
                else
                {
                    DialogResult = true;
                    Close();
                }
            }
            else
            {
                DialogResult = !string.IsNullOrEmpty(textViewModel.Text);
                Close();
            }
        }

        [Obfuscation]
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                textViewModel.Text += Environment.NewLine;
                (sender as TextBox).SelectionStart = textViewModel.Text.Length;
            }
        }
    }
}