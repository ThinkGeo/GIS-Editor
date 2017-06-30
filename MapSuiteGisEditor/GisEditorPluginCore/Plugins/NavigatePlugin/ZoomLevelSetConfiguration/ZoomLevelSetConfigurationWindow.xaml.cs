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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ZoomLevelConfig.xaml
    /// </summary>
    public partial class ZoomLevelSetConfigurationWindow : Window
    {
        private ZoomLevelConfigurationViewModel viewModel;

        private Action<IEnumerable<double>> applyAction;

        public ZoomLevelSetConfigurationWindow()
        {
            InitializeComponent();
            viewModel = new ZoomLevelConfigurationViewModel();
            DataContext = viewModel;
            helpContent.Content = HelpResourceHelper.GetHelpButton("SetZoomLevelsHelp", HelpButtonMode.NormalButton);
        }

        public Action<IEnumerable<double>> ApplyAction
        {
            get { return applyAction; }
            set { applyAction = value; }
        }

        [Obfuscation]
        private void AddZoomLevelClick(object sender, RoutedEventArgs e)
        {
            AddZoomLevelWindow addZoomLevelWindow = new AddZoomLevelWindow();
            ScaleSettingsRibbonGroupViewModel addZoomLevelViewModel = new ScaleSettingsRibbonGroupViewModel();
            addZoomLevelViewModel.UpdateValues();
            double currentScale = GisEditor.ActiveMap.CurrentScale * Conversion.ConvertMeasureUnits(1, DistanceUnit.Inch, addZoomLevelViewModel.SelectedDistanceUnit);
            addZoomLevelViewModel.SelectedScale = addZoomLevelViewModel.Scales.FirstOrDefault(s => Math.Abs(s.Scale - currentScale) < 1);
            addZoomLevelViewModel.Value = addZoomLevelViewModel.SelectedScale.DisplayScale;
            addZoomLevelWindow.DataContext = addZoomLevelViewModel;
            if (addZoomLevelWindow.ShowDialog().GetValueOrDefault())
            {
                var existedScales = viewModel.ZoomLevelSetViewModel.Select(z => z.Scale).ToList();
                if (existedScales.Any(s => Math.Round(s, 0) == Math.Round(addZoomLevelWindow.Scale, 0)))
                {
                    System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ZoomLevelSetWindowNumberDuplicateText"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                }
                else
                {
                    viewModel.ZoomLevelSetViewModel.Add(new ZoomLevelViewModel(viewModel.ZoomLevelSetViewModel.Count, addZoomLevelWindow.Scale));
                    var sortedArray = viewModel.ZoomLevelSetViewModel.OrderByDescending(z => z.Scale).ToArray();
                    viewModel.ZoomLevelSetViewModel.Clear();
                    for (int i = 0; i < sortedArray.Length; i++)
                    {
                        var zoomLevelModel = sortedArray[i];
                        zoomLevelModel.ZoomLevelIndex = i;
                        viewModel.ZoomLevelSetViewModel.Add(zoomLevelModel);
                    }
                }
            }
        }

        [Obfuscation]
        private void EditZoomLevelClick(object sender, RoutedEventArgs e)
        {
            var zoomLevelModel = sender.GetDataContext<ZoomLevelViewModel>();
            if (zoomLevelModel != null)
            {
                viewModel.SelectedZoomLevel = zoomLevelModel;
                zoomLevelModel.IsRenaming = true;
            }
        }

        [Obfuscation]
        private void RemoveZoomLevelClick(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("LoadCustomSeWindowAreYouSureText"), GisEditor.LanguageManager.GetStringResource("LegendManagerWindowDeleteButtonLabel"), System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                viewModel.SelectedZoomLevel = sender.GetDataContext<ZoomLevelViewModel>();
                if (viewModel.SelectedZoomLevel != null)
                {
                    var index = viewModel.ZoomLevelSetViewModel.IndexOf(viewModel.SelectedZoomLevel);
                    viewModel.ZoomLevelSetViewModel.Remove(viewModel.SelectedZoomLevel);
                    for (int i = index; i < viewModel.ZoomLevelSetViewModel.Count; i++)
                    {
                        viewModel.ZoomLevelSetViewModel[i].ZoomLevelIndex = i;
                    }
                }
            }
        }

        [Obfuscation]
        private void ListBoxItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var zoomLevelModel = sender.GetDataContext<ZoomLevelViewModel>();
            if (zoomLevelModel != null)
            {
                zoomLevelModel.IsRenaming = true;
            }
            e.Handled = true;
        }

        [Obfuscation]
        private void OkClick(object sender, RoutedEventArgs e)
        {
            bool zoomLevelSetChanged = IsZoomLevelSetChanged();
            if (zoomLevelSetChanged)
            {
                if (System.Windows.Forms.DialogResult.Yes == System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ZoomLevelSetWindowAdjustMatchZoomLevelsText"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"), System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning))
                {
                    ChangeExtent();
                    if (GisEditor.ActiveMap.MinimumScale > viewModel.ZoomLevelSetViewModel.Last().Scale)
                    {
                        GisEditor.ActiveMap.MinimumScale = viewModel.ZoomLevelSetViewModel.Last().Scale;
                    }
                    DialogResult = true;
                }
            }
            else DialogResult = false;
        }

        [Obfuscation]
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        [Obfuscation]
        private void ScaleEdited(object sender, TextRenamedEventArgs e)
        {
            double resultScale;
            if (string.IsNullOrEmpty(e.NewText) || e.NewText.Equals(e.OldText))
            {
                e.IsCancelled = true;
            }
            else if (!double.TryParse(e.NewText, out resultScale))
            {
                e.IsCancelled = true;
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ZoomLevelSetWindowInputValidNumberText"));
            }
            else if (viewModel.ZoomLevelSetViewModel.Select(z => z.Scale).ToList().Contains(resultScale))
            {
                e.IsCancelled = true;
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ZoomLevelSetWindowNumberDuplicateText"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
            else
            {
                EditZoomLevel(resultScale);
            }
        }

        [Obfuscation]
        private void RevertDefaultClick(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ZoomLevelSetWindowZoomLevelResetText"), GisEditor.LanguageManager.GetStringResource("ZoomLevelSetWindowRevertToDefaultCaption"), System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                var defaultZoomLevelSet = new GoogleMapsZoomLevelSet();
                defaultZoomLevelSet.AddZoomLevels();
                viewModel.ReaddZoomLevels(defaultZoomLevelSet);
            }
        }

        [Obfuscation]
        private void SaveCustomSetClick(object sender, RoutedEventArgs e)
        {
            var saveCustomSetWindow = new SaveCustomSetWindow();
            if (saveCustomSetWindow.ShowDialog().GetValueOrDefault())
            {
                var viewPlugin = GisEditor.UIManager.GetActiveUIPlugins<ViewUIPlugin>().FirstOrDefault();
                if (viewPlugin != null)
                {
                    if (!viewPlugin.CustomZoomLevelSets.ContainsKey(saveCustomSetWindow.CustomSetName))
                        viewPlugin.CustomZoomLevelSets.Add(saveCustomSetWindow.CustomSetName, viewModel.ZoomLevelSetViewModel.Select(z => z.Scale).ToList());
                    else System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ZoomLevelSetWindowNameExistedText"), GisEditor.LanguageManager.GetStringResource("ZoomLevelSetWindowDuplicateNameCaption"));
                }
            }
        }

        [Obfuscation]
        private void LoadCustomSetClick(object sender, RoutedEventArgs e)
        {
            LoadCustomSetWindow loadCustomSetWindow = new LoadCustomSetWindow();
            if (loadCustomSetWindow.ShowDialog().GetValueOrDefault())
            {
                viewModel.ZoomLevelSetViewModel.Clear();
                for (int i = 0; i < loadCustomSetWindow.SelectedScales.Count; i++)
                {
                    viewModel.ZoomLevelSetViewModel.Add(new ZoomLevelViewModel(i, loadCustomSetWindow.SelectedScales[i]));
                }
            }
        }

        [Obfuscation]
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            var viewPlugin = GisEditor.UIManager.GetActiveUIPlugins<ViewUIPlugin>().FirstOrDefault();
            if (viewPlugin != null) GisEditor.InfrastructureManager.SaveSettings(viewPlugin);
        }

        [Obfuscation]
        private void ApplyClick(object sender, RoutedEventArgs e)
        {
            bool zoomLevelSetChanged = IsZoomLevelSetChanged();
            if (zoomLevelSetChanged)
            {
                if (System.Windows.Forms.DialogResult.Yes == System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ZoomLevelSetWindowZoomLevelModifiedText"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"), System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning))
                    ApplyAction(viewModel.ZoomLevelSetViewModel.Select(z => z.Scale).ToList());
            }
            else System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ZoomLevelSetWindowModifyFirstText"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"), System.Windows.Forms.MessageBoxButtons.OK);
        }

        private void EditZoomLevel(double newScale)
        {
            viewModel.SelectedZoomLevel.Scale = newScale;
            var sortedArray = viewModel.ZoomLevelSetViewModel.OrderByDescending(z => z.Scale).ToArray();
            viewModel.ZoomLevelSetViewModel.Clear();
            for (int i = 0; i < sortedArray.Length; i++)
            {
                var zoomLevelModel = sortedArray[i];
                zoomLevelModel.ZoomLevelIndex = i;
                viewModel.ZoomLevelSetViewModel.Add(zoomLevelModel);
            }
        }

        private void ChangeExtent()
        {
            int zoomLevelIndex = GisEditor.ActiveMap.GetSnappedZoomLevelIndex(GisEditor.ActiveMap.CurrentScale);
            if (viewModel.ZoomLevelSetViewModel.Count < GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Count && zoomLevelIndex >= viewModel.ZoomLevelSetViewModel.Count)
            {
                GisEditor.ActiveMap.CurrentExtent = GetRectangleShape(GisEditor.ActiveMap.CurrentExtent.GetCenterPoint(), viewModel.ZoomLevelSetViewModel.Last().Scale);
                GisEditor.ActiveMap.Refresh();
            }
        }

        private RectangleShape GetRectangleShape(PointShape center, double scale)
        {
            double resolution = MapUtils.GetResolutionFromScale(scale, GisEditor.ActiveMap.MapUnit);
            double left = center.X - resolution * GisEditor.ActiveMap.ActualWidth * .5;
            double top = center.Y + resolution * GisEditor.ActiveMap.ActualHeight * .5;
            double right = center.X + resolution * GisEditor.ActiveMap.ActualWidth * .5;
            double bottom = center.Y - resolution * GisEditor.ActiveMap.ActualHeight * .5;
            return new RectangleShape(left, top, right, bottom);
        }

        private bool IsZoomLevelSetChanged()
        {
            bool zoomLevelSetChanged = false;

            var newScales = viewModel.ZoomLevelSetViewModel.Select(z => z.Scale).ToList();
            var originalScales = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Select(z => z.Scale).ToList();
            if (newScales.Count == originalScales.Count)
            {
                for (int i = 0; i < newScales.Count; i++)
                {
                    if (newScales[i] != originalScales[i])
                    {
                        zoomLevelSetChanged = true;
                        break;
                    }
                }
            }
            else { zoomLevelSetChanged = true; }
            return zoomLevelSetChanged;
        }

    }
}
