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


using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for AddDbfColumnWindow.xaml
    /// </summary>
    public partial class AddDbfColumnWindow : Window
    {
        private int originalColumnLength = -1;

        public AddDbfColumnWindow(DbfColumn dbfColumn, IEnumerable<string> columnNames, DbfColumnMode columnMode, bool isEditing = false, string aliasName = "")
        {
            UnitTestHelper.ApplyWindowStyle(this);

            InitializeComponent();

            if (!EditorUIPlugin.IsRelateAndAliasEnabled)
            {
                AliasGrid.RowDefinitions[0].Height = new GridLength(0);
            }

            addNewColumnViewModel.ColumnNames = new List<string>(columnNames);

            if (isEditing)
            {
                ColumnValues.Visibility = Visibility.Visible;
            }
            else
            {
                ColumnValues.Visibility = Visibility.Collapsed;
            }

            if (dbfColumn != null)
            {
                addNewColumnViewModel.ColumnName = dbfColumn.ColumnName;
                addNewColumnViewModel.AliasName = aliasName;
                addNewColumnViewModel.ColumnType = dbfColumn.ColumnType;
                switch (columnMode)
                {
                    case DbfColumnMode.Empty:
                        addNewColumnViewModel.IsEmptyChecked = true;
                        break;
                    case DbfColumnMode.Calculated:
                        addNewColumnViewModel.IsCalculatedChecked = true;
                        break;
                    default:
                        break;
                }
                addNewColumnViewModel.DecimalLength = dbfColumn.DecimalLength;
                addNewColumnViewModel.Length = dbfColumn.Length;
                originalColumnLength = dbfColumn.Length;

                if (columnMode == DbfColumnMode.Calculated)
                {
                    addNewColumnViewModel.CalculationType = ((CalculatedDbfColumn)dbfColumn).CalculationType;
                    addNewColumnViewModel.MeasurementUnit = ((CalculatedDbfColumn)dbfColumn).AreaUnit;
                }

                addNewColumnViewModel.ColumnNames.Remove(dbfColumn.ColumnName);
            }

            Messenger.Default.Register<bool>(this, DataContext, ProcessAddNewColumnMessage);
            Closing += (s, e) => Messenger.Default.Unregister(this);
        }

        private void ProcessAddNewColumnMessage(bool msg)
        {
            if (msg)
            {
                System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.Yes;
                if (originalColumnLength > addNewColumnViewModel.Length)
                {
                    result = System.Windows.Forms.MessageBox.Show("This change will make the field size smaller than the orignal one, the data in DBF might be truncated. Do you want to continue?", "Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
                }
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    DialogResult = true;
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Invalid Setting(s)\r\n\r\nPlease make sure your setting conform to the following rules: \r\n\r\n1.The column name setting is populated.\r\n2.The column name doesn't already exist.\r\n3.The length is greater than 0.\r\n4.The decimal length is greater than 0 if double is chosen for the column type.", "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }

        public string AliasName
        {
            get { return (DataContext as AddNewColumnViewModel).AliasName; }
        }

        public DbfColumn DbfColumn
        {
            get
            {
                DbfColumn calculatedDbfColumn;

                if (addNewColumnViewModel.ColumnMode == ThinkGeo.MapSuite.GisEditor.Plugins.DbfColumnMode.Calculated)
                {
                    if (addNewColumnViewModel.CalculationType == CalculatedDbfColumnType.Length)
                    {
                        calculatedDbfColumn = new CalculatedDbfColumn(addNewColumnViewModel.ColumnName, DbfColumnType.Float, addNewColumnViewModel.Length, addNewColumnViewModel.DecimalLength, addNewColumnViewModel.CalculationType, addNewColumnViewModel.LengthUnit);
                    }
                    else
                    {
                        calculatedDbfColumn = new CalculatedDbfColumn(addNewColumnViewModel.ColumnName, DbfColumnType.Float, addNewColumnViewModel.Length, addNewColumnViewModel.DecimalLength, addNewColumnViewModel.CalculationType, addNewColumnViewModel.MeasurementUnit);
                    }
                }
                else
                {
                    calculatedDbfColumn = new DbfColumn(addNewColumnViewModel.ColumnName, addNewColumnViewModel.ColumnType, addNewColumnViewModel.Length, addNewColumnViewModel.DecimalLength);
                    calculatedDbfColumn.TypeName = addNewColumnViewModel.ColumnType.ToString();
                }

                return calculatedDbfColumn;
            }
        }

        [Obfuscation]
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}