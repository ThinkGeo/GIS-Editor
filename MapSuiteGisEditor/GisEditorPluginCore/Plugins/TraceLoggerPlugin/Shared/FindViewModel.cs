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
    public class FindViewModel : ViewModelBase
    {
        private Action<LoggerMessageViewModel> findAction;
        private LoggerMessageViewModel lastLoggerMessage;
        private List<LoggerMessageViewModel> displayMessages;

        private string key;
        private bool matchCase;
        private Collection<FindLoggerDirection> directions;
        private FindLoggerDirection selectedDirection;
        private RelayCommand findCommand;

        public FindViewModel(IEnumerable<LoggerMessageViewModel> messages, Action<LoggerMessageViewModel> action)
        {
            this.findAction = action;
            displayMessages = new List<LoggerMessageViewModel>(messages);
            directions = new Collection<FindLoggerDirection>();
            foreach (string item in Enum.GetNames(typeof(FindLoggerDirection)))
            {
                directions.Add((FindLoggerDirection)Enum.Parse(typeof(FindLoggerDirection), item));
            }
        }

        public string Key
        {
            get { return key; }
            set
            {
                key = value;
                RaisePropertyChanged(()=>Key);
                findCommand.RaiseCanExecuteChanged();
            }
        }

        public bool MatchCase
        {
            get { return matchCase; }
            set
            {
                matchCase = value;
                RaisePropertyChanged(()=>MatchCase);
            }
        }

        public Collection<FindLoggerDirection> Directions
        {
            get { return directions; }
        }

        public FindLoggerDirection SelectedDirection
        {
            get { return selectedDirection; }
            set
            {
                selectedDirection = value;
                RaisePropertyChanged(()=>SelectedDirection);
            }
        }

        public RelayCommand FindCommand
        {
            get
            {
                if (findCommand == null)
                {
                    findCommand = new RelayCommand(() =>
                    {
                        string keyUpper = key.ToUpperInvariant();
                        List<LoggerMessageViewModel> result = new List<LoggerMessageViewModel>();
                        if (MatchCase)
                            result = displayMessages.Where(m => m.ToString().IndexOf(key) >= 0).ToList();
                        else
                            result = displayMessages.Where(m => m.ToString().ToUpperInvariant().Contains(keyUpper)).ToList();

                        if (result.Count > 0)
                        {
                            int index = result.IndexOf(lastLoggerMessage);
                            if (index >= 0)
                            {
                                if (selectedDirection == FindLoggerDirection.Down)
                                {
                                    lastLoggerMessage = index + 1 >= result.Count ? result[0] : result[index + 1];
                                }
                                else
                                {
                                    lastLoggerMessage = index == 0 ? result[result.Count - 1] : result[index - 1];
                                }
                            }
                            else
                            {
                                lastLoggerMessage = selectedDirection == FindLoggerDirection.Down ? result[0] : result[result.Count - 1];
                            }
                            findAction(lastLoggerMessage);
                        }
                        else
                        {
                            lastLoggerMessage = null;
                            MessageBox.Show("Cannot find \"" + key + "\"", "Logger Messages", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }, () => !string.IsNullOrEmpty(key));
                }
                return findCommand;
            }
        }
    }
}
