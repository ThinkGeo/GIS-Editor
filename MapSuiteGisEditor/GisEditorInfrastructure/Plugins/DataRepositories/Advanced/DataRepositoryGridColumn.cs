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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ThinkGeo.MapSuite.GisEditor
{
    public class DataRepositoryGridColumn : GridViewColumn
    {
        private DataRepositoryGridCellConverter cellConverter;

        public DataRepositoryGridColumn()
            : this(string.Empty, 60, null)
        { }

        public DataRepositoryGridColumn(string header, double width)
            : this(header, width, null)
        { }

        public DataRepositoryGridColumn(string header, double width, Func<DataRepositoryItem, object> cellContentConverter)
        {
            Header = new TextBlock { Text = header };
            Width = width;

            cellConverter = new DataRepositoryGridCellConverter();
            CellContentConvertHandler = cellContentConverter;

            CellTemplate = new DataTemplate();
            CellTemplate.VisualTree = new FrameworkElementFactory(typeof(ContentControl));
            CellTemplate.VisualTree.SetBinding(ContentControl.ContentProperty, new Binding(".") { Converter = cellConverter });
        }

        public Func<DataRepositoryItem, object> CellContentConvertHandler
        {
            get { return cellConverter.CellContentConvertHandler; }
            set { cellConverter.CellContentConvertHandler = value; }
        }
    }
}
