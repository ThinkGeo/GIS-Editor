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


using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    [Obfuscation]
    public class FeatureLayerPropertiesUserControlViewModel : ViewModelBase
    {
        private static readonly string PleaseSelectText = "-- Please Select --";
        private FeatureLayer targetFeatureLayer;
        private ObservableCollection<LayerInformationItem> layerInformation;
        private string featureIDColumn;

        public FeatureLayerPropertiesUserControlViewModel(FeatureLayer featureLayer)
        {
            // TODO: Complete member initialization
            this.targetFeatureLayer = featureLayer;
            this.layerInformation = new ObservableCollection<LayerInformationItem>();
            Initialize();
        }

        internal ObservableCollection<LayerInformationItem> LayerInformation
        {
            get { return layerInformation; }
        }

        public string FeatureIDColumn
        {
            get { return featureIDColumn; }
            set
            {
                featureIDColumn = value;
                RaisePropertyChanged(() => FeatureIDColumn);
            }
        }

        public FeatureLayer TargetFeatureLayer
        {
            get { return targetFeatureLayer; }
        }

        private void Initialize()
        {
            layerInformation.Add(new LayerInformationItem() { Key = "Layer Name", Value = targetFeatureLayer.Name });
            layerInformation.Add(new LayerInformationItem() { Key = "Columns Count", Value = "Loading..." });
            layerInformation.Add(new LayerInformationItem() { Key = "Rows Count", Value = "Loading..." });
            layerInformation.Add(new LayerInformationItem() { Key = "Upper Left X:", Value = "Loading..." });
            layerInformation.Add(new LayerInformationItem() { Key = "Upper Left Y:", Value = "Loading..." });
            layerInformation.Add(new LayerInformationItem() { Key = "Lower Right X:", Value = "Loading..." });
            layerInformation.Add(new LayerInformationItem() { Key = "Lower Right Y:", Value = "Loading..." });
            layerInformation.Add(new LayerInformationItem() { Key = "Feature ID Column", Value = new List<string> { "Loading..." } });

            Task.Factory.StartNew(() =>
            {
                targetFeatureLayer.SafeProcess(() =>
                {
                    LayerInformationItem columnCountItem = GetLayerInformationItem("Columns Count");
                    try
                    {
                        columnCountItem.Value = targetFeatureLayer.QueryTools.GetColumns().Count.ToString();
                    }
                    catch (Exception ex)
                    {
                        columnCountItem.Value = ex.Message;
                    }

                    LayerInformationItem rowCountItem = GetLayerInformationItem("Rows Count");
                    try
                    {
                        rowCountItem.Value = targetFeatureLayer.QueryTools.GetCount().ToString();
                    }
                    catch (Exception ex)
                    {
                        rowCountItem.Value = ex.Message;
                    }

                    LayerInformationItem upperXItem = GetLayerInformationItem("Upper Left X:");
                    LayerInformationItem upperYItem = GetLayerInformationItem("Upper Left Y:");
                    LayerInformationItem lowerXItem = GetLayerInformationItem("Lower Right X:");
                    LayerInformationItem lowerYItem = GetLayerInformationItem("Lower Right Y:");
                    if (targetFeatureLayer.HasBoundingBox)
                    {
                        try
                        {
                            RectangleShape boundingBox = targetFeatureLayer.GetBoundingBox();
                            upperXItem.Value = boundingBox.UpperLeftPoint.X.ToString("N4");
                            upperYItem.Value = boundingBox.UpperLeftPoint.Y.ToString("N4");
                            lowerXItem.Value = boundingBox.LowerRightPoint.X.ToString("N4");
                            lowerYItem.Value = boundingBox.LowerRightPoint.Y.ToString("N4");
                        }
                        catch (Exception ex)
                        {
                            upperXItem.Value = ex.Message;
                            upperYItem.Value = ex.Message;
                            lowerXItem.Value = ex.Message;
                            lowerYItem.Value = ex.Message;
                        }
                    }
                    else
                    {
                        upperXItem.Value = double.NaN.ToString();
                        upperYItem.Value = double.NaN.ToString();
                        lowerXItem.Value = double.NaN.ToString();
                        lowerYItem.Value = double.NaN.ToString();
                    }

                    LayerInformationItem featureIdColumnItem = GetLayerInformationItem("Feature ID Column");
                    List<string> columns = targetFeatureLayer.FeatureSource.GetColumns().Select(c => c.ColumnName).ToList();
                    columns.Insert(0, PleaseSelectText);
                    featureIdColumnItem.Value = columns;
                    if (string.IsNullOrEmpty(FeatureIDColumn))
                    {
                        FeatureIDColumn = GetFeatureIdColumn(targetFeatureLayer);
                    }
                    if (string.IsNullOrEmpty(FeatureIDColumn) && columns.Count > 0)
                    {
                        FeatureIDColumn = columns[0];
                    }
                });
            });
        }

        private LayerInformationItem GetLayerInformationItem(string key)
        {
            return layerInformation.FirstOrDefault(f => f.Key == key);
        }

        private static string GetFeatureIdColumn(FeatureLayer featureLayer)
        {
            string featureIdColumn = string.Empty;
            LayerPlugin layerPlugin = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).FirstOrDefault();
            if (layerPlugin != null)
            {
                Uri uri = layerPlugin.GetUri(featureLayer);
                if (uri != null && GisEditor.LayerManager.FeatureIdColumnNames.ContainsKey(uri.ToString()))
                {
                    featureIdColumn = GisEditor.LayerManager.FeatureIdColumnNames[uri.ToString()];
                    GisEditor.LayerManager.FeatureIdColumnNames.Remove(uri.ToString());
                    GisEditor.LayerManager.FeatureIdColumnNames[featureLayer.FeatureSource.Id] = featureIdColumn;
                }
                else if (GisEditor.LayerManager.FeatureIdColumnNames.ContainsKey(featureLayer.FeatureSource.Id))
                {
                    featureIdColumn = GisEditor.LayerManager.FeatureIdColumnNames[featureLayer.FeatureSource.Id];
                }
                else if (GisEditor.LayerManager.FeatureIdColumnNames.Count == 0)
                {
                    if (string.IsNullOrEmpty(featureIdColumn)
                        && featureLayer.FeatureSource.IsOpen)
                    {
                        var apnColumn = featureLayer.FeatureSource.GetColumns().FirstOrDefault(c => { return c.ColumnName == "APN"; });
                        if (apnColumn != null) featureIdColumn = "APN";
                    }
                }
            }

            return featureIdColumn;
        }

        private IEnumerable<Encoding> GetAllEncodings()
        {
            var allEncodings = Encoding.GetEncodings().Select(encodingInfo => encodingInfo.GetEncoding());
            return allEncodings;
        }
    }

    [Obfuscation]
    internal class LayerInformationItem : ViewModelBase
    {
        private object itemValue;

        public string Key { get; set; }

        public object Value
        {
            get { return itemValue; }
            set
            {
                itemValue = value;
                RaisePropertyChanged(() => Value);
            }
        }
    }
}