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
using System.Linq;
using System.Reflection;
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    [Obfuscation]
    public class LoggerWatchViewModel : ViewModelBase
    {
        private LoggerLevel loggerLevel;
        private ObservableCollection<LoggerMessage> loggerMessages;
        private ObservableCollection<LoggerMessage> resultLoggerMessages;
        private ObservedCommand clearCommand;

        public LoggerWatchViewModel()
        {
            loggerMessages = new ObservableCollection<LoggerMessage>();
            resultLoggerMessages = new ObservableCollection<LoggerMessage>();
            GisEditor.LoggerManager.Logged += new System.EventHandler<LoggedLoggerManagerEventArgs>(LoggerManager_Logged);
        }

        public LoggerLevel LoggerLevel
        {
            get { return loggerLevel; }
            set
            {
                loggerLevel = value;
                RaisePropertyChanged(()=>LoggerLevel);
                RaisePropertyChanged(()=>ResultLoggerMessages);
            }
        }

        public ObservableCollection<LoggerMessage> ResultLoggerMessages
        {
            get
            {
                resultLoggerMessages.Clear();
                foreach (var message in LoggerMessages.Where(m => m != null && m.LoggerLevel.Equals(LoggerLevel)))
                {
                    resultLoggerMessages.Add(message);
                }

                return resultLoggerMessages;
            }
        }

        public ObservableCollection<LoggerMessage> LoggerMessages
        {
            get { return loggerMessages; }
        }

        public ObservedCommand ClearCommand
        {
            get
            {
                if (clearCommand == null)
                {
                    clearCommand = new ObservedCommand(() =>
                    {
                        loggerMessages.Clear();
                        RaisePropertyChanged(()=>ResultLoggerMessages);
                    }, () => true);
                }
                return clearCommand;
            }
        }

        [Obfuscation]
        private void LoggerManager_Logged(object sender, LoggedLoggerManagerEventArgs e)
        {
            while (loggerMessages.Count > 100)
            {
                loggerMessages.RemoveAt(0);
            }

            loggerMessages.Add(e.LoggerMessage);

            RaisePropertyChanged(()=>ResultLoggerMessages);
        }
    }
}