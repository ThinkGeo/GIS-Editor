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


//using System;
//using System.Data;
//using System.Data.OleDb;

//namespace ThinkGeo.MapSuite.GisEditor.Plugins
//{
//    internal class OleDbUtil : IDisposable
//    {
//        private static OleDbConnection dbConnection;
//        private static OleDbUtil currentInstance;

//        public OleDbUtil(string connectionString = "")
//        {
//            dbConnection = new OleDbConnection();
//            if (!String.IsNullOrEmpty(connectionString)) dbConnection.ConnectionString = connectionString;
//        }

//        public string ConnectionString { get { return dbConnection.ConnectionString; } set { dbConnection.ConnectionString = value; } }

//        public bool IsDisposed { get; set; }

//        protected OleDbConnection DbConnection { get { return dbConnection; } set { dbConnection = value; } }

//        public void Open()
//        {
//            OpenCore();
//        }

//        protected virtual void OpenCore()
//        {
//            if (String.IsNullOrEmpty(ConnectionString))
//            {
//                throw new InvalidOperationException("ConnectionString is Empty.");
//            }

//            dbConnection.Open();
//        }

//        public void Close()
//        {
//            CloseCore();
//        }

//        protected virtual void CloseCore()
//        {
//            dbConnection.Close();
//        }

//        public OleDbDataReader ExecuteReader(string statement)
//        {
//            using (OleDbCommand oleDbCommand = new OleDbCommand(statement, dbConnection))
//                return oleDbCommand.ExecuteReader();
//        }

//        public T ExecuteScalar<T>(string statement)
//        {
//            return (T)ExecuteScalar(statement);
//        }

//        public object ExecuteScalar(string statement)
//        {
//            using (OleDbCommand oleDbCommand = new OleDbCommand(statement, dbConnection))
//                return oleDbCommand.ExecuteScalar();
//        }

//        public DataSet GetDataSet(string statement)
//        {
//            using (OleDbDataAdapter oleDbAdapter = new OleDbDataAdapter(statement, dbConnection))
//            {
//                DataSet dataSet = new DataSet();
//                oleDbAdapter.Fill(dataSet);
//                return dataSet;
//            }
//        }

//        public DataTable GetDataTable(string statement)
//        {
//            using (OleDbDataAdapter oleDbAdapter = new OleDbDataAdapter(statement, dbConnection))
//            {
//                DataTable dataTable = new DataTable();
//                oleDbAdapter.Fill(dataTable);
//                return dataTable;
//            }
//        }

//        public static OleDbUtil GetInstance()
//        {
//            if (currentInstance == null || currentInstance.IsDisposed)
//            {
//                currentInstance = new OleDbUtil();
//            }

//            return currentInstance;
//        }

//        ~OleDbUtil()
//        {
//            Dispose(false);
//        }

//        public void Dispose()
//        {
//            Dispose(true);
//            GC.SuppressFinalize(this);
//        }

//        protected virtual void Dispose(bool disposed)
//        {
//            if (disposed)
//            {
//                if (currentInstance != null)
//                {
//                    dbConnection.Close();
//                    dbConnection.Dispose();
//                    IsDisposed = true;
//                }
//            }
//        }
//    }
//}