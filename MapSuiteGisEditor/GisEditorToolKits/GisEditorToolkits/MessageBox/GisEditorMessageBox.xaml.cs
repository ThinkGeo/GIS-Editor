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
using System.Windows.Documents;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for GisEditorMessageBox.xaml
    /// </summary>
    public partial class GisEditorMessageBox : Window
    {
        private Action toggleButtonClickAction;
        private GisEditorMessageBoxViewModel viewModel;
        private string message;
        private InlineCollection inlines;

        public GisEditorMessageBox(MessageBoxButton buttonMode)
        {
            InitializeComponent();

            inlines = MessageTextBlock.Inlines;
            viewModel = new GisEditorMessageBoxViewModel();
            DataContext = viewModel;

            switch (buttonMode)
            {
                case MessageBoxButton.YesNo:
                default:
                    viewModel.IsYesOrNoVisibility = Visibility.Visible;
                    viewModel.IsOkVisibility = Visibility.Collapsed;
                    viewModel.IsCancelVisiblity = Visibility.Collapsed;
                    break;
                case MessageBoxButton.OKCancel:
                    viewModel.IsOkVisibility = Visibility.Visible;
                    viewModel.IsCancelVisiblity = Visibility.Visible;
                    viewModel.IsYesOrNoVisibility = Visibility.Collapsed;
                    break;
                case MessageBoxButton.YesNoCancel:
                    viewModel.IsOkVisibility = Visibility.Collapsed;
                    viewModel.IsYesOrNoVisibility = Visibility.Visible;
                    viewModel.IsCancelVisiblity = Visibility.Visible;
                    break;
                case MessageBoxButton.OK:
                    viewModel.IsOkVisibility = Visibility.Visible;
                    viewModel.IsCancelVisiblity = Visibility.Collapsed;
                    viewModel.IsYesOrNoVisibility = Visibility.Collapsed;
                    break;
            }
        }

        public Action ToggleButtonClickAction
        {
            get { return toggleButtonClickAction; }
            set { toggleButtonClickAction = value; }
        }

        public string NoteMessage
        {
            get { return viewModel.NoteMessage; }
            set { viewModel.NoteMessage = value; }
        }

        public string ViewDetailHeader
        {
            get { return viewModel.ViewDetailHeader; }
            set { viewModel.ViewDetailHeader = value; }
        }

        public object DetailContent
        {
            get { return viewModel.DetailContent; }
            set { viewModel.DetailContent = value; }
        }

        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        public InlineCollection Inlines
        {
            get { return inlines; }
        }

        public string ErrorMessage
        {
            get { return viewModel.ErrorMessage; }
            set { viewModel.ErrorMessage = value; }
        }

        public bool IsErrorMessageVisible
        {
            get { return ViewErrorButton.IsChecked.GetValueOrDefault(); }
            set { ViewErrorButton.IsChecked = value; }
        }

        [Obfuscation]
        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        [Obfuscation]
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        [Obfuscation]
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Inlines.Count == 0)
            {
                MessageTextBlock.Text = Message;
            }
        }

        [Obfuscation]
        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (ToggleButtonClickAction != null)
            {
                ToggleButtonClickAction();
            }
        }
    }
}
