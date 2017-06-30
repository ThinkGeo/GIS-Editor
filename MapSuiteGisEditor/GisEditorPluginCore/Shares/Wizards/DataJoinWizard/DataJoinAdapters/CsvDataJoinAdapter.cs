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
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class CsvDataJoinAdapter : DataJoinAdapter
    {
        public override DataTable ReadDataToDataGrid(string filePath, string customParameter)
        {
            DataTable PreviewDataTable = new DataTable();
            if (File.Exists(filePath))
            {
                DataTable dataTable = new DataTable();
                Kent.Boogaart.KBCsv.CsvReader csvReader = new Kent.Boogaart.KBCsv.CsvReader(filePath);
                csvReader.ValueSeparator = customParameter[0];
                foreach (string column in csvReader.ReadHeaderRecord().Values)
                {
                    dataTable.Columns.Add(column);
                }
                Collection<int> rowsNumber = new Collection<int>();
                int i = 1;
                foreach (Kent.Boogaart.KBCsv.DataRecord dt in csvReader.ReadDataRecords())
                {
                    DataRow dr = dataTable.NewRow();
                    if (dt.Values.Count == dt.HeaderRecord.Values.Count)
                    {
                        foreach (string column in dt.HeaderRecord.Values)
                        {
                            dr[column] = dt[column];
                        }
                        dataTable.Rows.Add(dr);
                    }
                    else
                    {
                        rowsNumber.Add(i);
                        continue;
                    }
                    i++;
                }
                if (rowsNumber.Count > 0)
                {
                    StringBuilder message = new StringBuilder("The following line(s) are invalid and has been ignored. " + Environment.NewLine + "Line Number: ");
                    foreach (var item in rowsNumber)
                    {
                        message.Append(item.ToString() + ", ");
                    }
                    Messenger.Default.Send(new DialogMessage(message.Remove(message.Length - 2, 2).ToString(), null) { Caption = "Warning" });
                }
                PreviewDataTable = dataTable;
            }
            return PreviewDataTable;
        }

        public override UserControl GetConfigurationContent()
        {
            return new CsvDataJoinDelimitedFileUserControl();
        }

        public override Collection<FeatureSourceColumn> GetColumnToAdd(string filePath, string customParameter)
        {
            Collection<FeatureSourceColumn> result = new Collection<FeatureSourceColumn>();
            CsvFeatureSource csvFeatureSource = new CsvFeatureSource();
            csvFeatureSource.DelimitedPathFilename = filePath;
            csvFeatureSource.Delimiter = customParameter;
            csvFeatureSource.RequireIndex = false;
            csvFeatureSource.Open();
            foreach (var column in csvFeatureSource.GetColumns())
            {
                DataJoinFeatureSourceColumn csvColumn = new DataJoinFeatureSourceColumn(column.ColumnName, column.TypeName, column.MaxLength);
                result.Add(csvColumn);
            }
            csvFeatureSource.Close();

            return result;
        }
    }
}
