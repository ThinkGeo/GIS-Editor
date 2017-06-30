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


using System.Collections.ObjectModel;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public static class GeoDbfExtension
    {
        public static Collection<DbfColumn> GetAllColumns(this GeoDbf geoDbf, bool includeNullColumnType = false)
        {
            Collection<DbfColumn> dbfColumns = new Collection<DbfColumn>();
            for (int i = 1; i <= geoDbf.ColumnCount; i++)
            {
                DbfColumn column = geoDbf.GetColumn(i);
                if (includeNullColumnType || column.ColumnType != DbfColumnType.Null)
                {
                    dbfColumns.Add(column);
                }
            }
            return dbfColumns;
        }
    }
}