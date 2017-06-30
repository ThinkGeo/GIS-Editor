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
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ThinkGeo.MapSuite.GisEditor
{
    public partial class RenameTextBlock : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<TextRenamedEventArgs> TextRenamed;

        public static readonly DependencyProperty IsFileNameProperty =
            DependencyProperty.Register("IsFileName", typeof(bool), typeof(RenameTextBlock), new PropertyMetadata(false));

        public static readonly DependencyProperty DisplayTextProperty =
            DependencyProperty.Register("DisplayText", typeof(string), typeof(RenameTextBlock), new PropertyMetadata(""));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(RenameTextBlock), new PropertyMetadata("", new PropertyChangedCallback(TextPropertyChanged)));

        public static readonly DependencyProperty IsEditProperty =
            DependencyProperty.Register("IsEdit", typeof(bool), typeof(RenameTextBlock), new PropertyMetadata(false, new PropertyChangedCallback(IsEditPropertyChanged)));

        private string editText;

        public RenameTextBlock()
        {
            InitializeComponent();
        }

        public bool IsFileName
        {
            get { return (bool)GetValue(IsFileNameProperty); }
            set { SetValue(IsFileNameProperty, value); }
        }

        public string DisplayText
        {
            get { return (string)GetValue(DisplayTextProperty); }
            set { SetValue(DisplayTextProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set
            {
                SetValue(TextProperty, value);
            }
        }

        public bool IsEdit
        {
            get { return (bool)GetValue(IsEditProperty); }
            set { SetValue(IsEditProperty, value); }
        }

        public Visibility DisplayVisibility
        {
            get { return IsEdit ? Visibility.Collapsed : Visibility.Visible; }
        }

        public Visibility EditVisibility
        {
            get { return IsEdit ? Visibility.Visible : Visibility.Collapsed; }
        }

        public string EditText
        {
            get { return editText; }
            set
            {
                editText = value;
                RaisePropertyChanged("EditText");
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                IsEdit = false;
            }
        }

        private void EditingTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            IsEdit = false;
        }

        private static void TextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var renameTextBlock = d as RenameTextBlock;
            if (renameTextBlock != null && e.NewValue != null)
            {
                renameTextBlock.DisplayText = e.NewValue.ToString();
            }
        }

        private static void IsEditPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var renameTextBlock = d as RenameTextBlock;
            if (renameTextBlock != null)
            {
                renameTextBlock.RaisePropertyChanged("DisplayVisibility");
                renameTextBlock.RaisePropertyChanged("EditVisibility");
                if ((bool)e.NewValue)
                {
                    renameTextBlock.EditingTextBox.Focus();
                    renameTextBlock.EditText = renameTextBlock.Text;

                    int dotIndex = renameTextBlock.Text.LastIndexOf(".");
                    if (renameTextBlock.IsFileName && dotIndex > 0)
                    {
                        renameTextBlock.EditingTextBox.Select(0, dotIndex);
                    }
                    else
                    {
                        renameTextBlock.EditingTextBox.SelectAll();
                    }
                }
                else
                {
                    var renamedEventArgs = new TextRenamedEventArgs(renameTextBlock.Text, renameTextBlock.EditText);
                    renameTextBlock.OnTextRenamed(renamedEventArgs);
                    if (!renamedEventArgs.IsCancelled)
                    {
                        renameTextBlock.Text = renameTextBlock.EditText;
                    }
                }
            }
        }

        private void OnTextRenamed(TextRenamedEventArgs renamedEventArgs)
        {
            EventHandler<TextRenamedEventArgs> handler = TextRenamed;
            if (handler != null)
            {
                handler(this, renamedEventArgs);
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
