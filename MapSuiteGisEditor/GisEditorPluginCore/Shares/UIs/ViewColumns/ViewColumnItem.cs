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


using GalaSoft.MvvmLight;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Obfuscation]
    internal class ViewColumnItem : ViewModelBase
    {
        private string id;
        private string columnName;
        private string columnType;
        private int columnLength;
        private string aliasName;
        private Visibility viewVisibility;
        private Visibility renameVisibility;
        private ContextMenu contextMenu;
        private FeatureSourceColumn column;

        public ViewColumnItem(FeatureSourceColumn column, string alias, bool isCalculated = false)
        {
            this.column = column;
            this.id = Guid.NewGuid().ToString();
            this.columnName = column.ColumnName;
            this.columnType = column.TypeName;
            this.columnLength = column.MaxLength;
            this.aliasName = alias;
            viewVisibility = Visibility.Visible;
            renameVisibility = Visibility.Collapsed;

            if (isCalculated)
            {
                contextMenu = new ContextMenu();

                MenuItem editItem = new MenuItem();
                editItem.Click += EditItem_Click;
                editItem.Header = "Edit";

                MenuItem deleteItem = new MenuItem();
                deleteItem.Click += DeleteItem_Click;
                deleteItem.Header = "Delete";

                contextMenu.Items.Add(editItem);
                contextMenu.Items.Add(deleteItem);
            }
        }

        public Action<FeatureSourceColumn> EditAction { get; set; }

        public Action<FeatureSourceColumn> DeleteAction { get; set; }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (DeleteAction != null)
            {
                DeleteAction(column);
            }
        }

        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            if (EditAction != null)
            {
                EditAction(column);
            }
        }

        public ContextMenu ContextMenu
        {
            get { return contextMenu; }
            set { contextMenu = value; }
        }

        public Visibility RenameVisibility
        {
            get { return renameVisibility; }
            set
            {
                renameVisibility = value;
                RaisePropertyChanged(() => RenameVisibility);
            }
        }

        public Visibility ViewVisibility
        {
            get { return viewVisibility; }
            set
            {
                viewVisibility = value;
                RaisePropertyChanged(() => ViewVisibility);
            }
        }

        public string AliasName
        {
            get { return aliasName; }
            set
            {
                aliasName = value;
                RaisePropertyChanged(() => AliasName);
            }
        }

        public string Id
        {
            get { return id; }
        }

        public string ColumnName
        {
            get { return columnName; }
        }

        public string ColumnType
        {
            get { return columnType; }
        }

        public int ColumnLength
        {
            get { return columnLength; }
            set { columnLength = value; }
        }
    }
}