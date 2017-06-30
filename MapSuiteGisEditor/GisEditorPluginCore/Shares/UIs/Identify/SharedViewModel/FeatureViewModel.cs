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
using System.Data;
using System.Globalization;
using System.Linq;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class FeatureViewModel : ViewModelBase
    {
        private bool isSelected;
        private string header;
        private string featureId;
        private string wkt;
        private FeatureLayer ownerFeatureLayer;
        private DataTable table;
        private Feature feature;

        public FeatureViewModel(Feature newFeature, FeatureLayer ownerFeatureLayer)
        {
            header = String.Format(CultureInfo.InvariantCulture, "{0}", newFeature.Id);
            //.Length > 5 ? feature.Feature.Id.Substring(0, 5) + "..." : feature.Feature.Id);

            string featureIdColumn = LayerPluginHelper.GetFeatureIdColumn(ownerFeatureLayer);
            if (newFeature.ColumnValues.ContainsKey(featureIdColumn))
            {
                header = String.Format(CultureInfo.InvariantCulture, "{0}", newFeature.ColumnValues[featureIdColumn]);
            }
            //if (newFeature.LinkColumnValues.ContainsKey(featureIdColumn))
            //{
            //    Collection<LinkColumnValue> linkColumnValues = newFeature.LinkColumnValues[featureIdColumn];
            //    header = String.Format(CultureInfo.InvariantCulture, "{0}", string.Join("|||", linkColumnValues.Select(lcv => lcv.Value.ToString())));
            //}

            if (string.IsNullOrEmpty(header) || string.IsNullOrWhiteSpace(header))
            {
                header = "[NONE]";
            }

            featureId = newFeature.Id;
            wkt = newFeature.GetWellKnownText();
            this.ownerFeatureLayer = ownerFeatureLayer;
            feature = newFeature.CloneDeep(ReturningColumnsType.AllColumns);

            table = new DataTable();
            table.Columns.Add("Column", typeof(string));
            table.Columns.Add("Value", typeof(string));
            table.Columns.Add("RealValue", typeof(string));
            Collection<FeatureSourceColumn> featureColumns = new Collection<FeatureSourceColumn>();
            FeatureLayer columnSourceLayer = newFeature.Tag as FeatureLayer;
            if (columnSourceLayer == null)
            {
                columnSourceLayer = ownerFeatureLayer;
            }

            columnSourceLayer.SafeProcess(() =>
            {
                var tempFeatureColumns = columnSourceLayer.QueryTools.GetColumns();
                foreach (var item in tempFeatureColumns)
                {
                    featureColumns.Add(item);
                }
            });

            if (CalculatedDbfColumn.CalculatedColumns.ContainsKey(columnSourceLayer.FeatureSource.Id))
            {
                foreach (var item in CalculatedDbfColumn.CalculatedColumns[columnSourceLayer.FeatureSource.Id])
                {
                    featureColumns.Add(item);
                }
            }

            foreach (var column in featureColumns)
            {
                if (!string.IsNullOrEmpty(column.ColumnName) && newFeature.ColumnValues.ContainsKey(column.ColumnName))
                {
                    string realValue = newFeature.ColumnValues[column.ColumnName];
                    string value = realValue;
                    object valueResult = null;
                    if (column.TypeName.Equals("Double", StringComparison.InvariantCultureIgnoreCase)
                        || column.TypeName.Equals("Float", StringComparison.InvariantCultureIgnoreCase))
                    {
                        double doubleValue;
                        if (double.TryParse(value, out doubleValue))
                        {
                            value = doubleValue.ToString(CultureInfo.InvariantCulture);
                            DbfColumn dbfColumn = column as DbfColumn;
                            if (dbfColumn != null)
                            {
                                value = doubleValue.ToString("N" + dbfColumn.DecimalLength);
                            }
                        }
                    }
                    else if (column.TypeName.Equals("Integer", StringComparison.InvariantCultureIgnoreCase)
                        || column.TypeName.Equals("Numeric", StringComparison.InvariantCultureIgnoreCase))
                    {
                        int intValue;
                        value = int.TryParse(value, out intValue) ? intValue.ToString(CultureInfo.InvariantCulture) : string.Empty;
                    }
                    else
                    {
                        valueResult = value;
                        //valueResult = new Uri(value, UriKind.RelativeOrAbsolute);
                    }

                    if (valueResult == null) valueResult = value;
                    //if (valueResult == null) valueResult = new Uri(value, UriKind.RelativeOrAbsolute);

                    string alias = ownerFeatureLayer.FeatureSource.GetColumnAlias(column.ColumnName);
                    table.Rows.Add(alias, valueResult, realValue);
                }
            }

            //foreach (KeyValuePair<string, Collection<LinkColumnValue>> item in newFeature.LinkColumnValues.Where(i => !newFeature.ColumnValues.ContainsKey(i.Key)))
            //{
            //    string alias = ownerFeatureLayer.FeatureSource.GetColumnAlias(item.Key);
            //    Collection<LinkColumnValue> linkColumnValues = item.Value;
            //    if (linkColumnValues != null)
            //    {
            //        IEnumerable<string> filteredColumnValues = linkColumnValues
            //            .Select(v =>
            //            {
            //                if (v.Value != null)
            //                {
            //                    if (v.Value is DateTime)
            //                    {
            //                        DateTime dateTime = (DateTime)v.Value;
            //                        if (dateTime != DateTime.MinValue)
            //                        {
            //                            return dateTime.ToString("MM/dd/yyyy");
            //                        }
            //                        return string.Empty;
            //                    }
            //                    return v.Value.ToString();
            //                }
            //                return null;
            //            })
            //            .Where(v => v != null);

            //        string value = string.Join(Environment.NewLine, filteredColumnValues);
            //        string realValue = string.Join(Environment.NewLine, linkColumnValues.Where(c => c != null).Where(v => !string.IsNullOrEmpty(v.ToString())).Select(l => l.Value));
            //        if (Uri.IsWellFormedUriString(value, UriKind.RelativeOrAbsolute))
            //        {
            //            table.Rows.Add(alias, new Uri(value, UriKind.RelativeOrAbsolute), realValue);
            //        }
            //        else
            //        {
            //            table.Rows.Add(alias, value, realValue);
            //        }
            //    }
            //    else
            //    {
            //        table.Rows.Add(alias, new Uri("", UriKind.RelativeOrAbsolute), null);
            //    }
            //}
        }

        public Feature Feature
        {
            get { return feature; }
        }

        public string LayerName
        {
            get { return OwnerFeatureLayer.Name; }
        }

        public Type Type
        {
            get { return GetType(); }
        }

        public string Header
        {
            get { return header; }
        }

        public string FeatureId
        {
            get { return featureId; }
        }

        public DataView DefaultView
        {
            get { return table.DefaultView; }
        }

        public FeatureLayer OwnerFeatureLayer
        {
            get { return ownerFeatureLayer; }
        }

        public string WKT
        {
            get { return wkt; }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                RaisePropertyChanged(() => IsSelected);
            }
        }
    }
}