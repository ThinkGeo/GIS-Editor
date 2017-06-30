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
using System.ComponentModel;
using System.Data;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DataViewerViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private DataTable originalDataTable;

        private int currentPageNumber;
        private int pageCount;
        private int pageSize;
        private DataTable currentDataTable;
        private ObservedCommand<string> pageCommand;

        public DataViewerViewModel(DataTable dataTable)
        {
            originalDataTable = dataTable;
            CurrentPageNumber = 1;
            pageSize = 20;
            PageCount = originalDataTable.Rows.Count % PageSize == 0 ? originalDataTable.Rows.Count / PageSize : (originalDataTable.Rows.Count / PageSize) + 1;

            CurrentDataTable = new DataTable();
            foreach (DataColumn dataColumn in originalDataTable.Columns)
            {
                CurrentDataTable.Columns.Add(dataColumn.ColumnName);
            }
            int value = PageSize <= originalDataTable.Rows.Count ? PageSize : originalDataTable.Rows.Count;
            for (int i = 0; i < value; i++)
            {
                DataRow dataRow = CurrentDataTable.NewRow();
                foreach (DataColumn dataColumn in CurrentDataTable.Columns)
                {
                    dataRow[dataColumn.ColumnName] = originalDataTable.Rows[i][dataColumn.ColumnName];
                }
                CurrentDataTable.Rows.Add(dataRow);
            }
        }

        public int CurrentPageNumber
        {
            get { return currentPageNumber; }
            set
            {
                currentPageNumber = value;
                OnPropertyChanged("CurrentPageNumber");
            }
        }

        public int PageCount
        {
            get { return pageCount; }
            set
            {
                pageCount = value;
                OnPropertyChanged("PageCount");
            }
        }

        public int PageSize
        {
            get { return pageSize; }
            set
            {
                pageSize = value;
                OnPropertyChanged("PageSize");
                PageCount = originalDataTable.Rows.Count % PageSize == 0 ? originalDataTable.Rows.Count / PageSize : (originalDataTable.Rows.Count / PageSize) + 1;
                RecreateTable();
            }
        }

        public DataTable CurrentDataTable
        {
            get { return currentDataTable; }
            set
            {
                currentDataTable = value;
                OnPropertyChanged("CurrentPage");
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
                            case "first":
                                CurrentPageNumber = 1;
                                break;
                            case "previous":
                                CurrentPageNumber--;
                                break;
                            case "next":
                                CurrentPageNumber++;
                                break;
                            case "last":
                                CurrentPageNumber = PageCount;
                                break;
                        }

                        CurrentDataTable.Rows.Clear();
                        RecreateTable();
                    },
                    (parameter) =>
                    {
                        switch (parameter)
                        {
                            case "first":
                                return CurrentPageNumber != 1;
                            case "previous":
                                return CurrentPageNumber != 1;
                            case "next":
                                return CurrentPageNumber != PageCount;
                            case "last":
                            default:
                                return CurrentPageNumber != PageCount;

                        }
                    });
                }
                return pageCommand;
            }
        }

        private void RecreateTable()
        {
            int maxValue = CurrentPageNumber * PageSize < originalDataTable.Rows.Count ? CurrentPageNumber * PageSize : originalDataTable.Rows.Count;
            CurrentDataTable.Rows.Clear();
            for (int i = (CurrentPageNumber - 1) * PageSize; i < maxValue; i++)
            {
                DataRow dataRow = CurrentDataTable.NewRow();
                foreach (DataColumn dataColumn in CurrentDataTable.Columns)
                {
                    dataRow[dataColumn.ColumnName] = originalDataTable.Rows[i][dataColumn.ColumnName];
                }
                CurrentDataTable.Rows.Add(dataRow);
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
