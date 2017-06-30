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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Collection<string> existedExceptions = new Collection<string>();

        static App()
        {
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppHelper.Startup();
            try
            {
                AppHelper.CopyFilesToDocumentFolder();
            }
            catch { }

            var startupProjectPath = string.Empty;
            AppHelper.ParseArguments(e.Args, ref startupProjectPath, string.Empty);

            base.OnStartup(e);

            DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(app_DispatcherUnhandledException);
            AppHelper.Started(startupProjectPath);
        }

        [Obfuscation]
        private void app_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string message = e.Exception.Message;

            LoggerLevel loggerLevel = LoggerLevel.Error;
            //if (e.Exception is TypeLoadException)
            //{
            //    loggerLevel = LoggerLevel.Debug;

            //    if (!existedExceptions.Contains(e.Exception.Message))
            //    {
            //        existedExceptions.Add(e.Exception.Message);

            //        message = string.Format(CultureInfo.InvariantCulture, "A TypeLoadException is caught. This exception might cause by a breaking change of MapSuite APIs. It will break functionality of {0}. Please contact ThinkGeo to update corresponding assemblies.", e.Exception.Source);

            //        //GisEditorMessageBox messageBox = new GisEditorMessageBox(MessageBoxButton.OK, true);
            //        //messageBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //        //messageBox.Owner = Application.Current.MainWindow;
            //        //messageBox.Title = "Info";
            //        //messageBox.Message = string.Format(CultureInfo.InvariantCulture, "A TypeLoadException is caught. This exception might cause by a breaking change of MapSuite APIs. It will break functionality of {0}. Please contact ThinkGeo to update corresponding assemblies.", e.Exception.Source);
            //        //StringBuilder sb = new StringBuilder();
            //        //sb.Append(String.Format(CultureInfo.InvariantCulture, "Source: \t{0}\r\n", e.Exception.Source));
            //        //sb.Append(String.Format(CultureInfo.InvariantCulture, "Message: \t{0}\r\n", e.Exception.Message));
            //        //sb.Append(String.Format(CultureInfo.InvariantCulture, "Stack: \t{0}\r\n", e.Exception.StackTrace));

            //        //messageBox.ErrorMessage = sb.ToString();
            //        //if (messageBox.ShowDialog().GetValueOrDefault() && messageBox.NeedSendError)
            //        //{
            //        //    GisEditor.LoggerManager.Log(LoggerLevel.Error, message, e.Exception);
            //        //}
            //    }
            //}

            GisEditor.LoggerManager.Log(loggerLevel, message, e.Exception);
            e.Handled = true;
        }
    }
}