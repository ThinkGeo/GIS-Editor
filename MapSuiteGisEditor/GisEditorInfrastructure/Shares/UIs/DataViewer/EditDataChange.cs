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


using System.Windows.Controls;
using System.Data;

namespace ThinkGeo.MapSuite.GisEditor
{
    internal class EditDataChange
    {
        private string originalValue;
        private string newValue;
        private string columnName;
        private string featureID;
        private DataGridCell gridCell;
        private DataRow dataRow;
        private EditDataShortcutKeyMode shortcutKeyMode;

        public EditDataChange(string originalValue, string newValue, string columnName, string featureID, DataGridCell gridCell, DataRow dataRow)
        {
            this.originalValue = originalValue;
            this.newValue = newValue;
            this.columnName = columnName;
            this.featureID = featureID;
            this.gridCell = gridCell;
            this.dataRow = dataRow;
            shortcutKeyMode = EditDataShortcutKeyMode.Undo;
        }

        public string FeatureID
        {
            get { return featureID; }
        }

        public string ColumnName
        {
            get { return columnName; }
        }

        public string NewValue
        {
            get { return newValue; }
        }

        public EditDataShortcutKeyMode ShortcutKeyMode
        {
            get { return shortcutKeyMode; }
            set { shortcutKeyMode = value; }
        }
        
        public DataGridCell GridCell
        {
            get { return gridCell; }
            set { gridCell = value; }
        }

        public void Undo()
        {
            dataRow[columnName] = originalValue;
            TextBlock textBlock = GridCell.Content as TextBlock;
            if (textBlock != null)
            {
                textBlock.Text = originalValue;
            }
            shortcutKeyMode = EditDataShortcutKeyMode.Redo;
        }

        public void Redo()
        {
            dataRow[columnName] = newValue;
            TextBlock textBlock = GridCell.Content as TextBlock;
            if (textBlock != null)
            {
                textBlock.Text = newValue;
            }
            shortcutKeyMode = EditDataShortcutKeyMode.Undo;
        }
    }
}
