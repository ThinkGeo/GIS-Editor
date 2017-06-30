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
using System.Reflection;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for AddFeatureSourceColumnWindow.xaml
    /// </summary>
    public partial class AddFeatureSourceColumnWindow : Window
    {
        private string tempColumnLength;

        public AddFeatureSourceColumnWindow(FeatureSourceColumnItem featureSourceColumnitem, IEnumerable<string> columnNames)
        {
            UnitTestHelper.ApplyWindowStyle(this);

            InitializeComponent();

            if (!EditorUIPlugin.IsRelateAndAliasEnabled)
            {
                mainGrid.RowDefinitions[1].Height = new GridLength(0);
            }

            tempColumnLength = addNewColumnViewModel.ColumnLength;
            addNewColumnViewModel.ColumnNames = new List<string>(columnNames);

            if (featureSourceColumnitem != null)
            {
                addNewColumnViewModel.ColumnName = featureSourceColumnitem.ColumnName;
                addNewColumnViewModel.AliasName = featureSourceColumnitem.AliasName;
                addNewColumnViewModel.ColumnType = (DbfColumnType)System.Enum.Parse(typeof(DbfColumnType), featureSourceColumnitem.ColumnType, true);
                addNewColumnViewModel.ColumnLength = featureSourceColumnitem.FeatureSourceColumn.MaxLength.ToString();

                addNewColumnViewModel.ColumnNames.Remove(featureSourceColumnitem.ColumnName);
            }

            Messenger.Default.Register<bool>(this, DataContext, ProcessAddNewColumnMessage);
            Closing += (s, e) => Messenger.Default.Unregister(this);
        }

        private void ProcessAddNewColumnMessage(bool msg)
        {
            if (msg)
            {
                DialogResult = true;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Invalid Setting(s)\r\n\r\nPlease make sure your setting conform to the following rules: \r\n\r\n1.The column name setting is populated.\r\n2.The column name doesn't already exist.\r\n3.The column name is less than 10 characters.\r\n4.The length is greater than 0.\r\n5.The length is less than 254.\r\n6.The decimal length is greater than 0 if double is chosen for the column type.", "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }

        public FeatureSourceColumnItem FeatureSourceColumnItem
        {
            get
            {
                FeatureSourceColumnItem featureSourceColumnItem;

                int columnLenght = int.Parse(addNewColumnViewModel.ColumnLength);

                //switch (addNewColumnViewModel.ColumnType)
                //{
                //    case DbfColumnType.Date:
                //        columnLenght = 8;
                //        break;

                //    case DbfColumnType.Double:
                //    case DbfColumnType.Integer:
                //    case DbfColumnType.String:
                //        columnLenght = 10;
                //        break;
                //    default:
                //        break;
                //}

                featureSourceColumnItem = new FeatureSourceColumnItem(addNewColumnViewModel.ColumnName);
                featureSourceColumnItem.AliasName = addNewColumnViewModel.AliasName;
                featureSourceColumnItem.ColumnName = addNewColumnViewModel.ColumnName;
                featureSourceColumnItem.ColumnType = addNewColumnViewModel.ColumnType.ToString();
                featureSourceColumnItem.FeatureSourceColumn = new FeatureSourceColumn(addNewColumnViewModel.ColumnName, addNewColumnViewModel.ColumnType.ToString(), columnLenght);
                featureSourceColumnItem.Id = Guid.NewGuid().ToString();

                return featureSourceColumnItem;
            }
        }

        [Obfuscation]
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        [Obfuscation]
        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            int result = 0;
            if (int.TryParse(columnLengthTb.Text, out result))
            {
                tempColumnLength = columnLengthTb.Text;
            }
            else
            {
                columnLengthTb.Text = tempColumnLength;
                columnLengthTb.SelectionStart = columnLengthTb.Text.Length;
            }
        }
    }
}