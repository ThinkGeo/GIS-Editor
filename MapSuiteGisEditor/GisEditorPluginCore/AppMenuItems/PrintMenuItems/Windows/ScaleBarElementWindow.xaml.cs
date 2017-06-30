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
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ScaleBarElementWindow.xaml
    /// </summary>
    public partial class ScaleBarElementWindow : Window
    {
        private ScaleBarElementViewModel viewModel;

        public ScaleBarElementWindow(MapPrinterLayer mapPrinterLayer)
        {
            InitializeComponent();
            viewModel = new ScaleBarElementViewModel(mapPrinterLayer);
            DataContext = viewModel;

            HelpContainer.Content = HelpResourceHelper.GetHelpButton("PrintMapScaleBarHelp", HelpButtonMode.NormalButton);
        }

        [Obfuscation]
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        [Obfuscation]
        private void OKClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        internal void SetProperties(PrinterLayer printerLayer)
        {
            ScaleBarPrinterLayer scaleBarPrinterLayer = printerLayer as ScaleBarPrinterLayer;
            if (scaleBarPrinterLayer != null)
            {
                viewModel.Background = scaleBarPrinterLayer.BackgroundMask;
                viewModel.Color = scaleBarPrinterLayer.BarBrush;
                viewModel.AlternatingColor = scaleBarPrinterLayer.AlternateBarBrush;

                switch (scaleBarPrinterLayer.TextStyle.NumericFormat)
                {
                    case "C":
                        viewModel.SelectedNumericFormatType = ScaleNumericFormatType.Currency;
                        break;
                    case "D":
                        viewModel.SelectedNumericFormatType = ScaleNumericFormatType.Decimal;
                        break;
                    case "E":
                        viewModel.SelectedNumericFormatType = ScaleNumericFormatType.Scientific;
                        break;
                    case "F":
                        viewModel.SelectedNumericFormatType = ScaleNumericFormatType.FixedPoint;
                        break;
                    case "G":
                        viewModel.SelectedNumericFormatType = ScaleNumericFormatType.General;
                        break;
                    case "N":
                        viewModel.SelectedNumericFormatType = ScaleNumericFormatType.Number;
                        break;
                    case "P":
                        viewModel.SelectedNumericFormatType = ScaleNumericFormatType.Percent;
                        break;
                    case "R":
                        viewModel.SelectedNumericFormatType = ScaleNumericFormatType.RoundTrip;
                        break;
                    case "X":
                        viewModel.SelectedNumericFormatType = ScaleNumericFormatType.Hexadecimal;
                        break;
                    case "":
                    default:
                        viewModel.SelectedNumericFormatType = ScaleNumericFormatType.None;
                        break;
                }

                viewModel.SelectedUnitSystem = scaleBarPrinterLayer.UnitFamily;
                viewModel.DragMode = scaleBarPrinterLayer.DragMode;
                viewModel.ResizeMode = scaleBarPrinterLayer.ResizeMode;
            }
        }
    }
}