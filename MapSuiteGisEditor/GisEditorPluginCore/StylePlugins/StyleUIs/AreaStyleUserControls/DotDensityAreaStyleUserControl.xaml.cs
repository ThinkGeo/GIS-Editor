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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class DotDensityAreaStyleUserControl : StyleUserControl
    {
        private const double maxDotsToDraw = 2000;
        private DotDensityStyle style;
        private DotDensityStyleViewModel dotDensityStyleViewModel;

        public DotDensityAreaStyleUserControl(DotDensityStyle style, StyleBuilderArguments requiredValues)
        {
            InitializeComponent();
            this.style = style;
            this.RequiredValues = requiredValues;

            if (RequiredValues.FeatureLayer == null || RequiredValues.FeatureLayer is InMemoryFeatureLayer)
            {
                viewDataButton.IsEnabled = false;
            }
            var ratio = Math.Round(1 / style.PointToValueRatio);
            dotDensityStyleViewModel = new DotDensityStyleViewModel(style, requiredValues);
            if (ratio != 1)
                dotDensityStyleViewModel.PointValueRatioY = ratio;
            DataContext = dotDensityStyleViewModel;

            string helpUri = GisEditor.LanguageManager.GetStringResource("DotDensityAreaStyleHelp");
            if (!string.IsNullOrEmpty(helpUri))
            {
                HelpUri = new Uri(helpUri);
            }
        }

        public StyleBuilderArguments RequiredValues { get; set; }

        internal DotDensityStyleViewModel ViewModel { get { return dotDensityStyleViewModel; } }

        protected override bool ValidateCore()
        {
            string errorMessage = GetErrorMessage();
            if (!string.IsNullOrEmpty(errorMessage))
            {
                System.Windows.Forms.MessageBox.Show(errorMessage, "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                return false;
            }
            else return true;
        }

        private string GetErrorMessage()
        {
            StringBuilder errorMessage = new StringBuilder();

            if (string.IsNullOrEmpty(style.ColumnName))
            {
                errorMessage.AppendLine("Column name cannot be empty.");
            }

            return errorMessage.ToString();
        }

        [Obfuscation]
        private void viewDataButton_Click(object sender, RoutedEventArgs e)
        {
            DataViewerUserControl content = new DataViewerUserControl();
            content.ShowDialog();
        }

        [Obfuscation]
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (columnNameComboBox.SelectedItem != null)
            {
                BusyPanel.IsBusy = true;
                KeyValuePair<string, string> selectedItem = (KeyValuePair<string, string>)columnNameComboBox.SelectedItem;

                Task.Factory.StartNew(tmpInfo =>
                {
                    object[] tmpLayerInfo = (object[])tmpInfo;
                    FeatureLayer tmpLayer = (FeatureLayer)tmpLayerInfo[0];
                    string tmpColumnName = (string)tmpLayerInfo[1];
                    if (tmpLayer != null)
                    {
                        style.PointToValueRatio = GetRecommendPointToValueRatio(tmpLayer, tmpColumnName);
                        ViewModel.RecommendPointValueRatio = style.PointToValueRatio;

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ViewModel.PointValueRatioY = Math.Round(1 / ViewModel.RecommendPointValueRatio);
                            BusyPanel.IsBusy = false;
                        }));
                    }
                }, new object[] { RequiredValues.FeatureLayer, selectedItem.Value });
            }
        }

        /// <summary>
        /// Points to Draw      Elapsed MS
        /// 10000               605
        /// 20000               998
        /// 50000               2658
        /// 100000              4980
        /// </summary>
        private static double GetRecommendPointToValueRatio(FeatureLayer layer, string columnName)
        {
            int pointsToDraw = 0;

            // better performance.
            if (layer is ShapeFileFeatureLayer)
            {
                var shpLayer = (ShapeFileFeatureLayer)layer;
                var shpFName = shpLayer.ShapePathFilename;
                var dbfFName = Path.ChangeExtension(shpFName, ".dbf");
                if (File.Exists(dbfFName))
                {
                    using (GeoDbf geoDbf = new GeoDbf(dbfFName, GeoFileReadWriteMode.Read))
                    {
                        geoDbf.Open();
                        for (int i = 1; i <= geoDbf.RecordCount; i++)
                        {
                            string fieldValue = geoDbf.ReadFieldAsString(i, columnName);
                            try
                            {
                                pointsToDraw += (int)Convert.ToDouble(fieldValue);
                            }
                            catch (Exception ex)
                            {
                                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                            }
                        }
                        geoDbf.Close();
                    }
                }
            }
            else
            {
                IEnumerable<Feature> features = null;
                layer.SafeProcess(() => { features = layer.QueryTools.GetAllFeatures(new string[] { columnName }); });
                pointsToDraw = features.Sum(tmpFeature =>
                {
                    try { return (int)Convert.ToDouble(tmpFeature.ColumnValues[columnName]); }
                    catch (Exception ex)
                    {
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                        return 0;
                    }
                });
            }

            double pointToValueRatio = 1d;
            if (maxDotsToDraw < pointsToDraw)
            {
                pointToValueRatio = (double)maxDotsToDraw / (double)pointsToDraw;
            }

            return pointToValueRatio;
        }
    }
}