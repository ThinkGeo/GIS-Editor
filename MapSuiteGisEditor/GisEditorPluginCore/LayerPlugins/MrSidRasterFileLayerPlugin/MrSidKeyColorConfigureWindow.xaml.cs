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
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using ThinkGeo.MapSuite.Drawing;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for MrSidKeyColorConfigureWindow.xaml
    /// </summary>
    public partial class MrSidKeyColorConfigureWindow : Window
    {
        public MrSidKeyColorConfigureWindow(Collection<GeoColor> colors)
        {
            InitializeComponent();

            if (colors != null)
            {
                if (colors.Count < 22)
                {
                    int colorLength = 22 - colors.Count;
                    for (int i = 0; i < colorLength; i++)
                    {
                        colors.Add(new GeoColor());
                    }
                }

                DataContext = new MrSidKeyColorConfigureViewModel(CreatePalette(colors));
            }
        }

        [Obfuscation]
        private void Window_Loaded(object sender, EventArgs e)
        {
            BackgroundColorPicker.SelectionChanged += BackgroundColorPicker_SelectionChanged;

            customColorList.SelectedIndex = 0;
        }

        [Obfuscation]
        private void BackgroundColorPicker_SelectionChanged(object sender, EventArgs e)
        {
            if (customColorList.SelectedItem != null)
            {
                var viewModel = DataContext as MrSidKeyColorConfigureViewModel;
                viewModel.Colors[customColorList.SelectedIndex] = viewModel.SelectedColor;
                viewModel.RefreshColors();


                var selectedIndex = customColorList.SelectedIndex;
                customColorList.Items.Refresh();

                if (customColorList.SelectedIndex == -1) customColorList.SelectedIndex = selectedIndex;
            }
        }

        [Obfuscation]
        private void ButtonOk_Click(object sender, EventArgs e)
        {
            DialogResult = true;
        }

        private static ObservableCollection<SolidColorBrush> CreatePalette(Collection<GeoColor> colors)
        {
            var defaultColors = new ObservableCollection<SolidColorBrush>();
            foreach (GeoColor colorx in colors)
            {
                Color color = Color.FromArgb(colorx.AlphaComponent, colorx.RedComponent, colorx.GreenComponent, colorx.BlueComponent);
                defaultColors.Add(new SolidColorBrush(color));
            }

            return defaultColors;
        }
    }
}
