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
using System.IO;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ConfigShapeFileViewModel : ViewModelBase
    {
        private bool isEditEnabled;
        private bool isAliasEnabled;
        private string layerName;
        private string folderPath;
        private GeneralShapeFileType shpFileType;
        private bool layerTypeIsEnable;
        private bool okIsEnable;
        private ShapeFileFeatureLayer shpFileFeatureLayer;
        private Dictionary<string, KeyValuePair<string, string>> editingColumnNames;
        private Collection<DbfColumn> addedColumns;
        private Dictionary<string, int> truncatedColumns;
        private Collection<DbfColumn> deleteColumns;
        private ObservableCollection<DbfColumnItem> columnListItemSource;

        [NonSerialized]
        private RelayCommand<string> editCommand;

        [NonSerialized]
        private RelayCommand<string> removeCommand;

        [NonSerialized]
        private RelayCommand addCommand;

        public ConfigShapeFileViewModel(ShapeFileFeatureLayer shpLayer)
            : base()
        {
            editingColumnNames = new Dictionary<string, KeyValuePair<string, string>>();
            columnListItemSource = new ObservableCollection<DbfColumnItem>();
            ShpFileFeatureLayer = shpLayer;
            folderPath = GetDefaultOutputPath();
            isAliasEnabled = true;
            addedColumns = new Collection<DbfColumn>();
            deleteColumns = new Collection<DbfColumn>();
            truncatedColumns = new Dictionary<string, int>();
        }

        public static string GetDefaultOutputPath()
        {
            string gisEditorFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "MapSuiteGisEditor");
            string tempFolderPath = Path.Combine(gisEditorFolder, "Output");
            if (!Directory.Exists(tempFolderPath))
            {
                Directory.CreateDirectory(tempFolderPath);
            }
            return tempFolderPath;
        }

        public Collection<DbfColumn> AddedColumns
        {
            get { return addedColumns; }
        }

        public Dictionary<string, int> TruncatedColumns
        {
            get { return truncatedColumns; }
        }

        public Collection<DbfColumn> DeleteColumns
        {
            get { return deleteColumns; }
        }

        public bool IsAliasEnabled
        {
            get { return isAliasEnabled; }
            set
            {
                isAliasEnabled = value;
                RaisePropertyChanged(() => IsAliasEnabled);
            }
        }

        public bool IsEditEnabled
        {
            get { return isEditEnabled; }
            set
            {
                isEditEnabled = value;
                RaisePropertyChanged(() => IsEditEnabled);
            }
        }

        public Visibility DeleteButtonVisibility
        {
            get { return IsAliasEnabled ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Dictionary<string, KeyValuePair<string, string>> EditingColumnNames
        {
            get { return editingColumnNames; }
        }

        public ObservableCollection<DbfColumnItem> ColumnListItemSource
        {
            get
            {
                return columnListItemSource;
            }
            set
            {
                columnListItemSource = value;
                RaisePropertyChanged(() => ColumnListItemSource);
            }
        }

        public string LayerName
        {
            get { return layerName; }
            set
            {
                layerName = value;
                RaisePropertyChanged(() => LayerName);
            }
        }

        public string FolderPath
        {
            get
            {
                return folderPath;
            }
            set
            {
                folderPath = value;
                RaisePropertyChanged(() => FolderPath);
            }
        }

        public GeneralShapeFileType ShpFileType
        {
            get { return shpFileType; }
            set
            {
                shpFileType = value;
                RaisePropertyChanged(() => ShpFileType);
            }
        }

        public RelayCommand<string> EditCommand
        {
            get
            {
                if (editCommand == null)
                {
                    editCommand = new RelayCommand<string>((id) =>
                    {
                        DbfColumnItem item = ColumnListItemSource.FirstOrDefault(i => { return i.Id == id; });
                        IEnumerable<string> columnNames = ColumnListItemSource.Select<DbfColumnItem, string>(
                dbfColumnItem => dbfColumnItem.AliasName);
                        bool isEditColumn = false;
                        Collection<FeatureSourceColumn> columns = new Collection<FeatureSourceColumn>();
                        if (ShpFileFeatureLayer != null)
                        {
                            ShpFileFeatureLayer.SafeProcess(() =>
                            {
                                columns = ShpFileFeatureLayer.FeatureSource.GetColumns();
                            });
                            isEditColumn = true;
                        }
                        AddDbfColumnWindow window = new AddDbfColumnWindow(item.DbfColumn, columnNames, item.ColumnMode, isEditColumn, item.AliasName);
                        window.Title = GisEditor.LanguageManager.GetStringResource("AddFeatureSourceColumnWindowTitle");
                        window.Owner = Application.Current.MainWindow;
                        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        AddNewColumnViewModel addNewColumnViewModel = window.DataContext as AddNewColumnViewModel;
                        if (window.ShowDialog().GetValueOrDefault())
                        {
                            if (addNewColumnViewModel != null)
                            {
                                if (item.DbfColumn.Length > window.DbfColumn.Length)
                                {
                                    truncatedColumns[window.DbfColumn.ColumnName] = window.DbfColumn.Length;
                                }

                                DbfColumnItem newItem = new DbfColumnItem(item.OrignalColumnName);
                                if (columns.Any(c => c.ColumnName.Equals(newItem.OrignalColumnName)))
                                {
                                    newItem.ChangedStatus = FeatureSourceColumnChangedStatus.Updated;
                                }
                                else
                                {
                                    newItem.ChangedStatus = FeatureSourceColumnChangedStatus.Added;
                                }
                                if (item.OrignalColumnName == "RECID" && !isEditColumn)
                                {
                                    newItem.ChangedStatus = FeatureSourceColumnChangedStatus.Added;
                                }
                                newItem.DbfColumn = window.DbfColumn;
                                if (newItem.ChangedStatus != FeatureSourceColumnChangedStatus.Added && newItem.DbfColumn is CalculatedDbfColumn)
                                {
                                    newItem.ChangedStatus = FeatureSourceColumnChangedStatus.Updated;
                                }
                                string tempAlias = window.AliasName;
                                newItem.AliasName = tempAlias;
                                if (ColumnListItemSource.Any(c => c.AliasName == tempAlias && c.OrignalColumnName != newItem.OrignalColumnName))
                                {
                                    MessageBox.Show(GisEditor.LanguageManager.GetStringResource("AliasDuplicateValidation"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxInfoCaption"), MessageBoxButton.OK);
                                    newItem.AliasName = item.AliasName;
                                }
                                newItem.ColumnName = window.DbfColumn.ColumnName;
                                newItem.ColumnType = window.DbfColumn.ColumnType.ToString();
                                newItem.ColumnMode = (DbfColumnMode)window.addNewColumnViewModel.ColumnMode;

                                int index = ColumnListItemSource.IndexOf(item);
                                ColumnListItemSource.RemoveAt(index);
                                ColumnListItemSource.Insert(index, newItem);
                            }
                        }
                    });
                }
                return editCommand;
            }
        }

        public RelayCommand<string> RemoveCommand
        {
            get
            {
                if (removeCommand == null)
                {
                    removeCommand = new RelayCommand<string>((id) =>
                    {
                        MessageBoxResult defaultResult = MessageBox.Show(GisEditor.LanguageManager.GetStringResource("AreYouDeleteSelectedColumnText"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxInfoCaption"), MessageBoxButton.YesNo, MessageBoxImage.Asterisk);

                        if (defaultResult == MessageBoxResult.Yes)
                        {
                            DbfColumnItem item = ColumnListItemSource.FirstOrDefault(i => { return i.Id == id; });
                            item.ChangedStatus = FeatureSourceColumnChangedStatus.Deleted;
                            ColumnListItemSource.Remove(item);
                            okIsEnable = true;
                        }
                    }, s => IsAliasEnabled);
                }
                return removeCommand;
            }
        }

        public RelayCommand AddCommand
        {
            get
            {
                if (addCommand == null)
                {
                    addCommand = new RelayCommand(() =>
                    {
                        IEnumerable<string> columnNames = ColumnListItemSource.Select<DbfColumnItem, string>(
                dbfColumnItem => dbfColumnItem.AliasName);
                        AddDbfColumnWindow window = new AddDbfColumnWindow(null, columnNames, DbfColumnMode.Empty, true);
                        window.Owner = Application.Current.MainWindow;
                        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        if (window.ShowDialog().GetValueOrDefault())
                        {
                            DbfColumn dbfColumn = window.DbfColumn;
                            DbfColumnItem newItem = new DbfColumnItem(dbfColumn.ColumnName);
                            newItem.ChangedStatus = FeatureSourceColumnChangedStatus.Added;
                            newItem.DbfColumn = dbfColumn;
                            newItem.AliasName = window.AliasName;
                            newItem.ColumnName = dbfColumn.ColumnName;
                            newItem.ColumnType = dbfColumn.ColumnType.ToString();
                            newItem.ColumnMode = (DbfColumnMode)window.addNewColumnViewModel.ColumnMode;
                            ColumnListItemSource.Add(newItem);
                        }
                    });
                }
                return addCommand;
            }
        }

        public bool LayerTypeIsEnable
        {
            get { return layerTypeIsEnable; }
            set
            {
                layerTypeIsEnable = value;
                RaisePropertyChanged(() => LayerTypeIsEnable);
            }
        }

        public bool OKIsEnable
        {
            get { return okIsEnable; }
            set
            {
                okIsEnable = value;
                RaisePropertyChanged(() => OKIsEnable);
            }
        }

        public ShapeFileFeatureLayer ShpFileFeatureLayer
        {
            get { return shpFileFeatureLayer; }
            set
            {
                shpFileFeatureLayer = value;
                if (shpFileFeatureLayer == null)
                {
                    LayerName = string.Empty;
                    ShpFileType = GeneralShapeFileType.Area;
                    LayerTypeIsEnable = true;
                    okIsEnable = true;

                    DbfColumnItem item = new DbfColumnItem("RECID");
                    item.ColumnName = "RECID";
                    item.ColumnType = "string";
                    item.AliasName = item.ColumnName;
                    item.ChangedStatus = FeatureSourceColumnChangedStatus.Added;
                    DbfColumn column = new DbfColumn(item.ColumnName, DbfColumnType.Character, 10, 0);
                    column.MaxLength = 10;
                    column.TypeName = item.ColumnType;
                    item.DbfColumn = column;
                    ColumnListItemSource.Add(item);
                    IsEditEnabled = true;
                    IsAliasEnabled = true;
                }
                else
                {
                    shpFileFeatureLayer.Open();
                    LayerName = shpFileFeatureLayer.Name;
                    FolderPath = Path.GetDirectoryName(shpFileFeatureLayer.ShapePathFilename);
                    switch (shpFileFeatureLayer.GetShapeFileType())
                    {
                        case ShapeFileType.Multipoint:
                            ShpFileType = GeneralShapeFileType.Multipoint;
                            break;

                        case ShapeFileType.Point:
                            ShpFileType = GeneralShapeFileType.Point;
                            break;

                        case ShapeFileType.Polyline:
                        case ShapeFileType.PolylineM:
                        case ShapeFileType.PolylineZ:
                            ShpFileType = GeneralShapeFileType.Line;
                            break;

                        case ShapeFileType.Polygon:
                        case ShapeFileType.PolygonM:
                        case ShapeFileType.PolygonZ:
                            ShpFileType = GeneralShapeFileType.Area;
                            break;
                    }
                    columnListItemSource.Clear();
                    foreach (var dbfColumn in ((ShapeFileFeatureSource)shpFileFeatureLayer.FeatureSource).GetDbfColumns())
                    {
                        if (!string.IsNullOrEmpty(dbfColumn.ColumnName))
                        {
                            DbfColumnItem item = new DbfColumnItem(dbfColumn.ColumnName);
                            item.ColumnName = dbfColumn.ColumnName;
                            item.AliasName = shpFileFeatureLayer.FeatureSource.GetColumnAlias(dbfColumn.ColumnName);
                            item.ColumnType = dbfColumn.ColumnType.ToString();
                            item.DbfColumn = dbfColumn;
                            EditorUIPlugin editorPlugin = GisEditor.UIManager.GetActiveUIPlugins<EditorUIPlugin>().FirstOrDefault();
                            if (editorPlugin != null)
                            {
                                string id = shpFileFeatureLayer.FeatureSource.Id;
                                if (editorPlugin.CalculatedColumns.ContainsKey(id))
                                {
                                    var calculateColumn = editorPlugin.CalculatedColumns[id].FirstOrDefault(c => c.ColumnName == dbfColumn.ColumnName);
                                    if (calculateColumn != null)
                                    {
                                        item.ColumnMode = DbfColumnMode.Calculated;
                                        item.DbfColumn = calculateColumn;
                                    }
                                }
                            }
                            columnListItemSource.Add(item);
                        }
                    }
                    LayerTypeIsEnable = false;
                    IsEditEnabled = false;
                    okIsEnable = false;
                }
            }
        }

        public string GetInvalidMessage()
        {
            string message = string.Empty;
            if (string.IsNullOrEmpty(LayerName))
            {
                message = "Layer name can't be empty.";
            }
            if (!ValidateFileName(LayerName))
            {
                message = "A file name can't contain any of the following characters:" + Environment.NewLine + "\\ / : * ? \" < > |";
            }
            else if (ColumnListItemSource.Count == 0)
            {
                message = "There must be at least one column.";
            }
            else if (!Directory.Exists(FolderPath))
            {
                message = "Folder path is invalid.";
            }
            return message;
        }

        private bool ValidateFileName(string fileName)
        {
            return !(fileName.Contains("\\")
                || fileName.Contains("/")
                || fileName.Contains(":")
                || fileName.Contains("*")
                || fileName.Contains("?")
                || fileName.Contains("\"")
                || fileName.Contains("<")
                || fileName.Contains(">")
                || fileName.Contains("|")
                );
        }
    }
}