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


using CSScriptLibrary;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class LabelFunctionsViewModel : ViewModelBase
    {
        private string scriptText;
        private Dictionary<string, string> columnNames;
        private RelayCommand testCommand;
        private RelayCommand viewDataCommand;
        private RelayCommand addColumnCommand;
        private RelayCommand importCodeCommand;
        private RelayCommand removeColumnCommand;
        private ObservableCollection<labelColumnItem> columnItems;

        public LabelFunctionsViewModel(Dictionary<string, string> columnNames, Dictionary<string, string> nameColumnDic)
        {
            this.columnNames = columnNames;
            columnItems = new ObservableCollection<labelColumnItem>();

            foreach (var name in nameColumnDic)
            {
                labelColumnItem item = new labelColumnItem(columnNames, name.Key, columnNames.FirstOrDefault(c => c.Key == name.Value));
                columnItems.Add(item);
            }

            if (columnItems.Count == 0)
            {
                labelColumnItem item = labelColumnItem.CreateColumnItem(columnNames, columnItems.Count + 1);
                columnItems.Add(item);
            }

            scriptText = "return column1;";
        }

        public ObservableCollection<labelColumnItem> ColumnItems
        {
            get { return columnItems; }
        }

        public string ScriptText
        {
            get { return scriptText; }
            set
            {
                scriptText = value;
                RaisePropertyChanged(() => ScriptText);
            }
        }

        public Dictionary<string, string> LabelFunctionColumnNames
        {
            get
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                foreach (var item in ColumnItems)
                {
                    result.Add(item.RealName, item.SelectionColumn.Key);
                }
                return result;
            }
        }

        public RelayCommand AddColumnCommand
        {
            get
            {
                if (addColumnCommand == null)
                {
                    addColumnCommand = new RelayCommand(() =>
                    {
                        if (columnItems.Count < 3)
                        {
                            labelColumnItem item = labelColumnItem.CreateColumnItem(columnNames, columnItems.Count + 1);
                            columnItems.Add(item);
                        }
                        else
                        {
                            MessageBox.Show("Only support a maximum of 3 columns.");
                        }
                    });
                }
                return addColumnCommand;
            }
        }

        public RelayCommand ViewDataCommand
        {
            get
            {
                if (viewDataCommand == null)
                {
                    viewDataCommand = new RelayCommand(() =>
                    {
                        DataViewerUserControl content = new DataViewerUserControl();
                        content.ShowDialog();
                    });
                }
                return viewDataCommand;
            }
        }

        public RelayCommand ImportCodeCommand
        {
            get
            {
                if (importCodeCommand == null)
                {
                    importCodeCommand = new RelayCommand(() =>
                    {

                    });
                }
                return importCodeCommand;
            }
        }

        public RelayCommand TestCommand
        {
            get
            {
                if (testCommand == null)
                {
                    testCommand = new RelayCommand(() =>
                    {
                        string message = Test();
                        if (string.IsNullOrEmpty(message))
                        {
                            MessageBox.Show("Test pass.");
                        }
                        else
                        {
                            MessageBox.Show(message, "Info", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    });
                }
                return testCommand;
            }
        }

        public RelayCommand RemoveColumnCommand
        {
            get
            {
                if (removeColumnCommand == null)
                {
                    removeColumnCommand = new RelayCommand(() =>
                    {
                        if (columnItems.Count > 1)
                        {
                            labelColumnItem item = columnItems.LastOrDefault();
                            if (item != null)
                            {
                                columnItems.Remove(item);
                            }
                        }
                    });
                }
                return removeColumnCommand;
            }
        }

        public string Test()
        {
            try
            {
                string parameters = "";
                foreach (var item in LabelFunctionColumnNames)
                {
                    parameters += "string ";
                    parameters += item.Key;
                    parameters += ",";
                }
                parameters = parameters.TrimEnd(',');

                string code = LoadString();
                code = code.Replace("#CODE#", ScriptText);
                code = code.Replace("#PARAMETERS#", parameters);

                Assembly assembly = CSScript.LoadCode(code);
                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static string LoadString()
        {
            return @"using System;
                            using System.Linq;
        
                            public class ScriptTemplate
                            {
                                public static string Execute(#PARAMETERS#)
                                {
        		                   #CODE#
                                }
                            }";
        }
    }
}
