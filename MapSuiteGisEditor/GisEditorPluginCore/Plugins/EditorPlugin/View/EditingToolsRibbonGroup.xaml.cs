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
using System.Windows.Input;
using Microsoft.Windows.Controls.Ribbon;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for EditRibbonGroup.xaml
    /// </summary>
    public partial class EditingToolsRibbonGroup : RibbonGroup
    {
        private bool isKeyEventHooked;
        private EditingToolsViewModel viewModel;

        public EditingToolsRibbonGroup()
        {
            InitializeComponent();

            viewModel = EditingToolsViewModel.Instance;
            DataContext = viewModel;
            Loaded += EditingToolsRibbonGroup_Loaded;
        }

        private void EditingToolsRibbonGroup_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isKeyEventHooked && Application.Current != null)
            {
#if GISEditorUnitTest
#else
                Application.Current.MainWindow.KeyDown += new KeyEventHandler(EditingToolsRibbonGroup_KeyDown);
                isKeyEventHooked = true;
#endif
            }
        }

        public EditingToolsViewModel ViewModel
        {
            get { return viewModel; }
        }

        [Obfuscation]
        private void EditingToolsRibbonGroup_KeyDown(object sender, KeyEventArgs e)
        {
            RibbonTab tab = Parent as RibbonTab;

            if (tab != null && tab.IsSelected && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (e.Key == Key.Z && ViewModel.RollBackCommand.CanExecute(null))
                {
                    ViewModel.RollBackCommand.Execute(null);
                }
                else if (e.Key == Key.Y && ViewModel.ForwardCommand.CanExecute(null))
                {
                    ViewModel.ForwardCommand.Execute(null);
                }
            }
        }
    }
}