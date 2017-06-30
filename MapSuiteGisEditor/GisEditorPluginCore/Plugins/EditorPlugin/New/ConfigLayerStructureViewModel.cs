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
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ConfigLayerStructureViewModel : ViewModelBase
    {
        private string layerName;
        private string tableName;
        private Uri layerUri;
        private GeneralShapeFileType shpFileType;
        private bool layerTypeIsEnable;
        private bool okIsEnable;
        private bool outputFolderIsEnable;
        private FeatureLayer featureLayer;
        private LayerPlugin layerPlugin;
        private ObservableCollection<FeatureSourceColumnItem> columnListItemSource;

        [NonSerialized]
        private RelayCommand<string> editCommand;

        [NonSerialized]
        private RelayCommand<string> removeCommand;

        [NonSerialized]
        private RelayCommand addCommand;

        private bool isEditEnabled;
        private bool isAliasEnabled;

        public ConfigLayerStructureViewModel(FeatureLayer featureLayer)
            : base()
        {
            shpFileType = GeneralShapeFileType.Area;
            columnListItemSource = new ObservableCollection<FeatureSourceColumnItem>();
            FeatureLayer = featureLayer;
            LayerPlugin = new ShapeFileFeatureLayerPlugin();
            isAliasEnabled = true;
            layerUri = new Uri(ConfigShapeFileViewModel.GetDefaultOutputPath());
        }

        public Visibility DeleteButtonVisibility
        {
            get { return IsAliasEnabled ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool IsAliasEnabled
        {
            get { return isAliasEnabled; }
            set
            {
                isAliasEnabled = value;
                RaisePropertyChanged(()=>IsAliasEnabled);
            }
        }

        public bool IsEditEnabled
        {
            get { return isEditEnabled; }
            set
            {
                isEditEnabled = value;
                RaisePropertyChanged(()=>IsEditEnabled);
            }
        }

        public LayerPlugin LayerPlugin
        {
            get { return layerPlugin; }
            set { layerPlugin = value; }
        }

        public ObservableCollection<FeatureSourceColumnItem> ColumnListItemSource
        {
            get
            {
                return columnListItemSource;
            }
            set
            {
                columnListItemSource = value;
                RaisePropertyChanged(()=>ColumnListItemSource);
            }
        }

        public string LayerName
        {
            get { return layerName; }
            set
            {
                layerName = value;
                RaisePropertyChanged(()=>LayerName);
            }
        }

        public string TableName
        {
            get { return tableName; }
            set
            {
                tableName = value;
                RaisePropertyChanged(()=>TableName);
            }
        }

        public Uri LayerUri
        {
            get
            {
                return layerUri;
            }
            set
            {
                layerUri = value;
                RaisePropertyChanged(()=>LayerUri);
            }
        }

        public GeneralShapeFileType ShpFileType
        {
            get { return shpFileType; }
            set
            {
                shpFileType = value;
                RaisePropertyChanged(()=>ShpFileType);
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
                        FeatureSourceColumnItem item = ColumnListItemSource.FirstOrDefault(i => { return i.Id == id; });

                        IEnumerable<string> columnNames = ColumnListItemSource.Select(dbfColumnItem => dbfColumnItem.ColumnName);

                        AddFeatureSourceColumnWindow window = new AddFeatureSourceColumnWindow(item, columnNames);
                        window.Title = GisEditor.LanguageManager.GetStringResource("AddFeatureSourceColumnWindowTitle");
                        if (window.ShowDialog().GetValueOrDefault())
                        {
                            FeatureSourceColumnItem newItem = new FeatureSourceColumnItem(item.OrignalColumnName);
                            FeatureSourceColumnItem tempItem = window.FeatureSourceColumnItem;
                            newItem.AliasName = tempItem.AliasName;
                            newItem.ColumnName = tempItem.ColumnName;
                            newItem.ColumnType = tempItem.ColumnType;
                            newItem.FeatureSourceColumn = tempItem.FeatureSourceColumn;
                            newItem.Id = tempItem.Id;
                            newItem.ChangedStatus = FeatureSourceColumnChangedStatus.Updated;
                            if (item.OrignalColumnName == "RECID")
                            {
                                newItem.ChangedStatus = FeatureSourceColumnChangedStatus.Added;
                            }
                            int index = ColumnListItemSource.IndexOf(item);
                            ColumnListItemSource.RemoveAt(index);
                            ColumnListItemSource.Insert(index, newItem);
                            IsOkEnabled = true;
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
                            FeatureSourceColumnItem item = ColumnListItemSource.FirstOrDefault(i => { return i.Id == id; });
                            item.ChangedStatus = FeatureSourceColumnChangedStatus.Deleted;
                            ColumnListItemSource.Remove(item);
                            okIsEnable = true;
                        }
                    });
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
                        IEnumerable<string> columnNames = ColumnListItemSource.Select(dbfColumnItem => dbfColumnItem.ColumnName);

                        AddFeatureSourceColumnWindow window = new AddFeatureSourceColumnWindow(null, columnNames);
                        window.Title = GisEditor.LanguageManager.GetStringResource("AddFeatureSourceColumnWindowTitle1");
                        if (window.ShowDialog().GetValueOrDefault())
                        {
                            FeatureSourceColumnItem newItem = window.FeatureSourceColumnItem;
                            newItem.ChangedStatus = FeatureSourceColumnChangedStatus.Added;
                            ColumnListItemSource.Add(newItem);
                            IsOkEnabled = true;
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
                RaisePropertyChanged(()=>LayerTypeIsEnable);
            }
        }

        public bool IsOkEnabled
        {
            get { return okIsEnable; }
            set
            {
                okIsEnable = value;
                RaisePropertyChanged(() => IsOkEnabled);
            }
        }

        public bool OutputFolderIsEnable
        {
            get { return outputFolderIsEnable; }
            set
            {
                outputFolderIsEnable = value;
                RaisePropertyChanged(()=>OutputFolderIsEnable);
            }
        }

        public FeatureLayer FeatureLayer
        {
            get { return featureLayer; }
            set
            {
                featureLayer = value;
                if (featureLayer == null)
                {
                    LayerName = string.Empty;
                    ShpFileType = GeneralShapeFileType.Area;
                    LayerTypeIsEnable = true;
                    okIsEnable = true;
                    OutputFolderIsEnable = true;
                    FeatureSourceColumnItem item = new FeatureSourceColumnItem("RECID");
                    item.ColumnName = "RECID";
                    item.ColumnType = "String";
                    item.AliasName = item.ColumnName;
                    item.ChangedStatus = FeatureSourceColumnChangedStatus.Added;
                    FeatureSourceColumn column = new FeatureSourceColumn(item.ColumnName, DbfColumnType.Character.ToString(), 10);
                    item.FeatureSourceColumn = column;
                    ColumnListItemSource.Add(item);
                    IsEditEnabled = true;
                    IsAliasEnabled = true;
                }
                else
                {
                    featureLayer.Open();
                    LayerName = featureLayer.Name;
                    TableName = ((FileGeoDatabaseFeatureLayer)featureLayer).TableName;

                    //FolderPath = Path.GetDirectoryName(featureLayer.ShapePathFileName);
                    var plugin = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).FirstOrDefault();

                    if (plugin != null) LayerUri = plugin.GetUri(featureLayer);

                    switch (featureLayer.FeatureSource.GetFirstFeaturesWellKnownType())
                    {
                        case WellKnownType.Multipoint:
                            ShpFileType = GeneralShapeFileType.Multipoint;
                            break;

                        case WellKnownType.Point:
                            ShpFileType = GeneralShapeFileType.Point;
                            break;

                        case WellKnownType.Line:
                        case WellKnownType.Multiline:
                            ShpFileType = GeneralShapeFileType.Line;
                            break;

                        case WellKnownType.Polygon:
                        case WellKnownType.Multipolygon:
                            ShpFileType = GeneralShapeFileType.Area;
                            break;
                    }

                    columnListItemSource.Clear();
                    foreach (var column in featureLayer.QueryTools.GetColumns())
                    {
                        FeatureSourceColumnItem item = new FeatureSourceColumnItem(column.ColumnName);
                        item.ColumnName = column.ColumnName;
                        item.ColumnType = column.TypeName;
                        item.FeatureSourceColumn = column;
                        item.FeatureSourceColumn.TypeName = column.TypeName;
                        columnListItemSource.Add(item);
                    }
                    LayerTypeIsEnable = false;
                    okIsEnable = false;
                    IsEditEnabled = false;
                    OutputFolderIsEnable = false;
                }
            }
        }
    }
}