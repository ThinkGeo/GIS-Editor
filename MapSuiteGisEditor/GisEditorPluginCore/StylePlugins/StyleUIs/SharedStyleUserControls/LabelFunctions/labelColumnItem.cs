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
using System.Linq;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class labelColumnItem
    {
        private string displayName;
        private string actualName;
        private KeyValuePair<string, string> selectionColumn;
        private Dictionary<string, string> columnNames;

        public labelColumnItem()
        {
        }

        public labelColumnItem(Dictionary<string, string> columnNames)
            : this(columnNames, string.Empty, new KeyValuePair<string, string>())
        { }

        public labelColumnItem(Dictionary<string, string> columnNames, string realName, KeyValuePair<string, string> selectedColumn)
        {
            if (columnNames != null && columnNames.Count > 0)
            {
                this.columnNames = columnNames;
            }
            else
            {
                this.columnNames = new Dictionary<string, string>();
            }

            if (!string.IsNullOrEmpty(selectedColumn.Key) && !string.IsNullOrEmpty(selectedColumn.Value))
            {
                this.selectionColumn = selectedColumn;
            }
            else
            {
                selectionColumn = columnNames.FirstOrDefault();
            }

            if (!string.IsNullOrEmpty(realName))
            {
                this.actualName = realName;
            }
            else
            {
                actualName = "column1";
            }

            displayName = string.Format("var {0} = ", actualName);
        }

        public KeyValuePair<string, string> SelectionColumn
        {
            get { return selectionColumn; }
            set { selectionColumn = value; }
        }

        public Dictionary<string, string> ColumnNames
        {
            get { return columnNames; }
        }

        public string RealName
        {
            get { return actualName; }
        }

        public string DisplayName
        {
            get { return displayName; }
        }

        internal static labelColumnItem CreateColumnItem(Dictionary<string, string> columnNames, int index)
        {
            string realName = string.Format("column{0}", index);
            labelColumnItem item = new labelColumnItem(columnNames, realName, columnNames.FirstOrDefault());
            return item;
        }
    }
}
