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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Kent.Boogaart.KBCsv;
using ThinkGeo.MapSuite.GeocodeServerSdk;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class GeocoderWizardSharedObject : ViewModelBase
    {
        private string inputFilePath;
        private GeocoderType geocoderMatchType;
        private string outputFilePath;
        private bool isAddToDataRepository;
        private DataTable resultDataTable;
        private DataTable finalResultDataTable;
        private DataTable previewDataTable;
        private KeyValuePair<string, string> selectedDelimiter;
        private bool isCustomerDelimiterEnable;
        private string delimiter;
        private ObservableCollection<MappedColumn> mappedColumns;
        private ObservableCollection<string> inputFileColumns;
        private ObservableCollection<string> geocoderColumns;
        private ObservableCollection<string> retainedColumns;
        private bool isColumnNamesInFirstRow;
        private double currentValue;
        private double maxValue;
        private DataTable errorTable;
        private Visibility progressBarVisibility;
        private Visibility errorTableVisibility;
        [NonSerialized]
        private RelayCommand browserCommand;
        [NonSerialized]
        private RelayCommand columnInFirstRowCommand;

        public GeocoderWizardSharedObject()
        {
            progressBarVisibility = Visibility.Visible;
            errorTableVisibility = Visibility.Collapsed;

            inputFileColumns = new ObservableCollection<string>();
            geocoderColumns = new ObservableCollection<string>();
            retainedColumns = new ObservableCollection<string>();

            GeocoderMatchType = GeocoderType.Default;
            IsColumnNamesInFirstRow = false;
            DelimiterDictionary delimiterDictionary = new DelimiterDictionary();
            Dictionary<string, string>.Enumerator enu = delimiterDictionary.GetEnumerator();
            if (enu.MoveNext())
            {
                SelectedDelimiter = enu.Current;
            }
        }

        public string InputFilePath
        {
            get { return inputFilePath; }
            set
            {
                inputFilePath = value;
                RaisePropertyChanged(()=>InputFilePath);
            }
        }

        public GeocoderType GeocoderMatchType
        {
            get { return geocoderMatchType; }
            set
            {
                geocoderMatchType = value;
                RaisePropertyChanged(()=>GeocoderMatchType);
                MappedColumns = new ObservableCollection<MappedColumn>();
                switch (value)
                {
                    case GeocoderType.AddressAndCityAndState:
                        MappedColumns.Add(new MappedColumn { ColumnKey = "Address" });
                        MappedColumns.Add(new MappedColumn { ColumnKey = "City" });
                        MappedColumns.Add(new MappedColumn { ColumnKey = "State" });
                        break;
                    case GeocoderType.AddressAndZip:
                        MappedColumns.Add(new MappedColumn { ColumnKey = "Address" });
                        MappedColumns.Add(new MappedColumn { ColumnKey = "Zip" });
                        break;
                    case GeocoderType.CensusBlock:
                        MappedColumns.Add(new MappedColumn { ColumnKey = "Censusblockid" });
                        break;
                    case GeocoderType.CensusBlockGroup:
                        MappedColumns.Add(new MappedColumn { ColumnKey = "Censusblockgroupid" });
                        break;
                    case GeocoderType.CensusTrack:
                        MappedColumns.Add(new MappedColumn { ColumnKey = "Censustrackid" });
                        break;
                    case GeocoderType.CityAndState:
                        MappedColumns.Add(new MappedColumn { ColumnKey = "City" });
                        MappedColumns.Add(new MappedColumn { ColumnKey = "State" });
                        break;
                    case GeocoderType.County:
                        MappedColumns.Add(new MappedColumn { ColumnKey = "County" });
                        break;
                    case GeocoderType.LongitudeAndLatitude:
                        MappedColumns.Add(new MappedColumn { ColumnKey = "Longitude" });
                        MappedColumns.Add(new MappedColumn { ColumnKey = "Latitude" });
                        break;
                    case GeocoderType.State:
                        MappedColumns.Add(new MappedColumn { ColumnKey = "State" });
                        break;
                    case GeocoderType.Zip:
                        MappedColumns.Add(new MappedColumn { ColumnKey = "Zip" });
                        break;
                    default:
                        MappedColumns.Add(new MappedColumn { ColumnKey = "Default" });
                        break;
                }

                try
                {
                    ReadDataToDataGrid();
                }
                catch (FormatException formatException)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, formatException.Message, new ExceptionInfo(formatException));
                    PreviewDataTable = GetDataTableForException(formatException.Message);
                }
                catch (Exception exception)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, exception.Message, new ExceptionInfo(exception));
                    PreviewDataTable = GetDataTableForException(exception.Message);
                }
            }
        }

        public string OutputFilePath
        {
            get { return outputFilePath; }
            set
            {
                outputFilePath = value;
                RaisePropertyChanged(()=>OutputFilePath);
            }
        }

        public bool IsAddToDataRepository
        {
            get { return isAddToDataRepository; }
            set
            {
                isAddToDataRepository = value;
                RaisePropertyChanged(()=>IsAddToDataRepository);
            }
        }

        public DataTable ResultDataTable
        {
            get { return resultDataTable; }
            set
            {
                resultDataTable = value;
                RaisePropertyChanged(()=>ResultDataTable);
            }
        }

        public DataTable FinalResultDataTable
        {
            get { return finalResultDataTable; }
            set
            {
                finalResultDataTable = value;
                RaisePropertyChanged(()=>FinalResultDataTable);
            }
        }

        public DataTable PreviewDataTable
        {
            get { return previewDataTable; }
            set
            {
                previewDataTable = value;
                RaisePropertyChanged(()=>PreviewDataTable);
            }
        }

        public KeyValuePair<string, string> SelectedDelimiter
        {
            get { return selectedDelimiter; }
            set
            {
                selectedDelimiter = value;
                RaisePropertyChanged(()=>SelectedDelimiter);
                Delimiter = value.Value;
                IsCustomerDelimiterEnable = (value.Key == "Custom");
            }
        }

        public bool IsCustomerDelimiterEnable
        {
            get { return isCustomerDelimiterEnable; }
            set
            {
                isCustomerDelimiterEnable = value;
                RaisePropertyChanged(()=>IsCustomerDelimiterEnable);
            }
        }

        public string Delimiter
        {
            get { return delimiter; }
            set
            {
                delimiter = value;
                RaisePropertyChanged(()=>Delimiter);
            }
        }

        public ObservableCollection<MappedColumn> MappedColumns
        {
            get { return mappedColumns; }
            set
            {
                mappedColumns = value;
                RaisePropertyChanged(()=>MappedColumns);
            }
        }

        public ObservableCollection<string> InputFileColumns
        {
            get { return inputFileColumns; }
            set
            {
                inputFileColumns = value;
                RaisePropertyChanged(()=>InputFileColumns);
            }
        }

        public ObservableCollection<string> GeocoderColumns
        {
            get { return geocoderColumns; }
            set
            {
                geocoderColumns = value;
                RaisePropertyChanged(()=>GeocoderColumns);
            }
        }

        public ObservableCollection<string> RetainedColumns
        {
            get { return retainedColumns; }
            set
            {
                retainedColumns = value;
                RaisePropertyChanged(()=>RetainedColumns);
            }
        }

        public bool IsColumnNamesInFirstRow
        {
            get { return isColumnNamesInFirstRow; }
            set
            {
                isColumnNamesInFirstRow = value;
                RaisePropertyChanged(()=>IsColumnNamesInFirstRow);
                if (!isColumnNamesInFirstRow)
                {
                    GeocoderMatchType = GeocoderType.Default;
                }
            }
        }

        public double CurrentValue
        {
            get { return currentValue; }
            set
            {
                currentValue = value;
                RaisePropertyChanged(()=>CurrentValue);
            }
        }

        public double MaxValue
        {
            get { return maxValue; }
            set
            {
                maxValue = value;
                RaisePropertyChanged(()=>MaxValue);
            }
        }

        public DataTable ErrorTable
        {
            get { return errorTable; }
            set
            {
                errorTable = value;
                RaisePropertyChanged(()=>ErrorTable);
            }
        }

        public Visibility ProgressBarVisibility
        {
            get { return progressBarVisibility; }
            set
            {
                progressBarVisibility = value;
                RaisePropertyChanged(()=>ProgressBarVisibility);
            }
        }

        public Visibility ErrorTableVisibility
        {
            get { return errorTableVisibility; }
            set
            {
                errorTableVisibility = value;
                RaisePropertyChanged(()=>ErrorTableVisibility);
            }
        }

        public RelayCommand BrowserCommand
        {
            get
            {
                if (browserCommand == null)
                {
                    browserCommand = new RelayCommand(() =>
                    {
                        NotificationMessageAction<string> message = new NotificationMessageAction<string>("CSV Files(*.CSV)|*.CSV", (fileName) =>
                        {
                            try
                            {
                                InputFilePath = fileName;
                                ReadDataToDataGrid();
                            }
                            catch (IOException ioException)
                            {
                                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ioException.Message, new ExceptionInfo(ioException));
                                InputFilePath = String.Empty;
                                PreviewDataTable = GetDataTableForException(ioException.Message);
                                Messenger.Default.Send(new DialogMessage(ioException.Message, null) { Caption = "Error" });
                            }
                            catch (FormatException formatException)
                            {
                                GisEditor.LoggerManager.Log(LoggerLevel.Debug, formatException.Message, new ExceptionInfo(formatException));
                                PreviewDataTable = GetDataTableForException(formatException.Message);
                            }
                            catch (Exception exception)
                            {
                                GisEditor.LoggerManager.Log(LoggerLevel.Debug, exception.Message, new ExceptionInfo(exception));
                                PreviewDataTable = GetDataTableForException(exception.Message);
                            }
                        });
                        Messenger.Default.Send(message, this);
                    });
                }
                return browserCommand;
            }
        }

        public RelayCommand ColumnInFirstRowCommand
        {
            get
            {
                if (columnInFirstRowCommand == null)
                {
                    columnInFirstRowCommand = new RelayCommand(ReadDataToDataGrid);
                }
                return columnInFirstRowCommand;
            }
        }

        public void ReadDataToDataGrid()
        {
            if (File.Exists(InputFilePath))
            {
                if (Delimiter.Length == 1)
                {
                    DataTable dataTable = new DataTable();
                    CsvReader csvReader = new CsvReader(InputFilePath);
                    csvReader.ValueSeparator = Delimiter[0];
                    if (IsColumnNamesInFirstRow)
                    {
                        foreach (string column in csvReader.ReadHeaderRecord().Values)
                        {
                            dataTable.Columns.Add(column);
                        }
                        Collection<int> rowsNumber = new Collection<int>();
                        int i = 1;
                        foreach (DataRecord dt in csvReader.ReadDataRecords())
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
                    }
                    else
                    {
                        dataTable.Columns.Add("Default");
                        foreach (string[] strArray in csvReader.ReadDataRecordsAsStrings())
                        {
                            DataRow dr = dataTable.NewRow();
                            StringBuilder sb = new StringBuilder();
                            foreach (string str in strArray)
                            {
                                sb.Append(str + Delimiter);
                            }
                            if (sb.Length > 0)
                            {
                                dr["Default"] = sb.ToString().TrimEnd(Delimiter.ToCharArray());
                                dataTable.Rows.Add(dr);
                            }
                        }
                    }
                    PreviewDataTable = dataTable;
                    csvReader.Close();
                }
            }
        }

        public void MapColumnsAutomatically()
        {
            foreach (MappedColumn mappedColumn in MappedColumns)
            {
                foreach (DataColumn dataColumn in PreviewDataTable.Columns)
                {
                    var columnName = dataColumn.ColumnName.ToUpperInvariant();
                    if (columnName.Contains(mappedColumn.ColumnKey.ToUpperInvariant()))
                    {
                        mappedColumn.SelectedColumn = dataColumn;
                    }
                }
            }
        }

        public void GenerateColumns()
        {
            InputFileColumns.Clear();
            foreach (DataColumn dataColumn in PreviewDataTable.Columns)
            {
                InputFileColumns.Add(dataColumn.ColumnName);
            }
            GeocoderColumns.Clear();
            foreach (DataColumn dataColumn in ResultDataTable.Columns)
            {
                if (dataColumn.ColumnName.Contains(" (O)"))
                {
                    GeocoderColumns.Add(dataColumn.ColumnName.Replace(" (O)", ""));
                }
            }
            InputFileColumns.Sort();
            GeocoderColumns.Sort();

            FinalResultDataTable = new DataTable();
            if (ResultDataTable.Columns.Contains("CentroidPoint (O)"))
            {
                FinalResultDataTable.Columns.Add("X");
                FinalResultDataTable.Columns.Add("Y");
                foreach (DataRow dataRow in ResultDataTable.Rows)
                {
                    DataRow newDataRow = FinalResultDataTable.NewRow();
                    if (String.IsNullOrEmpty(dataRow["CentroidPoint (O)"].ToString()))
                    {
                        newDataRow["X"] = "";
                        newDataRow["Y"] = "";
                    }
                    else
                    {
                        string[] xys = dataRow["CentroidPoint (O)"].ToString().Split(' ');
                        if (xys.Length == 2)
                        {
                            newDataRow["X"] = xys[0].Replace("POINT(", "");
                            newDataRow["Y"] = xys[1].Replace(")", "");
                        }
                        else
                        {
                            newDataRow["X"] = "";
                            newDataRow["Y"] = "";
                        }
                    }

                    FinalResultDataTable.Rows.Add(newDataRow);
                }
            }
        }

        public void CreateResultsPreview()
        {
            DataTable tmpDataTable = new DataTable();
            foreach (string item in RetainedColumns)
            {
                tmpDataTable.Columns.Add(item);
            }
            tmpDataTable.Columns.Add("X");
            tmpDataTable.Columns.Add("Y");
            foreach (DataRow dataRow in ResultDataTable.Rows)
            {
                DataRow newDataRow = tmpDataTable.NewRow();
                foreach (string item in RetainedColumns)
                {
                    newDataRow[item] = dataRow[item];
                }

                if (ResultDataTable.Columns.Contains("CentroidPoint (O)"))
                {
                    if (String.IsNullOrEmpty(dataRow["CentroidPoint (O)"].ToString()))
                    {
                        newDataRow["X"] = "";
                        newDataRow["Y"] = "";
                    }
                    else
                    {
                        string[] xys = dataRow["CentroidPoint (O)"].ToString().Split(' ');
                        if (xys.Length == 2)
                        {
                            newDataRow["X"] = xys[0].Replace("POINT(", "");
                            newDataRow["Y"] = xys[1].Replace(")", "");
                        }
                        else
                        {
                            newDataRow["X"] = "";
                            newDataRow["Y"] = "";
                        }
                    }
                }

                tmpDataTable.Rows.Add(newDataRow);
            }
            foreach (DataColumn dataColumn in tmpDataTable.Columns)
            {
                string columnName = dataColumn.ColumnName;
                if (columnName != "X" && columnName != "Y")
                {
                    dataColumn.ColumnName = columnName.Remove(columnName.Length - 4, 4);
                }
            }
            FinalResultDataTable = tmpDataTable;
        }

        public void CreateResultFile()
        {
            using (StreamWriter streamWriter = new StreamWriter(OutputFilePath))
            {
                StringBuilder columnString = new StringBuilder();
                foreach (DataColumn retainedColumn in FinalResultDataTable.Columns)
                {
                    columnString.Append(retainedColumn.ColumnName + Delimiter);
                }
                streamWriter.WriteLine(columnString.ToString().TrimEnd(Delimiter.ToCharArray()));

                foreach (DataRow dataRow in FinalResultDataTable.Rows)
                {
                    StringBuilder rowString = new StringBuilder();
                    foreach (object obj in dataRow.ItemArray)
                    {
                        string field = obj.ToString();
                        if (field.IndexOf(Delimiter) >= 0)
                        {
                            field = "\"" + field.Replace("\"", "\"\"") + "\"";
                        }
                        rowString.Append(field + Delimiter);
                    }
                    if (rowString.Length > 0)
                    {
                        streamWriter.WriteLine(rowString.ToString().TrimEnd(Delimiter.ToCharArray()));
                    }
                }
            }

            if (IsAddToDataRepository)
            {
                //var folderDataPlugin = GisEditor.DataRepositoryManager.GetPlugins().OfType<FolderDataPlugin>().FirstOrDefault();
                //if (folderDataPlugin != null)
                //{
                //    folderDataPlugin.Children.Add(new FolderDataItem(Path.GetDirectoryName(OutputFilePath), true));
                //}
            }
        }

        public void WriteErrorMessage(string fileName)
        {
            using (StreamWriter streamWriter = new StreamWriter(fileName))
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (DataColumn dataColumn in ErrorTable.Columns)
                {
                    stringBuilder.Append(dataColumn.ColumnName + Delimiter);
                }
                if (stringBuilder.Length > 0)
                {
                    streamWriter.WriteLine(stringBuilder.ToString().TrimEnd(Delimiter.ToCharArray()));
                }
                foreach (DataRow dataRow in ErrorTable.Rows)
                {
                    stringBuilder.Clear();
                    foreach (object obj in dataRow.ItemArray)
                    {
                        string field = obj.ToString();
                        if (field.IndexOf(Delimiter) >= 0)
                        {
                            field = "\"" + field.Replace("\"", "\"\"") + "\"";
                        }
                        stringBuilder.Append(field + Delimiter);
                    }
                    if (stringBuilder.Length > 0)
                    {
                        streamWriter.WriteLine(stringBuilder.ToString().TrimEnd(Delimiter.ToCharArray()));
                    }
                }
            }
        }

        private DataTable GetDataTableForException(string message)
        {
            DataTable dataTable = new DataTable("ErrorTable");
            dataTable.Columns.Add("Error");
            DataRow dataRow = dataTable.NewRow();
            dataRow["Error"] = message;
            dataTable.Rows.Add(dataRow);

            return dataTable;
        }

        private string GetValue(string key, DataRow dataRow)
        {
            var addressResult = from mapColumn in MappedColumns where mapColumn.ColumnKey == key select mapColumn.SelectedColumn.ColumnName;
            return dataRow[addressResult.FirstOrDefault()].ToString();
        }
    }
}
