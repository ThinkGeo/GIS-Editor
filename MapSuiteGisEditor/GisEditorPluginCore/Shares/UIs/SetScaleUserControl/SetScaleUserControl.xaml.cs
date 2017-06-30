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
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for SetScaleUserControl.xaml
    /// </summary>
    public partial class SetScaleUserControl : UserControl
    {
        public static readonly DependencyProperty ScaleProperty =
   DependencyProperty.Register("Scale", typeof(double), typeof(SetScaleUserControl), new PropertyMetadata(0d));

        public SetScaleUserControl()
        {
            InitializeComponent();

            var units = Enum.GetValues(typeof(DistanceUnit));
            foreach (var item in units)
            {
                UnitComboBox.Items.Add(item);
            }
            UnitComboBox.SelectedItem = DistanceUnit.Feet;
        }

        public double Scale
        {
            get
            {
                return (double)GetValue(ScaleProperty);
            }
            set
            {
                SetValue(ScaleProperty, value);
                double temp = Conversion.ConvertMeasureUnits(Scale, DistanceUnit.Inch, (DistanceUnit)UnitComboBox.SelectedItem); ;
                ScaleComboBox.Text = temp.ToString();
            }
        }

        private void UnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ScaleComboBox.Text))
            {
                Scale = CalculateScale();
            }
        }

        private void ScaleComboBox_DropDownOpened(object sender, EventArgs e)
        {
            ScaleComboBox.Items.Clear();
            foreach (var zoomLevel in GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels)
            {
                Scale = CalculateScale();
            }
        }

        private void ScaleComboBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!string.IsNullOrEmpty(ScaleComboBox.Text))
            {
                Scale = CalculateScale();
            }
        }

        private double CalculateScale()
        {
            double zoomToScale = Conversion.ConvertMeasureUnits(double.Parse(ScaleComboBox.Text), (DistanceUnit)UnitComboBox.SelectedItem, DistanceUnit.Inch);
            return Math.Round(zoomToScale, 6);
        }
    }
}