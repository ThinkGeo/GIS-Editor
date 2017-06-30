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
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class CSVViewModel : ViewModelBase
    {
        private CSVModel selectedCSVModel;
        private List<CSVModel> csvModelList;
        private KeyValuePair<string, string> selectedDelimiter;
        private ObservableCollection<string> currentCSVColumnNames;
        private DataTable currentCSVFieldDatas;
        private bool applySettingsToAllModel;
        private string customDelimiter;
        private DataTable emptyDataTable;
        private int pageSize, pageNumber, pageCount;
        private ObservedCommand<string> pageCommand;
        private bool isCustomDelimiterEnabled;

        public CSVViewModel()
            : this(new Collection<CSVInfoModel>())
        { }

        public CSVViewModel(Collection<CSVInfoModel> entities)
        {
            this.csvModelList = new List<CSVModel>();

            foreach (CSVInfoModel entity in entities)
            {
                CsvFeatureLayer layer = new CsvFeatureLayer(entity.CSVFileName, entity.WktColumnName, entity.Delimiter);
                layer.YColumnName = entity.LatitudeColumnName;
                layer.XColumnName = entity.LongitudeColumnName;
                layer.SpatialColumnType = entity.MappingType;
                CSVModel model = new CSVModel(layer);
                model.PropertyChanged += (seder, e) =>
                {
                    if (e.PropertyName == "Delimiter")
                    {
                        RefreshColumnNamesAndFieldDatas();
                    }
                };

                this.csvModelList.Add(model);
            }

            currentCSVColumnNames = new ObservableCollection<string>();
            emptyDataTable = new DataTable();
            emptyDataTable.Columns.Add("Error");
            DataRow dr = emptyDataTable.NewRow();
            dr[0] = "Invalid Delimiter.";
            emptyDataTable.Rows.Add(dr);
            pageSize = 25;
            pageNumber = 1;

            SelectedCSVModel = this.csvModelList[0];
            SelectedCSVModel.CanAutoMatch = false;
            switch (SelectedCSVModel.Delimiter)
            {
                case ",":
                    SelectedDelimiter = new KeyValuePair<string, string>("Comma", ",");
                    break;
                case ".":
                    SelectedDelimiter = new KeyValuePair<string, string>("Dot", ".");
                    break;
                case "|":
                    SelectedDelimiter = new KeyValuePair<string, string>("Pipe", "|");
                    break;
                default:
                    CustomDelimiter = SelectedCSVModel.Delimiter;
                    SelectedDelimiter = new KeyValuePair<string, string>("Custom", "");
                    break;
            }
            SelectedCSVModel.CanAutoMatch = true;
        }

        public Visibility IsForMultipleFiles
        {
            get
            {
                return this.csvModelList.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public ObservedCommand<string> PageCommand
        {
            get
            {
                if (pageCommand == null)
                {
                    pageCommand = new ObservedCommand<string>((parameter) =>
                    {
                        switch (parameter)
                        {
                            case "previous": PageNumber--; break;
                            case "last": PageNumber = PageCount; break;
                            case "next": PageNumber++; break;
                            case "first":
                            default: PageNumber = 1; break;
                        }
                    }, (parameter) =>
                    {
                        switch (parameter)
                        {
                            case "first":
                            case "previous":
                                return PageNumber > 1;
                            case "last":
                            case "next":
                                return PageNumber < PageCount;
                            default: return true;
                        }
                    });
                }
                return pageCommand;
            }
        }

        public List<CSVModel> CSVModelList { get { return csvModelList; } }

        public ObservableCollection<string> CurrentCSVColumnNames { get { return currentCSVColumnNames; } }

        public KeyValuePair<string, string> SelectedDelimiter
        {
            get { return selectedDelimiter; }
            set
            {
                selectedDelimiter = value;
                IsCustomDelimiterEnabled = value.Key.Equals("Custom", StringComparison.InvariantCulture);
                if (IsCustomDelimiterEnabled && !String.IsNullOrEmpty(CustomDelimiter))
                {
                    SelectedCSVModel.Delimiter = CustomDelimiter;
                }
                else
                {
                    SelectedCSVModel.Delimiter = value.Value;
                }
                RaisePropertyChanged(() => SelectedDelimiter);
                RaisePropertyChanged(() => IsCustomDelimiterEnabled);
            }
        }

        public string CustomDelimiter
        {
            get { return customDelimiter; }
            set
            {
                customDelimiter = value;
                SelectedCSVModel.Delimiter = value;
                RaisePropertyChanged(() => CustomDelimiter);
            }
        }

        public CSVModel SelectedCSVModel
        {
            get { return selectedCSVModel; }
            set
            {
                selectedCSVModel = value;
                RefreshColumnNamesAndFieldDatas();
                RaisePropertyChanged(() => SelectedCSVModel);
            }
        }

        public DataTable CurrentCSVFieldDatas
        {
            get { return currentCSVFieldDatas; }
            set
            {
                currentCSVFieldDatas = value;
                InitPageCount();
                RaisePropertyChanged(() => CurrentPagedCSVFieldDatas);
                RaisePropertyChanged(() => CurrentCSVFieldDatas);
                RaisePropertyChanged(() => PageNumber);
            }
        }

        public DataTable CurrentPagedCSVFieldDatas
        {
            get
            {
                DataTable newDataTable = new DataTable();
                foreach (var column in CurrentCSVFieldDatas.Columns)
                {
                    newDataTable.Columns.Add(column.ToString());
                }

                foreach (var dataRow in CurrentCSVFieldDatas.AsEnumerable().Skip(PageSize * (PageNumber - 1)).Take(PageSize))
                {
                    DataRow newDataRow = newDataTable.NewRow();
                    for (int i = 0; i < dataRow.ItemArray.Length; i++)
                    {
                        newDataRow[i] = dataRow.ItemArray[i].ToString();
                    }
                    newDataTable.Rows.Add(newDataRow);
                }

                return newDataTable;
                //return CurrentCSVFieldDatas;
            }
        }

        public bool IsCustomDelimiterEnabled
        {
            get { return isCustomDelimiterEnabled; }
            set { isCustomDelimiterEnabled = value; }
        }

        public bool ApplySettingsToAllModel
        {
            get { return applySettingsToAllModel; }
            set
            {
                applySettingsToAllModel = value;
                if (value)
                {
                    foreach (var child in CSVModelList)
                    {
                        if (child != SelectedCSVModel)
                        {
                            SelectedCSVModel.CopySettingsTo(child);
                        }
                    }
                }

                RaisePropertyChanged(() => ApplySettingsToAllModel);
            }
        }

        public int PageCount
        {
            get { return pageCount; }
            set
            {
                pageCount = value;
                RaisePropertyChanged(() => PageCount);
            }
        }

        public int PageNumber
        {
            get { return pageNumber; }
            set
            {
                pageNumber = value;
                RaisePropertyChanged(() => CurrentPagedCSVFieldDatas);
                RaisePropertyChanged(() => PageNumber);
            }
        }

        public int PageSize
        {
            get { return pageSize; }
            set
            {
                pageSize = value;
                InitPageCount();
                RaisePropertyChanged(() => CurrentPagedCSVFieldDatas);
                RaisePropertyChanged(() => PageNumber);
                RaisePropertyChanged(() => PageSize);
            }
        }

        private void RefreshColumnNamesAndFieldDatas()
        {
            CurrentCSVColumnNames.Clear();

            foreach (var columnName in selectedCSVModel.GetAllColumns())
            {
                CurrentCSVColumnNames.Add(columnName);
            }

            CurrentCSVFieldDatas = currentCSVColumnNames.Count != 0 ? selectedCSVModel.GetDatas() : emptyDataTable;
        }

        private void InitPageCount()
        {
            pageCount = CurrentCSVFieldDatas.Rows.Count / PageSize;
            if (CurrentCSVFieldDatas.Rows.Count % PageSize != 0)
            {
                pageCount++;
            }

            pageNumber = 1;
            RaisePropertyChanged(() => PageCount);
        }
    }
}