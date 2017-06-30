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


using System.Collections.Generic;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for CreateNewLayerWindow.xaml
    /// </summary>
    public partial class BookmarkNamePromptWindow : Window
    {
        private BookmarkNamePromptViewModel viewModel;

        public BookmarkNamePromptWindow(string bookmarkName, IEnumerable<string> existingNames)
        {
            UnitTestHelper.ApplyWindowStyle(this);
            InitializeComponent();

            HelpContainer.Content = HelpResourceHelper.GetHelpButton("BookmarksHelp", HelpButtonMode.IconOnly);
            viewModel = new BookmarkNamePromptViewModel(bookmarkName, existingNames);
            DataContext = viewModel;
            Messenger.Default.Register<DialogMessage>(this, viewModel, msg =>
            {
                MessageBox.Show(msg.Content, msg.Caption, msg.Button, msg.Icon);
            });
            Messenger.Default.Register<bool>(this, viewModel, msg =>
            {
                if (msg)
                {
                    DialogResult = true;
                }
            });

            Unloaded += (s, e) => Messenger.Default.Unregister(this);
        }

        public string BookmarkName
        {
            get
            {
                return viewModel.Name;
            }
        }

        public bool IsGlobal
        {
            get 
            {
                return viewModel.IsGlobal;
            }
        }
    }
}