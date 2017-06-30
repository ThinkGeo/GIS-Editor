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
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class KmlConfigureWindow : Window
    {
        public KmlConfigureWindow()
        {
            InitializeComponent();

            OkButton.IsEnabled = false;
            ZHeightNumeric.Value = GetAltitudeValue();
            ZHeightNumeric.IsEnabled = false;
            KmlParameter = new KmlParameter();
        }

        public KmlParameter KmlParameter { get; set; }

        [Obfuscation]
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sf = new SaveFileDialog();
            sf.Filter = "(*.kml)|*.kml|(*.kmz)|*.kmz";
            sf.FileName = string.Format("{0}-{1}", "KmlExportFile", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
            if (sf.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (Path.GetExtension(sf.FileName).ToUpper() == ".KML")
                {
                    PathTextBox.Text = sf.FileName;
                }
                else if (Path.GetExtension(sf.FileName).ToUpper() == ".KMZ")
                {
                    PathTextBox.Text = sf.FileName;
                }
            }
        }

        [Obfuscation]
        private void Kml3dCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ZHeightNumeric.IsEnabled = true;
        }

        [Obfuscation]
        private void Kml3dCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ZHeightNumeric.IsEnabled = false;
        }

        [Obfuscation]
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (((ComboBoxItem)UnitBox.SelectedItem).Content.ToString() == "Meter")
            {
                KmlParameter.ZHeight = (int)ZHeightNumeric.Value;
            }
            else
            {
                double result = Conversion.ConvertMeasureUnits((double)ZHeightNumeric.Value, DistanceUnit.Feet, DistanceUnit.Meter);
                KmlParameter.ZHeight = (int)result;
            }
            KmlParameter.PathFileName = PathTextBox.Text;
            KmlParameter.Is3DKml = Kml3dCheckBox.IsChecked == true ? true : false;
            SaveAltitudeValue(KmlParameter.ZHeight);
            DialogResult = true;
        }

        [Obfuscation]
        private void PathTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            OkButton.IsEnabled = !string.IsNullOrEmpty(PathTextBox.Text);
        }

        private void SaveAltitudeValue(int value)
        {
            string path = GisEditor.InfrastructureManager.TemporaryPath;
            path = Path.Combine(path, "Altitude.txt");
            File.WriteAllText(path, value.ToString());
        }

        private int GetAltitudeValue()
        {
            int result = 100;
            string path = GisEditor.InfrastructureManager.TemporaryPath;
            path = Path.Combine(path, "Altitude.txt");
            if (File.Exists(path))
            {
                string content = File.ReadAllText(path);
                int value = 100;
                if (content.Contains("."))
                {
                    content = content.Substring(0, content.IndexOf('.'));
                }
                if (int.TryParse(content, out value))
                {
                    result = value;
                }
            }
            return result;
        }
    }
}