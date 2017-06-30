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


using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for SetMargins.xaml
    /// </summary>
    public partial class SetMarginsWindow : Window
    {
        public SetMarginsWindow()
        {
            InitializeComponent();
            SetIncrement((decimal)0.1);
            cbxUnit.ItemsSource = new Collection<PrintingUnit>() { PrintingUnit.Inch, PrintingUnit.Centimeter };
            cbxUnit.SelectedItem = PrintingUnit.Inch;
        }

        public double MarginBottom
        {
            get { return (double)txtBottom.Value; }
            set
            {
                txtBottom.Value = (decimal)value;
            }
        }

        public double MarginLeft
        {
            get { return (double)txtLeft.Value; }
            set
            {
                txtLeft.Value = (decimal)value;
            }
        }


        public double MarginRight
        {
            get { return (double)txtRight.Value; }
            set
            {
                txtRight.Value = (decimal)value;
            }
        }

        public double MarginTop
        {
            get { return (double)txtTop.Value; }
            set
            {
                txtTop.Value = (decimal)value;
            }
        }

        public PrintingUnit ResultUnit
        {
            get { return (PrintingUnit)cbxUnit.SelectedItem; }
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

        [Obfuscation]
        private void CbxUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                if (ResultUnit == PrintingUnit.Inch)
                {
                    MarginLeft = Math.Round(PrinterHelper.ConvertLength(MarginLeft, PrintingUnit.Centimeter, PrintingUnit.Inch), 1);
                    MarginTop = Math.Round(PrinterHelper.ConvertLength(MarginTop, PrintingUnit.Centimeter, PrintingUnit.Inch), 1);
                    MarginRight = Math.Round(PrinterHelper.ConvertLength(MarginRight, PrintingUnit.Centimeter, PrintingUnit.Inch), 1);
                    MarginBottom = Math.Round(PrinterHelper.ConvertLength(MarginBottom, PrintingUnit.Centimeter, PrintingUnit.Inch), 1);
                }
                else
                {
                    MarginLeft = Math.Round(PrinterHelper.ConvertLength(MarginLeft, PrintingUnit.Inch, PrintingUnit.Centimeter), 1);
                    MarginTop = Math.Round(PrinterHelper.ConvertLength(MarginTop, PrintingUnit.Inch, PrintingUnit.Centimeter), 1);
                    MarginRight = Math.Round(PrinterHelper.ConvertLength(MarginRight, PrintingUnit.Inch, PrintingUnit.Centimeter), 1);
                    MarginBottom = Math.Round(PrinterHelper.ConvertLength(MarginBottom, PrintingUnit.Inch, PrintingUnit.Centimeter), 1);
                }
            }
        }

        private void SetIncrement(decimal value)
        {
            txtLeft.Increment = value;
            txtTop.Increment = value;
            txtRight.Increment = value;
            txtBottom.Increment = value;
        }
    }
}