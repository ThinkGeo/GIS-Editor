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
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.ServiceModel;
//using System.Windows;
//using System.Windows.Controls;
//using ThinkGeo.MapSuite.GisEditor.Resources;

//namespace ThinkGeo.MapSuite.GisEditor
//{
//    /// <summary>
//    /// Interaction logic for OnlinePluginList.xaml
//    /// </summary>
//    public partial class OnlinePluginListUserControl : UserControl
//    {
//        private static readonly string onlineStoreUri = "http://ap.thinkgeo.com:8889/OnlinePluginService.svc";
//        private string directoryPath;

//        public OnlinePluginListUserControl()
//        {
//            directoryPath = GetAssemblyLocation() + "\\Plugins\\Upgrade\\";
//            InitializeComponent();
//        }

//        private void CommandBinding_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
//        {
//            OnlinePluginInformation currentOnlinePluginInfo = (OnlinePluginInformation)e.Parameter;
//            UIPlugin currentLocalPlugin = Globals.UIPluginManager.GetEnabledPlugins().FirstOrDefault(p 
//                => p.Name.Equals(currentOnlinePluginInfo.Name, StringComparison.OrdinalIgnoreCase));
//            if (currentLocalPlugin == null)
//            {
//                e.CanExecute = true;
//            }
//            else
//            {
//                Version localVersion = new Version(FileVersionInfo.GetVersionInfo(currentLocalPlugin.GetType().Assembly.Location).FileVersion);
//                Version onlineVersion = new Version(currentOnlinePluginInfo.Version);
//                string tmpOnlinePluginFullName = Path.Combine(directoryPath, currentLocalPlugin.Name + ".dll.tmp");
//                if (localVersion < onlineVersion && !File.Exists(tmpOnlinePluginFullName)) e.CanExecute = true;
//                else e.CanExecute = false;
//            }
//        }

//        private void CommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
//        {
//            OnlinePluginInformation currentOnlinePluginInfo = (OnlinePluginInformation)e.Parameter;
//            string pluginName = currentOnlinePluginInfo.Name;
//            pluginName += "Package.zip";

//            BasicHttpBinding httpBinding = new BasicHttpBinding();
//            httpBinding.MaxBufferSize = Int32.MaxValue;
//            httpBinding.MaxReceivedMessageSize = Int32.MaxValue;
//            httpBinding.MaxBufferPoolSize = Int32.MaxValue;
//            httpBinding.ReaderQuotas.MaxArrayLength = Int32.MaxValue;
//            using (OnlinePluginServiceClient client = new OnlinePluginServiceClient(httpBinding, new EndpointAddress(onlineStoreUri)))
//            {
//                StackPanel itemPanel = (StackPanel)((TextBlock)((System.Windows.Documents.Hyperlink)e.OriginalSource).Parent).Parent;
//                ProgressBar progressBar = ((ProgressBar)itemPanel.FindName("DownloadProgressBar"));
//                progressBar.Visibility = Visibility.Visible;
//                client.BeginGetOnlinePlugin(pluginName, new AsyncCallback(GetOnlinePluginBinaries), new { Client = client, StatusBar = progressBar });
//                client.BeginGetOnlinePlugin(pluginName, new AsyncCallback(GetOnlinePluginBinaries), new { Client = client, StatusBar = progressBar });
//            }
//        }

//        private void GetOnlinePluginBinaries(IAsyncResult result)
//        {
//            dynamic state = result.AsyncState;
//            var onlinePluginBinaries = (state.Client).EndGetOnlinePlugin(result);
//            string directoryPath = GetAssemblyLocation() + "\\Plugins\\Upgrade\\";
//            if (!Directory.Exists(directoryPath))
//            {
//                Directory.CreateDirectory(directoryPath);
//            }

//            if (onlinePluginBinaries.Count > 0)
//            {
//                foreach (var item in onlinePluginBinaries)
//                {
//                    string tmpPluginName = directoryPath + item.Key + ".tmp";
//                    using (FileStream tmpStream = File.Create(tmpPluginName))
//                    {
//                        tmpStream.Write(item.Value, 0, item.Value.Length);
//                    }
//                }
//            }

//            Dispatcher.BeginInvoke(new Action<ProgressBar>((statusBar) =>
//            {
//                statusBar.Visibility = Visibility.Collapsed;
//                Globals.UIPluginManager.CollectPluginInfos();
//            }), (ProgressBar)state.StatusBar);
//        }

//        private static string GetAssemblyLocation()
//        {
//            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
//        }
//    }
//}
