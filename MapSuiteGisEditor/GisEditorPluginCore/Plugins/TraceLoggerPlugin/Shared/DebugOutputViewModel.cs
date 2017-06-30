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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class DebugOutputViewModel : ViewModelBase
    {
        private static readonly int maximumLoggerCount = 3000;
        private Collection<LoggerPlugin> loggerPlugins;
        private ObservableCollection<LoggerMessageViewModel> loggerMessages;
        private ObservableCollection<LoggerMessageViewModel> displayLoggerMessages;
        private LoggerMessageViewModel selectedLoggerMessage;
        private bool isCapture;
        private DateTime startTime;

        private RelayCommand loadCommand;
        private RelayCommand filterCommand;
        private RelayCommand clearCommand;

        public DebugOutputViewModel()
        {
            isCapture = false;
            loggerPlugins = new Collection<LoggerPlugin>();
            loggerMessages = new ObservableCollection<LoggerMessageViewModel>();
            displayLoggerMessages = new ObservableCollection<LoggerMessageViewModel>();
            startTime = DateTime.MinValue;
        }

        public Action SearchFinished { get; set; }

        public Collection<LoggerPlugin> LoggerPlugins
        {
            get { return loggerPlugins; }
        }

        public ObservableCollection<LoggerMessageViewModel> LoggerMessages
        {
            get { return loggerMessages; }
        }

        public ObservableCollection<LoggerMessageViewModel> DisplayLoggerMessages
        {
            get { return displayLoggerMessages; }
        }

        public LoggerMessageViewModel SelectedLoggerMessage
        {
            get { return selectedLoggerMessage; }
            set
            {
                selectedLoggerMessage = value;
                RaisePropertyChanged(()=>SelectedLoggerMessage);
            }
        }

        public bool IsCapture
        {
            get { return isCapture; }
            set
            {
                isCapture = value;
                RaisePropertyChanged(()=>IsCapture);
                if (isCapture)
                {
                    GisEditor.LoggerManager.Logging -= LoggerManager_Logging;
                    GisEditor.LoggerManager.Logging += LoggerManager_Logging;
                }
                else
                {
                    GisEditor.LoggerManager.Logging -= LoggerManager_Logging;
                }
            }
        }

        public RelayCommand LoadCommand
        {
            get
            {
                if (loadCommand == null)
                {
                    loadCommand = new RelayCommand(() =>
                    {
                        Collection<LoggerMessageViewModel> result = GetLoadLoggerMessage();
                        if (result != null)
                        {
                            loggerMessages.Clear();
                            foreach (var item in result)
                            {
                                loggerMessages.Add(item);
                            }
                            Filter(new LoggerMessageFilterViewModel());
                            LimiteLoggerMeesage();
                        }
                    });
                }
                return loadCommand;
            }
        }

        public RelayCommand FilterCommand
        {
            get
            {
                if (filterCommand == null)
                {
                    filterCommand = new RelayCommand(() =>
                    {
                        LoggerMessageFilterWindow filterWindow = new LoggerMessageFilterWindow();
                        if (filterWindow.ViewModel.Categories.Count == 0)
                        {
                            LoggerMessageFilterViewModel.Load(loggerMessages);
                        }
                        if (filterWindow.ShowDialog().GetValueOrDefault())
                        {
                            Filter(filterWindow.ViewModel);
                        }
                    });
                }
                return filterCommand;
            }
        }

        public RelayCommand ClearCommand
        {
            get
            {
                if (clearCommand == null)
                {
                    clearCommand = new RelayCommand(() =>
                    {
                        startTime = DateTime.MinValue;
                        LoggerMessageViewModel.BaseIndex = 0;
                        loggerMessages.Clear();
                        Filter(new LoggerMessageFilterViewModel());
                    });
                }
                return clearCommand;
            }
        }

        private void Filter(LoggerMessageFilterViewModel viewModel)
        {
            displayLoggerMessages.Clear();
            List<LoggerMessageViewModel> results = new List<LoggerMessageViewModel>();
            if (!viewModel.Level.Equals("All"))
            {
                var level = (LoggerLevel)Enum.Parse(typeof(LoggerLevel), viewModel.Level, true);
                results = loggerMessages.Where(m =>
                    m.LoggerMessage.LoggerLevel == level
                    && (string.IsNullOrEmpty(viewModel.Include) ? true : m.ToString().IndexOf(viewModel.Include, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    && (string.IsNullOrEmpty(viewModel.Exclude) ? true : m.ToString().IndexOf(viewModel.Exclude, StringComparison.InvariantCultureIgnoreCase) < 0)
                    && (viewModel.Category.Equals("All") ? true : m.LoggerMessage.Category.Equals(viewModel.Category, StringComparison.InvariantCultureIgnoreCase))
             ).ToList();
            }
            else
            {
                results = loggerMessages.Where(m =>
                    (string.IsNullOrEmpty(viewModel.Include) ? true : m.ToString().IndexOf(viewModel.Include, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    && (string.IsNullOrEmpty(viewModel.Exclude) ? true : m.ToString().IndexOf(viewModel.Exclude, StringComparison.InvariantCultureIgnoreCase) < 0)
                    && (viewModel.Category.Equals("All") ? true : m.LoggerMessage.Category.Equals(viewModel.Category, StringComparison.InvariantCultureIgnoreCase))
             ).ToList();
            }
            foreach (var item in results)
            {
                displayLoggerMessages.Add(item);
            }

            if (SearchFinished != null) SearchFinished();
        }

        private DateTime GetStartTime()
        {
            if (startTime == DateTime.MinValue) return DateTime.Now;
            else return startTime;
        }

        private void LoggerManager_Logging(object sender, LoggingLoggerManagerEventArgs e)
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (startTime == DateTime.MinValue)
                    {
                        startTime = DateTime.Now;
                    }

                    if (IsCapture)
                    {
                        LoggerMessageViewModel loggerMessageViewModel = new LoggerMessageViewModel(e.LoggerMessage);
                        loggerMessageViewModel.Time = DateTime.Now.Subtract(GetStartTime()).TotalSeconds;
                        loggerMessages.Add(loggerMessageViewModel);
                        displayLoggerMessages.Add(loggerMessageViewModel);

                        LimiteLoggerMeesage();
                        Filter(new LoggerMessageFilterViewModel());
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void LimiteLoggerMeesage()
        {
            bool needClearSource = false;
            while (displayLoggerMessages.Count > maximumLoggerCount)
            {
                displayLoggerMessages.RemoveAt(0);
                needClearSource = true;
            }
            if (needClearSource)
            {
                DateTime minDateTime = displayLoggerMessages.Min(m => m.LoggerMessage.DateTime);
                List<LoggerMessageViewModel> messages = loggerMessages.Where(m => DateTime.Compare(m.LoggerMessage.DateTime, minDateTime) < 0).ToList();
                foreach (var item in messages)
                {
                    loggerMessages.Remove(item);
                }
            }
        }
    }
}