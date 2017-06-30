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


using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class MsSqlServerDataRepositoryPlugin : DataRepositoryPlugin
    {
        private DataRepositoryItem rootItem;
        private Dictionary<string, string> recordCountCache;

        public MsSqlServerDataRepositoryPlugin()
            : base()
        {
            recordCountCache = new Dictionary<string, string>();
            Name = "MsSql Server";
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/mssql1.png", UriKind.RelativeOrAbsolute));
            Content = GetDataRepositoryContentUserControl();
            Index = DataRepositoryOrder.MsSql;
            InitContextMenu();
        }

        protected override DataRepositoryItem RootDataRepositoryItemCore
        {
            get
            {
                if (rootItem == null)
                {
                    rootItem = new DataRepositoryItem();
                    rootItem.Refreshing += RootItem_Refreshing;
                }
                return rootItem;
            }
        }

        private void RootItem_Refreshing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            recordCountCache.Clear();
        }

        protected override StorableSettings GetSettingsCore()
        {
            XElement xElement = new XElement("MsSqlServers", rootItem.Children.OfType<MsSqlServerDataRepositoryItem>().Select(item =>
            {
                string server = String.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}", item.Server, item.UserName, item.Password);
                return new XElement("Server", StringProtector.Instance.Encrypt(server));
            }));

            StorableSettings settings = base.GetSettingsCore();
            settings.GlobalSettings.Add("MsSqlServers", xElement.ToString());
            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            if (settings.GlobalSettings.ContainsKey("MsSqlServers"))
            {
                try
                {
                    if (rootItem != null)
                    {
                        rootItem.Children.Clear();
                        XElement serversElement = XElement.Parse(settings.GlobalSettings["MsSqlServers"]);
                        serversElement.Elements("Server").ForEach(item =>
                        {
                            string serverString = item.Value;
                            serverString = StringProtector.Instance.Decrypt(serverString);
                            string[] server = serverString.Split('|');

                            MsSqlServerDataRepositoryItem serverItem = new MsSqlServerDataRepositoryItem();
                            serverItem.Name = server[0];
                            serverItem.Server = server[0];
                            serverItem.UserName = server[1];
                            serverItem.Password = server[2];
                            rootItem.Children.Add(serverItem);

                            MsSql2008FeatureLayerInfo configureInfo = new MsSql2008FeatureLayerInfo();
                            configureInfo.ServerName = serverItem.Server;
                            configureInfo.UserName = serverItem.UserName;
                            configureInfo.Password = serverItem.Password;
                            if (!string.IsNullOrEmpty(configureInfo.UserName) && !string.IsNullOrEmpty(configureInfo.Password))
                            {
                                configureInfo.UseTrustAuthority = false;
                            }
                            SyncToServerItem(configureInfo, serverItem);
                        });
                    }
                }
                catch (Exception)
                { }
            }
        }

        private void InitContextMenu()
        {
            MenuItem addServerMenuItem = new MenuItem();
            addServerMenuItem.Header = "Add Server";
            addServerMenuItem.Command = new RelayCommand(AddServer);
            addServerMenuItem.Icon = new Image
            {
                Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/server_add.png", UriKind.RelativeOrAbsolute)),
                Width = 16,
                Height = 16
            };

            ContextMenu = new ContextMenu();
            ContextMenu.Items.Add(addServerMenuItem);
        }

        private void AddServer()
        {
            MsSqlServerConfigureWindow configureWindow = new MsSqlServerConfigureWindow();
            configureWindow.Owner = Application.Current.MainWindow;
            configureWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            MsSql2008FeatureLayerInfo model = new MsSql2008FeatureLayerInfo();
            configureWindow.SetSource(model);
            if (configureWindow.ShowDialog().GetValueOrDefault())
            {
                MsSqlServerDataRepositoryItem serverItem = GetMsSqlServerDataRepositoryItem(model);
                serverItem.Parent = rootItem;
                rootItem.Children.Add(serverItem);
                GisEditor.InfrastructureManager.SaveSettings(this);
            }
        }

        public static MsSqlServerDataRepositoryItem GetMsSqlServerDataRepositoryItem(MsSql2008FeatureLayerInfo configureInfo)
        {
            MsSqlServerDataRepositoryItem serverItem = new MsSqlServerDataRepositoryItem();
            SyncToServerItem(configureInfo, serverItem);
            return serverItem;
        }

        private static void SyncToServerItem(MsSql2008FeatureLayerInfo configureInfo, MsSqlServerDataRepositoryItem serverItem)
        {
            serverItem.Name = configureInfo.ServerName;
            serverItem.Server = configureInfo.ServerName;
            serverItem.UserName = configureInfo.UserName;
            serverItem.Password = configureInfo.Password;

            Task<Collection<DatabaseModel>> task = new Task<Collection<DatabaseModel>>(GetDatabaseItems, configureInfo);
            task.Start();
            task.ContinueWith(t =>
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (t.Result.Count > 0)
                    {
                        serverItem.Children.Clear();
                        foreach (var databaseItem in t.Result)
                        {
                            if (databaseItem.TableModels.Count > 0)
                            {
                                DatabaseDataRepositoryItem dbItem = new DatabaseDataRepositoryItem();
                                dbItem.Name = databaseItem.Name;

                                var groupItems = databaseItem.TableModels.GroupBy(g => g.SchemaName);

                                DataRepositoryItem schemaItem = new DataRepositoryItem();
                                schemaItem.Name = String.Format(CultureInfo.InvariantCulture, "{0} ({1})", "Schemas", groupItems.Count());
                                schemaItem.Icon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/schemas.png", UriKind.RelativeOrAbsolute));
                                dbItem.Children.Add(schemaItem);

                                foreach (var group in groupItems)
                                {
                                    IEnumerable<string> tableGroup = group.Where(v => !v.IsView).Select(g => g.Name);
                                    IEnumerable<string> viewGroup = group.Where(v => v.IsView).Select(g => g.Name);

                                    AddSchemaChildrens(schemaItem, tableGroup, viewGroup, "/GisEditorPluginCore;component/Images/tablefolder.png", group.Key, configureInfo, databaseItem.Name);
                                }

                                serverItem.Children.Add(dbItem);
                                DataRepositoryContentViewModel.RestoreChildrenExpandStatus(new ObservableCollection<DataRepositoryItem> { serverItem }, GisEditor.DataRepositoryManager.ExpandedFolders);
                            }
                        }
                    }
                });
            });
        }

        private static Collection<DatabaseModel> GetDatabaseItems(object obj)
        {
            MsSql2008FeatureLayerInfo tempParam = (MsSql2008FeatureLayerInfo)obj;

            Collection<DatabaseModel> result = new Collection<DatabaseModel>();

            foreach (var dbName in tempParam.CollectDatabaseFromServer())
            {
                DatabaseModel dbItem = new DatabaseModel(dbName);
                try
                {
                    Collection<string> tableNames = tempParam.CollectTablesFromDatabase(dbName);
                    Collection<string> viewNames = tempParam.CollectViewsFromDatabase(dbName);

                    foreach (var item in tableNames)
                    {
                        TableModel model = new TableModel(item);
                        string schema = string.Empty;
                        int index = item.IndexOf(".", StringComparison.Ordinal);
                        if (index != -1)
                        {
                            schema = item.Substring(0, index);
                        }
                        model.SchemaName = schema;
                        dbItem.TableModels.Add(model);
                    }

                    foreach (var item in viewNames)
                    {
                        TableModel model = new TableModel(item);
                        model.IsView = true;
                        string schema = string.Empty;
                        int index = item.IndexOf(".", StringComparison.Ordinal);
                        if (index != -1)
                        {
                            schema = item.Substring(0, index);
                        }
                        model.SchemaName = schema;
                        dbItem.TableModels.Add(model);
                    }
                }
                catch
                { }

                result.Add(dbItem);
            }

            return result;
        }

        private static void AddSchemaChildrens(DataRepositoryItem dbItem, IEnumerable<string> tableNames, IEnumerable<string> viewNames, string iconUri, string name, MsSql2008FeatureLayerInfo layerInfo, string databaseName)
        {
            if (!tableNames.Any() && !viewNames.Any()) return;

            PostgreSchemaDataRepositoryItem schemaItem = new PostgreSchemaDataRepositoryItem();
            schemaItem.Name = String.Format(CultureInfo.InvariantCulture, "{0}", name);
            schemaItem.SchemaName = schemaItem.Name;
            schemaItem.Icon = new BitmapImage(new Uri(iconUri, UriKind.RelativeOrAbsolute));
            dbItem.Children.Add(schemaItem);

            AddChildrens(schemaItem, tableNames.ToList(), "/GisEditorPluginCore;component/Images/tables.png", "Tables", layerInfo, databaseName);

            AddChildrens(schemaItem, viewNames.ToList(), "/GisEditorPluginCore;component/Images/dataviews.png", "Views", layerInfo, databaseName);
        }

        private static void AddChildrens(DataRepositoryItem dbItem, List<string> itemNames, string iconUri, string name, MsSql2008FeatureLayerInfo layerInfo, string databaseName)
        {
            if (itemNames.Count == 0) return;

            DataRepositoryItem tablesItem = new DataRepositoryItem();
            tablesItem.Name = String.Format(CultureInfo.InvariantCulture, "{0} ({1})", name, itemNames.Count);
            tablesItem.Icon = new BitmapImage(new Uri(iconUri, UriKind.RelativeOrAbsolute));
            dbItem.Children.Add(tablesItem);

            foreach (var itemName in itemNames)
            {
                MsSqlTableDataRepositoryItem tableItem = new MsSqlTableDataRepositoryItem();
                tableItem.LayerInfo = layerInfo;
                tableItem.TableName = itemName;
                tableItem.SchemaName = dbItem.Name;
                tableItem.DatabaseName = databaseName;
                tableItem.Name = itemName;

                int index = itemName.IndexOf(".", StringComparison.Ordinal);
                if (index != -1)
                {
                    tableItem.Name = itemName.Substring(index + 1);
                    tableItem.TableName = tableItem.Name;
                }

                tablesItem.Children.Add(tableItem);
            }
        }

        private UserControl GetDataRepositoryContentUserControl()
        {
            DataRepositoryContentUserControl userControl = new DataRepositoryContentUserControl();

            string header1 = "Record Count";
            DataRepositoryGridColumn column1 = new DataRepositoryGridColumn(header1, 100, di =>
            {
                MsSqlTableDataRepositoryItem msSqlTableDataRepositoryItem = di as MsSqlTableDataRepositoryItem;
                if (msSqlTableDataRepositoryItem == null) return string.Empty;

                TextBlock textBlock = new TextBlock();
                if (recordCountCache.ContainsKey(msSqlTableDataRepositoryItem.Id))
                {
                    textBlock.Text = recordCountCache[msSqlTableDataRepositoryItem.Id];
                }
                else
                {
                    textBlock.Text = "Loading...";

                    Task task = Task.Factory.StartNew(() =>
                    {
                        string tempTableName = GetFullTableName(msSqlTableDataRepositoryItem);
                        string sqlStatement = "SELECT COUNT(*) FROM " + tempTableName;

                        SqlCommand command = null;
                        SqlDataReader dataReader = null;

                        try
                        {
                            command = GetSqlCommand(sqlStatement, msSqlTableDataRepositoryItem);

                            dataReader = command.ExecuteReader();

                            string text = "";
                            if (dataReader.Read())
                            {
                                text = Convert.ToInt32(dataReader[0], CultureInfo.InvariantCulture).ToString();
                                recordCountCache[msSqlTableDataRepositoryItem.Id] = text;
                            }
                            Application.Current.Dispatcher.BeginInvoke(() =>
                            {
                                textBlock.Text = text;
                            });
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.BeginInvoke(() =>
                            {
                                textBlock.Text = "Timeout";
                            });
                        }
                        finally
                        {
                            if (dataReader != null)
                            {
                                dataReader.Dispose();
                            }
                            if (command != null)
                            {
                                command.Connection.Close();
                                command.Dispose();
                            }
                        }
                    });
                }

                return textBlock;
            });

            userControl.Columns.Add(column1);
            return userControl;
        }

        private string GetFullTableName(MsSqlTableDataRepositoryItem msSqlTableDataRepositoryItem)
        {
            string tempTableName = msSqlTableDataRepositoryItem.TableName;
            if (!string.IsNullOrEmpty(msSqlTableDataRepositoryItem.SchemaName))
            {
                tempTableName = string.Format(CultureInfo.InvariantCulture, "[{0}].[{1}]", msSqlTableDataRepositoryItem.SchemaName, msSqlTableDataRepositoryItem.TableName);
            }

            return tempTableName;
        }

        private SqlCommand GetSqlCommand(string sqlStatement, MsSqlTableDataRepositoryItem msSqlTableDataRepositoryItem)
        {
            SqlConnection connection = new SqlConnection(GetToDatabaseConnectionString(msSqlTableDataRepositoryItem));
            connection.Open();
            SqlCommand command = new SqlCommand(sqlStatement, connection);
            command.CommandTimeout = 60;

            return command;
        }

        private string GetToDatabaseConnectionString(MsSqlTableDataRepositoryItem msSqlTableDataRepositoryItem)
        {
            StringBuilder connectionBuilder = new StringBuilder();
            connectionBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}Initial Catalog={1};", GetToServerConnectionString(msSqlTableDataRepositoryItem), msSqlTableDataRepositoryItem.DatabaseName);
            return connectionBuilder.ToString();
        }

        private string GetToServerConnectionString(MsSqlTableDataRepositoryItem msSqlTableDataRepositoryItem)
        {
            DataRepositoryItem item = msSqlTableDataRepositoryItem.Parent;
            while (!(item is MsSqlServerDataRepositoryItem))
            {
                item = item.Parent;
            }

            MsSqlServerDataRepositoryItem serverItem = (MsSqlServerDataRepositoryItem)item;
            StringBuilder connectionBuilder = new StringBuilder();
            connectionBuilder.AppendFormat(CultureInfo.InvariantCulture, "Data Source={0};Persist Security Info=True;", serverItem.Server);
            if (string.IsNullOrEmpty(serverItem.UserName) && string.IsNullOrEmpty(serverItem.Password))
            {
                connectionBuilder.Append("Trusted_Connection=Yes;");
            }
            else
            {
                connectionBuilder.AppendFormat(CultureInfo.InvariantCulture, "User ID={0};Password={1};", serverItem.UserName, serverItem.Password);
            }

            return connectionBuilder.ToString();
        }
    }
}