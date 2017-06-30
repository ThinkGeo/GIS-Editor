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
using System.Reflection;
using System.Windows;
using System.IO;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ConfigTinyGeoFileUserControl.xaml
    /// </summary>
    public partial class ConfigTinyGeoFileUserControl : CreateFeatureLayerUserControl
    {
        public ConfigTinyGeoFileUserControl(FeatureLayer featureLayer)
        {
            InitializeComponent();
            lbxFeatureLayers.ItemsSource = GisEditor.ActiveMap.GetFeatureLayers();
            txtOutput.Text = ConfigShapeFileViewModel.GetDefaultOutputPath();
        }

        protected override string InvalidMessageCore
        {
            get
            {
                string message = string.Empty;
                if (string.IsNullOrEmpty(txtLayerName.Text))
                {
                    message = "Layer name can't be empty.";
                }
                if (!ValidateFileName(txtLayerName.Text))
                {
                    message = "A file name can't contain any of the following characters:" + Environment.NewLine + "\\ / : * ? \" < > |";
                }
                else if (lbxFeatureLayers.SelectedItem == null)
                {
                    message = "Please select a feature layer.";
                }
                else if (!Directory.Exists(txtOutput.Text))
                {
                    message = "Folder path is invalid.";
                }
                return message;
            }
        }

        protected override ConfigureFeatureLayerParameters GetFeatureLayerInfoCore()
        {
            ConfigureFeatureLayerParameters featureLayerInfo = new ConfigureFeatureLayerParameters();
            FeatureLayer featureLayer = lbxFeatureLayers.SelectedItem as FeatureLayer;
            if (featureLayer != null)
            {
                featureLayerInfo.CustomData["SourceLayer"] = featureLayer;
                featureLayerInfo.WellKnownType = featureLayer.FeatureSource.GetFirstFeaturesWellKnownType();
            }
            featureLayerInfo.LayerUri = new Uri(string.Format(@"{0}\{1}.tgeo", txtOutput.Text, txtLayerName.Text));
            return featureLayerInfo;
        }

        [Obfuscation]
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            FolderHelper.OpenFolderBrowserDialog((tmpDialog, tmpResult) =>
            {
                if (tmpResult == System.Windows.Forms.DialogResult.OK || tmpResult == System.Windows.Forms.DialogResult.Yes)
                {
                    txtOutput.Text = tmpDialog.SelectedPath;
                }
            });
        }

        private bool ValidateFileName(string fileName)
        {
            return !(fileName.Contains("\\")
                || fileName.Contains("/")
                || fileName.Contains(":")
                || fileName.Contains("*")
                || fileName.Contains("?")
                || fileName.Contains("\"")
                || fileName.Contains("<")
                || fileName.Contains(">")
                || fileName.Contains("|")
                );
        }
    }
}
