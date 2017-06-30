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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    internal class FeatureLayerAdapter
    {
        public static readonly string FeatureIdColumnName = "TGFeatureID";
        public static readonly string IsSelectedColumnName = "TGFeatureIsSelected";
        public static readonly string WkbColumnName = "TGWKBBase64String";

        public event EventHandler<ProgressChangedEventArgs> LoadingData;

        private bool isLinkDataSourceEnabled;
        private DataTable dataTable;
        private FeatureLayer featureLayer;
        private Dictionary<string, Feature> selectedFeatures;
        private bool enableColumnVirtualization;
        private Collection<string> linkColumnNames;

        public FeatureLayerAdapter(FeatureLayer featureLayer, Collection<string> linkColumnNames)
        {
            this.enableColumnVirtualization = true;
            this.featureLayer = featureLayer;
            this.selectedFeatures = new Dictionary<string, Feature>();
            this.InitializeSelectedFeatures();
            this.linkColumnNames = linkColumnNames;
        }

        public bool EnableColumnVirtualization
        {
            get { return enableColumnVirtualization; }
            set { enableColumnVirtualization = value; }
        }

        public bool IsLinkDataSourceEnabled
        {
            get { return isLinkDataSourceEnabled; }
            set { isLinkDataSourceEnabled = value; }
        }

        public string Name
        {
            get { return FeatureLayer.Name; }
        }

        public FeatureLayer FeatureLayer
        {
            get { return featureLayer; }
        }

        public Dictionary<string, Feature> SelectedFeatures
        {
            get { return selectedFeatures; }
        }

        protected virtual void OnLoadingData(int index)
        {
            EventHandler<ProgressChangedEventArgs> handler = LoadingData;
            if (handler != null)
            {
                handler(this, new ProgressChangedEventArgs(index, null));
            }
        }

        public void GetCountAsync(Action<int> action)
        {
            Task.Factory.StartNew(() =>
            {
                int count = GetCountCore();
                if (action != null)
                {
                    action(count);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public int GetCount()
        {
            int rowCount = int.MaxValue;
            if (featureLayer.FeatureSource.CanGetCountQuickly())
            {
                rowCount = GetCountCore();
            }
            return rowCount;
        }

        protected virtual int GetCountCore()
        {
            int count = 0;
            if (dataTable != null)
            {
                count = dataTable.Rows.Count;
            }
            if (count == 0 && featureLayer != null)
            {
                featureLayer.SafeProcess(() =>
                {
                    count = featureLayer.QueryTools.GetCount();
                });
            }
            return count;
        }

        public DataTable GetDataTable(DataTable currentDataTable, int length)
        {
            try
            {
                InitializeSelectedFeatures();
                return GetDataTableCore(currentDataTable, length);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "View Data");
                return new DataTable();
            }
        }

        protected virtual DataTable GetDataTableCore(DataTable currentDataTable, int length)
        {
            return GetDataTableCore();
        }

        public DataTable GetDataTable(IEnumerable<string> featureIds)
        {
            try
            {
                InitializeSelectedFeatures();
                return GetDataTableCore(featureIds);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return new DataTable();
            }
        }

        protected virtual DataTable GetDataTableCore(IEnumerable<string> featureIds)
        {
            DataTable newDataTable = new DataTable();
            Collection<Feature> features = new Collection<Feature>();
            FeatureLayer.SafeProcess(() =>
            {
                features = featureLayer.FeatureSource.GetFeaturesByIds(featureIds, featureLayer.GetDistinctColumnNames());
            });
            ReadFeatureData(features, newDataTable);
            return newDataTable;
        }

        public DataTable GetDataTable()
        {
            try
            {
                InitializeSelectedFeatures();
                return GetDataTableCore();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "View Data");
                return new DataTable();
            }
        }

        protected virtual DataTable GetDataTableCore()
        {
            dataTable = new DataTable();
            Collection<Feature> features = new Collection<Feature>();
            FeatureLayer.SafeProcess(() =>
            {
                features = FeatureLayer.QueryTools.GetAllFeatures(FeatureLayer.GetDistinctColumnNames());
            });

            ReadFeatureData(features, dataTable);

            return dataTable;
        }

        public DataTable GetDataTableInCurrentExtent(RectangleShape currentExtent)
        {
            InitializeSelectedFeatures();
            return GetDataTableInCurrentExtentCore(currentExtent);
        }

        protected virtual DataTable GetDataTableInCurrentExtentCore(RectangleShape currentExtent)
        {
            DataTable dataTable = new DataTable();
            Collection<Feature> features = new Collection<Feature>();
            FeatureLayer.SafeProcess(() =>
            {
                features = FeatureLayer.QueryTools.GetFeaturesIntersecting(currentExtent, FeatureLayer.GetDistinctColumnNames());
            });
            ReadFeatureData(features, dataTable, true);
            return dataTable;
        }

        private void InitializeSelectedFeatures()
        {
            SelectedFeatures.Clear();
            SelectionTrackInteractiveOverlay selectionOverlay = GisEditor.SelectionManager.GetSelectionOverlay();
            if (selectionOverlay != null)
            {
                var results = selectionOverlay.GetSelectedFeaturesGroup(featureLayer);
                if (results.Count > 0)
                {
                    foreach (var feature in results[featureLayer])
                    {
                        SelectedFeatures.Add(feature.Id, feature);
                    }
                }
            }
        }

        protected static void FillUriColumnNames(Collection<string> uriColumnNames, DataTable dataTable)
        {
            if (uriColumnNames.Count > 0)
            {
                foreach (DataColumn column in dataTable.Columns)
                {
                    if (uriColumnNames.Contains(column.ColumnName))
                    {
                        column.DataType = typeof(Uri);
                    }
                }
            }
        }

        public void ReadFeatureData(IEnumerable<Feature> features, DataTable dataTable, bool showDataInCurrentExtent = false)
        {
            if (features.Count() > 0)
            {
                List<DataColumn> columns = null; // features[0].ColumnValues.Select(v => new DataColumn(v.Key)).ToArray();

                FeatureLayer.SafeProcess(() =>
                {
                    columns = FeatureLayer.GetDistinctColumnNames().Select(tmpColumnName => new DataColumn(tmpColumnName)).ToList();
                });

                columns.Insert(0, new DataColumn(FeatureIdColumnName, typeof(string)));
                columns.Insert(1, new DataColumn(IsSelectedColumnName, typeof(string)));

                if (showDataInCurrentExtent) columns.Add(new DataColumn(WkbColumnName, typeof(string)));
                dataTable.Columns.AddRange(columns.ToArray());

                FillUriColumnNames(linkColumnNames, dataTable);

                foreach (var feature in features)
                {
                    var row = dataTable.NewRow();
                    row[FeatureIdColumnName] = feature.Id;
                    row[IsSelectedColumnName] = SelectedFeatures.ContainsKey(feature.Id);
                    if (showDataInCurrentExtent)
                    {
                        byte[] wkb = feature.GetWellKnownBinary();
                        if (wkb != null) row[WkbColumnName] = Convert.ToBase64String(wkb);
                    }
                    string tempPreviousValue = string.Empty;
                    foreach (var columnValue in feature.ColumnValues)
                    {
                        if (columns.Any(c => c.ColumnName.Equals(columnValue.Key)))
                        {
                            string value = columnValue.Value;
                            if (linkColumnNames.Contains(columnValue.Key))
                            {
                                row[columnValue.Key] = new Uri(columnValue.Value, UriKind.RelativeOrAbsolute);
                            }
                            else
                            {
                                row[columnValue.Key] = columnValue.Value;
                            }

                            int previousRowCount = tempPreviousValue.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Count();
                            int currentRowCount = value.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Count();
                            if (previousRowCount != currentRowCount)
                            {
                                EnableColumnVirtualization = false;
                            }
                            tempPreviousValue = value;
                        }
                    }
                    //foreach (var columnValue in feature.LinkColumnValues)
                    //{
                    //    if (columns.Any(c => c.ColumnName.Equals(columnValue.Key)))
                    //    {
                    //        if (columnValue.Value != null)
                    //        {
                    //            row[columnValue.Key] = string.Join(",", columnValue.Value.Select(v => v.Value));
                    //        }
                    //    }
                    //}
                    dataTable.Rows.Add(row);
                    OnLoadingData(dataTable.Rows.Count);
                }
            }
        }
    }
}