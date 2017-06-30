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


using Microsoft.Windows.Controls.Ribbon;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class MeasureRibbonGroup : RibbonGroup
    {
        private bool isKeyEventHooked;
        private MeasureRibbonGroupViewModel viewModel;

        public MeasureRibbonGroup()
        {
            InitializeComponent();
            viewModel = new MeasureRibbonGroupViewModel();
            DataContext = viewModel;
            Loaded += new RoutedEventHandler(MeasureRibbonGroup_Loaded);
        }

        public MeasureRibbonGroupViewModel ViewModel
        {
            get { return viewModel; }
        }

        public void SynchronizeState(WpfMap currentMap)
        {
            var measureOverlay = viewModel.MeasureOverlay;
            if (measureOverlay != null)
            {
                switch (measureOverlay.TrackMode)
                {
                    case TrackMode.Rectangle:
                        rectangleMeasure.IsChecked = true;
                        break;
                    case TrackMode.Square:
                        squareMeasure.IsChecked = true;
                        break;
                    case TrackMode.Ellipse:
                        ellipseMeasure.IsChecked = true;
                        break;
                    case TrackMode.Circle:
                        circleMeasure.IsChecked = true;
                        break;
                    case TrackMode.Polygon:
                        polygonMeasure.IsChecked = true;
                        break;
                    case TrackMode.Line:
                        lineMeasure.IsChecked = true;
                        break;
                    case TrackMode.Custom:
                        selectMeasure.IsChecked = true;
                        break;
                    case TrackMode.None:
                        TurnOffRadioButtons();
                        break;
                }

                if (measureOverlay.MeasureCustomeMode == MeasureCustomeMode.Move
                    && measureOverlay.TrackMode == TrackMode.Custom)
                {
                    TurnOffRadioButtons();
                    move.IsChecked = true;
                }

                viewModel.SelectedPolygonTrackMode = measureOverlay.PolygonTrackMode;
                if (measureOverlay.ShapeLayer.MapShapes.Count > 0 && currentMap.ActualWidth > 0 && currentMap.ActualHeight > 0) currentMap.Refresh(measureOverlay);
                DataContext = null;
                DataContext = viewModel;
                viewModel.UpdateStylePreview();
            }
        }

        private void TurnOffRadioButtons()
        {
            selectMeasure.IsChecked = false;
            rectangleMeasure.IsChecked = false;
            squareMeasure.IsChecked = false;
            ellipseMeasure.IsChecked = false;
            circleMeasure.IsChecked = false;
            polygonMeasure.IsChecked = false;
            lineMeasure.IsChecked = false;
            move.IsChecked = false;
        }

        [Obfuscation]
        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            RibbonTab tab = Parent as RibbonTab;

            if (tab != null && tab.IsSelected && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (e.Key == Key.Z && viewModel.UndoCommand.CanExecute(null))
                {
                    viewModel.UndoCommand.Execute(null);
                }
                else if (e.Key == Key.Y && viewModel.RedoCommand.CanExecute(null))
                {
                    viewModel.RedoCommand.Execute(null);
                }
            }
        }

        private void MeasureRibbonGroup_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isKeyEventHooked && Application.Current != null)
            {
                Application.Current.MainWindow.KeyDown += new System.Windows.Input.KeyEventHandler(MainWindow_KeyDown);
                isKeyEventHooked = true;
            }
        }
    }
}
