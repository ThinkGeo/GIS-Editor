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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Windows.Controls.Ribbon;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for AnnotationRibbonGroup.xaml
    /// </summary>
    public partial class AnnotationRibbonGroup : RibbonGroup
    {
        private static bool isKeyEventHooked = false;

        public AnnotationRibbonGroup()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(AnnotationRibbonGroup_Loaded);
        }

        private AnnotationViewModel ViewModel
        {
            get
            {
                var viewmodel = DataContext as AnnotationViewModel;
                return viewmodel;
            }
        }

        [Obfuscation]
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            RibbonTab tab = Parent as RibbonTab;

            if (tab != null && tab.IsSelected && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && ViewModel != null)
            {
                if (e.Key == Key.Z && ViewModel.CanUndo)
                {
                    ViewModel.UndoCommand.Execute(null);
                }
                else if (e.Key == Key.Y && ViewModel.CanRedo)
                {
                    ViewModel.RedoCommand.Execute(null);
                }
            }
        }

        private void AnnotationRibbonGroup_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isKeyEventHooked && Application.Current != null)
            {
                Application.Current.MainWindow.KeyDown += new KeyEventHandler(MainWindow_KeyDown);
                isKeyEventHooked = true;
            }
        }

        [Obfuscation]
        private void RibbonToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!((RibbonToggleButton)sender).IsChecked.GetValueOrDefault())
            {
                ((RibbonToggleButton)sender).IsChecked = true;
                //"{Binding RelativeSource={RelativeSource AncestorType=ListBoxItem},
                //Path=IsSelected,Mode=OneWayToSource}";
                Binding binding = new Binding("IsSelected");
                binding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ListBoxItem), 1);
                ((RibbonToggleButton)sender).SetBinding(RibbonToggleButton.IsCheckedProperty, binding);
            }

            if (((RibbonToggleButton)sender).Label == "Point")
            {
                ViewModel.CurrentAnnotationOverlay.FileLinkable = false;
            }
            e.Handled = true;
        }

        [Obfuscation]
        private void RibbonToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!((RibbonToggleButton)sender).IsChecked.GetValueOrDefault())
            {
                ((RibbonToggleButton)sender).IsChecked = true;
                Binding binding = new Binding("IsInModifyMode");
                ((RibbonToggleButton)sender).SetBinding(RibbonToggleButton.IsCheckedProperty, binding);
            }
            e.Handled = true;
        }
    }
}