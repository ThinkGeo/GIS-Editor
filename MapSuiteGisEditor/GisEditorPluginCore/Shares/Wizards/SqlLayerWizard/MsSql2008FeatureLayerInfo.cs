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
using System.Linq;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class MsSql2008FeatureLayerInfo : DatabaseLayerInfo<MsSqlFeatureLayer>
    {
        private static readonly string selectTableColumnsSqlStatementTemplate = "use {0}; SELECT name FROM sys.columns WHERE (object_id = OBJECT_ID('{1}'))";
        private static readonly string selectTableNamesSqlStatement = "SELECT allTables.name,allSchemas.name as schema_name FROM sys.tables allTables left join sys.schemas allSchemas on allTables.schema_id = allSchemas.schema_id union (SELECT allViews.name,allSchemas.name as schema_name FROM sys.views allViews left join sys.schemas allSchemas on allViews.schema_id = allSchemas.schema_id) order by schema_name,name";

        public MsSql2008FeatureLayerInfo()
        {
            UseTrustAuthority = true;
            ServerName = "";
            UserName = "";
            Password = "";
            Description = GisEditor.LanguageManager.GetStringResource("MsSql2008FeatureLayerInfoDescription");
        }

        public string SchemaName { get; set; }

        protected override SimpleShapeType GetSimpleShapeTypeCore(MsSqlFeatureLayer layer)
        {
            if (!layer.IsOpen) layer.Open();
            var shapeType = layer.GetFirstGeometryType();
            var simpleShapeType = WellKnownTypeToSimpleShapeTypeConverter.To(shapeType);
            return simpleShapeType;
        }

        protected override MsSqlFeatureLayer CreateLayerCore()
        {
            string tempTableName = TableName;
            if (tempTableName.IndexOf('.') != -1)
            {
                string[] pattens = tempTableName.Split('.');
                SchemaName = pattens.First();
                tempTableName = pattens.ToList()[1];
            }
            var layer = new MsSqlFeatureLayer(GetToDatabaseConnectionString(DatabaseName), tempTableName, FeatureIDColumnName);
            layer.SchemaName = SchemaName;
            return layer;
        }

        public Collection<string> CollectViewsFromDatabase(string databaseName)
        {
            Collection<string> views = new Collection<string>();

            string statement = @"use {0};
                            select distinct o.name, s.TABLE_SCHEMA as schema_name from sysobjects o
            	            left join syscolumns c on o.id = c.id
            	            left join INFORMATION_SCHEMA.VIEWS s on o.name=s.TABLE_NAME
							where s.TABLE_SCHEMA != ''
            	            and c.xtype = '240'
							and o.xtype = 'V' ";

            statement = String.Format(CultureInfo.InvariantCulture, statement, databaseName);

            ExecuteDB(GetToDatabaseConnectionString(DatabaseName), statement, reader =>
            {
                while (reader.Read())
                {
                    string viewName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", reader.GetString(1), reader.GetString(0));
                    views.Add(viewName);
                }
            });

            return views;
        }

        public Collection<string> CollectTablesFromDatabase(string databaseName)
        {
            //string statement = "select o.name, c.name as ColumnName from " + databaseName + "..sysobjects o left join " + databaseName + "..syscolumns c on o.id = c.id where o.xtype = 'U' and c.xtype = '240'";

            string statement = @"use {0};
                select o.name, s.TABLE_SCHEMA as schema_name from sysobjects o
	            left join syscolumns c on o.id = c.id
	            left join INFORMATION_SCHEMA.TABLES s on o.name=s.TABLE_NAME
	            where o.xtype = 'U' and c.xtype = '240'";

            statement = String.Format(CultureInfo.InvariantCulture, statement, databaseName);

            Collection<string> tableNames = new Collection<string>();
            ExecuteDB(GetToDatabaseConnectionString(DatabaseName), statement, reader =>
            {
                while (reader.Read())
                {
                    string tableName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", reader.GetString(1), reader.GetString(0));
                    tableNames.Add(tableName);
                }
            }); ;

            return tableNames;
        }

        protected override Collection<string> CollectTablesFromDatabaseCore()
        {
            Collection<string> tableNames = new Collection<string>();
            ExecuteDB(GetToDatabaseConnectionString(DatabaseName), selectTableNamesSqlStatement, reader =>
            {
                while (reader.Read())
                {
                    string tableName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", reader.GetString(1), reader.GetString(0));
                    tableNames.Add(tableName);
                }
            });

            return tableNames;
        }

        protected override Collection<string> CollectColumnsFromTableCore()
        {
            Collection<string> columnNames = new Collection<string>();
            string tempTableName = TableName;
            if (!string.IsNullOrEmpty(SchemaName))
            {
                tempTableName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", SchemaName, tempTableName);
            }
            ExecuteDB(GetToDatabaseConnectionString(DatabaseName), GetColumnsSqlStatement(DatabaseName, tempTableName), reader =>
            {
                while (reader.Read())
                {
                    columnNames.Add(reader.GetString(0));
                }
            });

            return columnNames;
        }

        protected override Collection<string> CollectDatabaseFromServerCore()
        {
            Collection<string> databases = new Collection<string>();
            ExecuteDB(GetToServerConnectionString(), "sp_databases", reader =>
            {
                while (reader.Read())
                {
                    databases.Add(reader.GetString(0));
                }
            }, CommandType.StoredProcedure);

            return databases;
        }

        private string GetToDatabaseConnectionString(string databaseName)
        {
            StringBuilder connectionBuilder = new StringBuilder();
            connectionBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}Initial Catalog={1};", GetToServerConnectionString(), databaseName);
            return connectionBuilder.ToString();
        }

        private string GetToServerConnectionString()
        {
            StringBuilder connectionBuilder = new StringBuilder();
            connectionBuilder.AppendFormat(CultureInfo.InvariantCulture, "Data Source={0};Persist Security Info=True;", ServerName);
            if (UseTrustAuthority)
            {
                connectionBuilder.Append("Trusted_Connection=Yes;");
            }
            else
            {
                connectionBuilder.AppendFormat(CultureInfo.InvariantCulture, "User ID={0};Password={1};", UserName, Password);
            }

            return connectionBuilder.ToString();
        }

        private string GetColumnsSqlStatement(string databaseName, string tableName)
        {
            return string.Format(CultureInfo.InvariantCulture, selectTableColumnsSqlStatementTemplate, databaseName, tableName);
        }

        private void ExecuteDB(string connectionString, string commandStatement, Action<SqlDataReader> readerExecuted, CommandType commandType = CommandType.Text)
        {
            SqlConnection connection = null;
            SqlCommand command = null;
            SqlDataReader reader = null;

            try
            {
                connection = new SqlConnection(connectionString);
                command = new SqlCommand(commandStatement, connection);
                command.CommandType = commandType;

                connection.Open();
                reader = command.ExecuteReader();

                if (readerExecuted != null) readerExecuted(reader);
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            finally
            {
                if (connection != null) connection.Dispose();
                if (command != null) command.Dispose();
                if (reader != null) reader.Dispose();
            }
        }
    }
}