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
using System.Data;
using System.IO;
using System.Collections.ObjectModel;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DbfDataJoinAdapter : DataJoinAdapter
    {
        public override DataTable ReadDataToDataGrid(string filePath, string customParameter)
        {
            DataTable PreviewDataTable = new DataTable();
            if (File.Exists(filePath))
            {
                DataTable dataTable = new DataTable();

                using (GeoDbf geoDbf = new GeoDbf(filePath, GeoFileReadWriteMode.Read))
                {
                    geoDbf.Open();

                    dataTable = new DataTable();

                    var columns = geoDbf.GetAllColumns();
                    foreach (var item in columns)
                    {
                        dataTable.Columns.Add(item.ColumnName);
                    }

                    for (int i = 1; i <= geoDbf.RecordCount; i++)
                    {
                        DataRow dr = dataTable.NewRow();
                        var dictionary = geoDbf.ReadRecordAsString(i);
                        for (int j = 1; j <= geoDbf.ColumnCount; j++)
                        {
                            string value;
                            dictionary.TryGetValue(geoDbf.GetColumnName(j), out value);
                            dr[j - 1] = value;
                        }
                        dataTable.Rows.Add(dr);
                    }

                    geoDbf.Close();
                }

                PreviewDataTable = dataTable;
            }

            return PreviewDataTable;
        }

        public override Collection<FeatureSourceColumn> GetColumnToAdd(string filePath, string customParameter)
        {
            Collection<FeatureSourceColumn> result = new Collection<FeatureSourceColumn>();

            using (GeoDbf geoDbf = new GeoDbf(filePath, GeoFileReadWriteMode.Read))
            {
                geoDbf.Open();
                var columns = geoDbf.GetAllColumns();
                foreach (var item in columns)
                {
                    DataJoinFeatureSourceColumn csvColumn = new DataJoinFeatureSourceColumn(item.ColumnName, item.TypeName, item.MaxLength);
                    result.Add(csvColumn);
                }

                geoDbf.Close();
            }

            return result;
        }
    }
}
