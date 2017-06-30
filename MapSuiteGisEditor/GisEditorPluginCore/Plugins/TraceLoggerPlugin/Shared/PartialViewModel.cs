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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using System;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class DebugOutputViewModel : ViewModelBase
    {
        private static readonly string logFormat = "==  Log on   {0}   ==";
        private static readonly string category = "Category:    {0}";
        private static readonly string loggerLevel = "LoggerLevel:  {0}";
        private static readonly string message = "Message:  {0}";
        private static readonly string error = "Error:  ";
        private static readonly string errorMessage = " Message:  {0}";
        private static readonly string source = "   Source:  {0}";
        private static readonly string stackTrace = "   StackTrace:  {0}";
        private static readonly string customData = "CustomData:    ";

        private Collection<LoggerMessageViewModel> GetLoadLoggerMessage()
        {
            Collection<LoggerMessageViewModel> results = null;
            GetLoggerMessageWindow window = new GetLoggerMessageWindow();
            window.Owner = Application.Current.MainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (window.ShowDialog().GetValueOrDefault())
            {
                results = new Collection<LoggerMessageViewModel>();
                foreach (LoggerMessage loggerMessage in window.ResultLoggerMessages)
                {
                    LoggerMessageViewModel loggerMessageViewModel = new LoggerMessageViewModel(loggerMessage);
                    loggerMessageViewModel.Time = DateTime.Now.Subtract(GetStartTime()).TotalSeconds;
                    results.Add(loggerMessageViewModel);
                }

            }
            return results;
        }

        internal void ExportToFile(string filePath)
        {
            Collection<string> contents = new Collection<string>();
            foreach (LoggerMessage loggerMessage in DisplayLoggerMessages.Select(l => l.LoggerMessage))
            {
                contents.Add(string.Format(CultureInfo.InvariantCulture, logFormat, loggerMessage.DateTime.ToShortTimeString()));
                contents.Add(string.Format(CultureInfo.InvariantCulture, category, loggerMessage.Category));
                contents.Add(string.Format(CultureInfo.InvariantCulture, loggerLevel, loggerMessage.LoggerLevel));
                contents.Add(string.Format(CultureInfo.InvariantCulture, message, loggerMessage.Message));
                if (!string.IsNullOrEmpty(loggerMessage.Error.Message)
                || !string.IsNullOrEmpty(loggerMessage.Error.Source)
                || !string.IsNullOrEmpty(loggerMessage.Error.StackTrace))
                {
                    contents.Add(string.Format(CultureInfo.InvariantCulture, error));
                    contents.Add(string.Format(CultureInfo.InvariantCulture, errorMessage, loggerMessage.Error.Message));
                    contents.Add(string.Format(CultureInfo.InvariantCulture, source, loggerMessage.Error.Source));
                    contents.Add(string.Format(CultureInfo.InvariantCulture, stackTrace, loggerMessage.Error.StackTrace));
                }
                contents.Add(string.Format(CultureInfo.InvariantCulture, customData, loggerMessage.CustomData));
            }

            File.WriteAllLines(filePath, contents);
        }
    }
}