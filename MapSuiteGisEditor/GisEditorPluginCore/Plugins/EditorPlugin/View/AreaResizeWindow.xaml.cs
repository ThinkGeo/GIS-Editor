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
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for AreaResizeWindow.xaml
    /// </summary>
    public partial class AreaResizeWindow : Window
    {
        private static AreaUnit defaultAreaUnit = AreaUnit.SquareMeters;

        private string tempAcreage;
        private Collection<AreaUnit> distanceUnits;
        private AreaBaseShape areaShape;

        public AreaResizeWindow(AreaBaseShape areaShape)
        {
            InitializeComponent();

            this.areaShape = areaShape;

            distanceUnits = new Collection<AreaUnit>();
            double acreage = areaShape.GetArea(GisEditor.ActiveMap.MapUnit, defaultAreaUnit);

            origalAcreageTb.Text = acreage.ToString();
            targetAcreageTb.Text = acreage.ToString();
            tempAcreage = targetAcreageTb.Text;

            foreach (AreaUnit item in Enum.GetValues(typeof(AreaUnit)))
            {
                distanceUnits.Add(item);
            }

            cmxDistanceUnit.ItemsSource = distanceUnits;
            cmxDistanceUnit.SelectedItem = defaultAreaUnit;
        }

        public static AreaUnit DefaultAreaUnit
        {
            get
            {
                return defaultAreaUnit;
            }
        }

        public double OriginalAcreage
        {
            get { return double.Parse(origalAcreageTb.Text); }
        }

        public double ResultAcreage
        {
            get { return double.Parse(targetAcreageTb.Text); }
        }

        private void TargetAcreageTb_TextChanged(object sender, TextChangedEventArgs e)
        {
            double result = double.NaN;
            if (!double.TryParse(targetAcreageTb.Text, out result))
            {
                targetAcreageTb.Text = tempAcreage;
                targetAcreageTb.SelectionStart = targetAcreageTb.Text.Length;
            }
            else
            {
                tempAcreage = targetAcreageTb.Text;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void cmxDistanceUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AreaUnit areaUnit = (AreaUnit)cmxDistanceUnit.SelectedItem;
            defaultAreaUnit = areaUnit;
            double acreage = areaShape.GetArea(GisEditor.ActiveMap.MapUnit, areaUnit);
            origalAcreageTb.Text = acreage.ToString();
            targetAcreageTb.Text = acreage.ToString();
        }
    }
}