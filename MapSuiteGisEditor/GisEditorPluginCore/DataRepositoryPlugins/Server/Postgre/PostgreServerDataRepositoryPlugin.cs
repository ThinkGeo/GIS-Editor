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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class PostgreServerDataRepositoryPlugin : DataRepositoryPlugin
    {
        private DataRepositoryItem rootItem;

        public PostgreServerDataRepositoryPlugin()
            : base()
        {
            Name = "Postgre Server";
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/apacheconf.png", UriKind.RelativeOrAbsolute));
            Content = new DataRepositoryContentUserControl();
            Index = DataRepositoryOrder.Postgre;
            InitContextMenu();
        }

        protected override DataRepositoryItem RootDataRepositoryItemCore
        {
            get { return rootItem ?? (rootItem = new DataRepositoryItem()); }
        }

        protected override StorableSettings GetSettingsCore()
        {
            XElement xElement = new XElement("Servers", rootItem.Children.OfType<PostgreServerDataRepositoryItem>().Select(item =>
            {
                string server = String.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}", item.Server, item.Port, item.UserName, item.Password);
                return new XElement("Server", StringProtector.Instance.Encrypt(server));
            }));

            StorableSettings settings = base.GetSettingsCore();
            settings.GlobalSettings.Add("Servers", xElement.ToString());
            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            if (settings.GlobalSettings.ContainsKey("Servers"))
            {
                try
                {
                    if (rootItem != null)
                    {
                        rootItem.Children.Clear();
                        XElement serversElement = XElement.Parse(settings.GlobalSettings["Servers"]);
                        serversElement.Elements("Server").ForEach(item =>
                        {
                            string serverString = item.Value;
                            serverString = StringProtector.Instance.Decrypt(serverString);
                            string[] server = serverString.Split('|');

                            PostgreServerDataRepositoryItem serverItem = new PostgreServerDataRepositoryItem();
                            serverItem.Name = server[0];
                            serverItem.Server = server[0];
                            serverItem.Port = Int32.Parse(server[1]);
                            serverItem.UserName = server[2];
                            serverItem.Password = server[3];
                            rootItem.Children.Add(serverItem);

                            PostgreConfigureInfo configureInfo = new PostgreConfigureInfo();
                            configureInfo.Server = serverItem.Server;
                            configureInfo.Port = serverItem.Port;
                            configureInfo.UserName = serverItem.UserName;
                            configureInfo.Password = serverItem.Password;
                            SyncToServerItem(configureInfo, serverItem, true);
                        });
                    }
                }
                catch { }
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
            PostgreServerConfigureWindow configureWindow = new PostgreServerConfigureWindow();
            configureWindow.Owner = Application.Current.MainWindow;
            configureWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (configureWindow.ShowDialog().GetValueOrDefault())
            {
                PostgreServerDataRepositoryItem serverItem = new PostgreServerDataRepositoryItem();
                serverItem.Parent = rootItem;
                SyncToServerItem(configureWindow.Result, serverItem);
                rootItem.Children.Add(serverItem);
                GisEditor.InfrastructureManager.SaveSettings(this);
            }
        }

        internal static void SyncToServerItem(PostgreConfigureInfo configureInfo, PostgreServerDataRepositoryItem serverItem, bool resetDatabases = false)
        {
            serverItem.Name = configureInfo.Server;
            serverItem.Server = configureInfo.Server;
            serverItem.Port = configureInfo.Port;
            serverItem.UserName = configureInfo.UserName;
            serverItem.Password = configureInfo.Password;

            GetDatabasesParameter parameter = new GetDatabasesParameter();
            parameter.ServerItem = serverItem;
            parameter.ResetDatabases = resetDatabases;
            foreach (var item in configureInfo.DbaseNames)
            {
                parameter.DatabaseNames.Add(item);
            }

            Task<Collection<DatabaseModel>> task = new Task<Collection<DatabaseModel>>(GetDatabaseItems, parameter);
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

                                    AddSchemaChildrens(schemaItem, tableGroup, viewGroup, "/GisEditorPluginCore;component/Images/tablefolder.png", group.Key, databaseItem.Name);
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
            GetDatabasesParameter tempParam = (GetDatabasesParameter)obj;

            if (tempParam.ResetDatabases)
            {
                try
                {
                    Collection<string> databases = PostgreSqlFeatureSource.GetDatabaseNames(tempParam.ServerItem.Server, tempParam.ServerItem.Port, tempParam.ServerItem.UserName, tempParam.ServerItem.Password);
                    tempParam.DatabaseNames.Clear();
                    foreach (var newDbaseName in databases)
                    {
                        tempParam.DatabaseNames.Add(newDbaseName);
                    }
                }
                catch
                { }
            }

            Collection<DatabaseModel> result = new Collection<DatabaseModel>();

            foreach (var dbName in tempParam.DatabaseNames)
            {
                DatabaseModel dbItem = new DatabaseModel();
                dbItem.Name = dbName;
                try
                {
                    string connectionString = GetConnectionString(tempParam.ServerItem, dbName);
                    Collection<string> tableNames = PostgreSqlFeatureSource.GetTableNames(connectionString);
                    Collection<string> viewNames = PostgreSqlFeatureSource.GetViewNames(connectionString);
                    dbItem.TableModels.Clear();

                    foreach (var item in tableNames)
                    {
                        TableModel model = new TableModel();
                        model.Name = item;
                        string pattern = string.Empty;
                        int index = item.IndexOf(":", StringComparison.Ordinal);
                        if (index != -1)
                        {
                            pattern = item.Substring(0, index);
                        }
                        model.SchemaName = pattern;
                        dbItem.TableModels.Add(model);
                    }

                    foreach (var item in viewNames)
                    {
                        TableModel model = new TableModel();
                        model.Name = item;
                        model.IsView = true;
                        string pattern = string.Empty;
                        int index = item.IndexOf(":", StringComparison.Ordinal);
                        if (index != -1)
                        {
                            pattern = item.Substring(0, index);
                        }
                        model.SchemaName = pattern;
                        dbItem.TableModels.Add(model);
                    }
                }
                catch
                { }

                result.Add(dbItem);
            }

            return result;
        }

        private static void AddSchemaChildrens(DataRepositoryItem dbItem, IEnumerable<string> tableNames, IEnumerable<string> viewNames, string iconUri, string name, string databaseName)
        {
            if (!tableNames.Any() && !viewNames.Any()) return;

            PostgreSchemaDataRepositoryItem schemaItem = new PostgreSchemaDataRepositoryItem();
            schemaItem.Name = String.Format(CultureInfo.InvariantCulture, "{0}", name);
            schemaItem.SchemaName = schemaItem.Name;
            schemaItem.Icon = new BitmapImage(new Uri(iconUri, UriKind.RelativeOrAbsolute));
            dbItem.Children.Add(schemaItem);

            AddChildrens(schemaItem, tableNames.ToList(), "/GisEditorPluginCore;component/Images/tables.png", "Tables", tmpName =>
            {
                PostgreTableDataRepositoryItem tableItem = new PostgreTableDataRepositoryItem();
                tableItem.TableName = tmpName;
                tableItem.SchemaName = schemaItem.Name;
                tableItem.DatabaseName = databaseName;
                return tableItem;
            });

            AddChildrens(schemaItem, viewNames.ToList(), "/GisEditorPluginCore;component/Images/dataviews.png", "Views", tmpName =>
            {
                PostgreViewDataRepositoryItem viewItem = new PostgreViewDataRepositoryItem();
                viewItem.ViewName = tmpName;
                viewItem.SchemaName = schemaItem.Name;
                viewItem.DatabaseName = databaseName;
                return viewItem;
            });
        }

        private static void AddChildrens(DataRepositoryItem dbItem, List<string> itemNames, string iconUri, string name, Func<string, DataRepositoryItem> newItemFunc)
        {
            if (itemNames.Count == 0) return;

            DataRepositoryItem tablesItem = new DataRepositoryItem();
            tablesItem.Name = String.Format(CultureInfo.InvariantCulture, "{0} ({1})", name, itemNames.Count);
            tablesItem.Icon = new BitmapImage(new Uri(iconUri, UriKind.RelativeOrAbsolute));
            dbItem.Children.Add(tablesItem);

            foreach (var itemName in itemNames)
            {
                DataRepositoryItem tableItem = newItemFunc(itemName);
                tableItem.Name = itemName;

                int index = itemName.IndexOf(":", StringComparison.Ordinal);
                if (index != -1)
                {
                    tableItem.Name = itemName.Substring(index + 1);
                }

                tablesItem.Children.Add(tableItem);
            }
        }

        public static string GetConnectionString(string server, string dbName, int port, string userName, string password)
        {
            return String.Format(CultureInfo.InvariantCulture, "Server={0};Database={1};Port={2};User Id={3};Password={4};"
                    , server
                    , dbName
                    , port
                    , userName
                    , password);
        }

        public static string GetConnectionString(PostgreServerDataRepositoryItem serverItem, string dbName)
        {
            return GetConnectionString(serverItem.Server
                , dbName
                , serverItem.Port
                , serverItem.UserName
                , serverItem.Password);
        }
    }

    public class GetDatabasesParameter
    {
        private Collection<string> databaseNames;
        private PostgreServerDataRepositoryItem serverItem;

        public GetDatabasesParameter()
        {
            databaseNames = new Collection<string>();
        }

        public Collection<string> DatabaseNames
        {
            get { return databaseNames; }
        }

        public PostgreServerDataRepositoryItem ServerItem
        {
            get { return serverItem; }
            set { serverItem = value; }
        }

        public bool ResetDatabases { get; set; }
    }
}