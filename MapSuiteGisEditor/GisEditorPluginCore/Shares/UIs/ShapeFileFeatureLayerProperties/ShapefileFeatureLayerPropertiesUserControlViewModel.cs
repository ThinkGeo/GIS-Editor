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
using System.Text;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ShapefileFeatureLayerPropertiesUserControlViewModel : ViewModelBase
    {
        public static readonly string PleaseSelectText = "-- Please Select --";

        private ShapeFileFeatureLayer targetFeatureLayer;
        private Dictionary<string, object> layerInformation;
        private Encoding selectedEncoding;
        private bool isEncodingPending;
        private string featureIDColumn;
        private KeyValuePair<string, string> featureIDColumnAlias;
        private Dictionary<string, string> columns;

        public ShapefileFeatureLayerPropertiesUserControlViewModel(ShapeFileFeatureLayer featureLayer)
        {
            // TODO: Complete member initialization
            this.targetFeatureLayer = featureLayer;
            columns = new Dictionary<string, string>();
            Refresh();
        }

        public ShapeFileFeatureLayer TargetFeatureLayer
        {
            get { return targetFeatureLayer; }
        }

        public Dictionary<string, object> LayerInformation
        {
            get { return layerInformation; }
            private set
            {
                layerInformation = value;
                RaisePropertyChanged(() => LayerInformation);
            }
        }

        public Encoding SelectedEncoding
        {
            get { return selectedEncoding; }
            set
            {
                selectedEncoding = value;
                IsEncodingPending = true;
                RaisePropertyChanged(() => SelectedEncoding);
            }
        }

        public string FeatureIDColumn
        {
            get { return featureIDColumn; }
            set
            {
                if (featureIDColumn != value)
                {
                    featureIDColumn = value;
                    RaisePropertyChanged(() => FeatureIDColumn);
                    FeatureIDColumnAlias = columns.FirstOrDefault(c => c.Key == FeatureIDColumn);
                }
            }
        }

        public KeyValuePair<string, string> FeatureIDColumnAlias
        {
            get { return featureIDColumnAlias; }
            set
            {
                if (value.Key != featureIDColumnAlias.Key || value.Value != featureIDColumnAlias.Value)
                {
                    featureIDColumnAlias = value;
                    FeatureIDColumn = value.Key;
                    RaisePropertyChanged(() => FeatureIDColumnAlias);
                }
            }
        }

        public bool IsEncodingPending
        {
            get { return isEncodingPending; }
            set
            {
                isEncodingPending = value;
                RaisePropertyChanged(() => IsEncodingPending);
            }
        }

        private void Refresh()
        {
            Dictionary<string, object> information = new Dictionary<string, object>();
            targetFeatureLayer.SafeProcess(() =>
            {
                string fileName = targetFeatureLayer.ShapePathFilename.Replace('/', '\\');
                information.Add("File Name", fileName);
                information.Add("File Size", string.Format("{0} bytes", new FileInfo(targetFeatureLayer.ShapePathFilename).Length));

                string indexPathFileName = targetFeatureLayer.IndexPathFilename.Replace('/', '\\');
                information.Add("Index File", indexPathFileName);
                information.Add("ShapeFile Type", targetFeatureLayer.GetShapeFileType().ToString());
                information.Add("Layer Name", targetFeatureLayer.Name);
                information.Add("Columns Count", targetFeatureLayer.QueryTools.GetColumns().Count.ToString());
                information.Add("Rows Count", targetFeatureLayer.GetRecordCount().ToString());


                if (targetFeatureLayer.HasBoundingBox)
                {
                    RectangleShape boundingBox = targetFeatureLayer.GetBoundingBox();
                    information.Add("Upper Left X:", boundingBox.UpperLeftPoint.X.ToString("N4"));
                    information.Add("Upper Left Y:", boundingBox.UpperLeftPoint.Y.ToString("N4"));
                    information.Add("Lower Right X:", boundingBox.LowerRightPoint.X.ToString("N4"));
                    information.Add("Lower Right Y:", boundingBox.LowerRightPoint.Y.ToString("N4"));
                }
                else
                {
                    information.Add("Upper Left X:", double.NaN.ToString());
                    information.Add("Upper Left Y:", double.NaN.ToString());
                    information.Add("Lower Right X:", double.NaN.ToString());
                    information.Add("Lower Right Y:", double.NaN.ToString());
                }

                if (!targetFeatureLayer.Name.Equals("TempLayer"))
                {
                    information.Add("Encoding", GetAllEncodings());
                    selectedEncoding = targetFeatureLayer.Encoding;
                    targetFeatureLayer.SafeProcess(() =>
                    {
                        columns.Add(PleaseSelectText, PleaseSelectText);
                        foreach (var item in targetFeatureLayer.FeatureSource.GetColumns())
                        {
                            columns.Add(item.ColumnName, targetFeatureLayer.FeatureSource.GetColumnAlias(item.ColumnName));
                        }

                        information.Add("Feature ID Column", columns);
                        if (string.IsNullOrEmpty(FeatureIDColumn) && columns.Count > 0)
                        {
                            FeatureIDColumnAlias = columns.FirstOrDefault();
                        }

                        if (!string.IsNullOrEmpty(FeatureIDColumn) && columns.Count > 0)
                        {
                            FeatureIDColumnAlias = columns.FirstOrDefault(c => c.Key == FeatureIDColumn);
                        }
                    });
                }
            });

            LayerInformation = information;
        }

        private IEnumerable<Encoding> GetAllEncodings()
        {
            var allEncodings = Encoding.GetEncodings().Select(encodingInfo => encodingInfo.GetEncoding());
            return allEncodings;
        }

        internal void ChangeEncoding()
        {
            if (targetFeatureLayer != null && targetFeatureLayer is ShapeFileFeatureLayer && SelectedEncoding != null)
            {
                ShapeFileFeatureLayer shapeFileFeatureLayer = (ShapeFileFeatureLayer)targetFeatureLayer;
                if (shapeFileFeatureLayer.Encoding != SelectedEncoding)
                {
                    var oldEncoding = shapeFileFeatureLayer.Encoding;
                    shapeFileFeatureLayer.Encoding = SelectedEncoding;
                    lock (shapeFileFeatureLayer)
                    {
                        lock (shapeFileFeatureLayer)
                        {
                            if (shapeFileFeatureLayer.IsOpen) shapeFileFeatureLayer.Close();
                            shapeFileFeatureLayer.Open();
                            shapeFileFeatureLayer.FeatureSource.RefreshColumns();
                        }
                        var textStyles = shapeFileFeatureLayer.ZoomLevelSet.CustomZoomLevels
                        .SelectMany(level => level.CustomStyles)
                        .OfType<TextStyle>().Distinct();

                        foreach (var textStyle in textStyles)
                        {
                            textStyle.TextColumnName = SelectedEncoding.GetString(oldEncoding.GetBytes(textStyle.TextColumnName));
                        }
                    }

                    SaveEncodingToXML(shapeFileFeatureLayer.ShapePathFilename, SelectedEncoding.CodePage);
                    if (GisEditor.ActiveMap != null)
                    {
                        var layerOverlays = from overlay in GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>()
                                            where overlay.Layers.Contains(targetFeatureLayer)
                                            select overlay;

                        foreach (var layerOverlay in layerOverlays)
                        {
                            layerOverlay.Invalidate();
                        }
                    }

                    IsEncodingPending = false;
                }
            }
        }

        private static void SaveEncodingToXML(string shpPath, int codePage)
        {
            var shpPlugin = GisEditor.LayerManager.GetPlugins().OfType<ShapeFileFeatureLayerPlugin>().FirstOrDefault();
            shpPlugin.Encodings[shpPath] = codePage.ToString();
        }
    }
}