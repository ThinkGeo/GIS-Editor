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
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BookmarkNamePromptViewModel : ViewModelBase
    {
        private string name;
        private bool isGlobal;
        private List<string> existingNames;
        private ObservedCommand confirmCommand;

        public BookmarkNamePromptViewModel(string name, IEnumerable<string> existingNames)
            : base()
        {
            this.Name = name;
            this.existingNames = new List<string>(existingNames);
            this.IsGlobal = true;
        }

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                RaisePropertyChanged(()=>Name);
            }
        }

        public bool IsGlobal
        {
            get { return isGlobal; }
            set
            {
                isGlobal = value;
                RaisePropertyChanged(()=>IsGlobal);
            }
        }

        public ObservedCommand ConfirmCommand
        {
            get
            {
                if (confirmCommand == null)
                {
                    confirmCommand = new ObservedCommand(() =>
                    {
                        try
                        {
                            if (ValidateBookmarkName(Name, existingNames))
                                MessengerInstance.Send(true, this);
                            else
                                SendMessage("Bookmark name exists. Please use another name.", "Warning", MessageBoxImage.Warning);
                        }
                        catch (ArgumentNullException ex)
                        {
                            GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                            SendMessage(ex.Message, "Warning", MessageBoxImage.Warning);
                        }
                        catch (ArgumentOutOfRangeException ex)
                        {
                            GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                            SendMessage(ex.Message, "Info", MessageBoxImage.Information);
                        }
                    }, () => ValidateBookmarkName(Name, existingNames));
                }
                return confirmCommand;
            }
        }

        private void SendMessage(string content, string caption, MessageBoxImage messageImage)
        {
            DialogMessage message = new DialogMessage(this, content, result => { });
            message.Button = MessageBoxButton.OK;
            message.Caption = caption;
            message.Icon = MessageBoxImage.Warning;
            MessengerInstance.Send(message, this);
        }

        internal static bool ValidateBookmarkName(string bookmarkName, IEnumerable<string> existingNames)
        {
            if (string.IsNullOrEmpty(bookmarkName) || string.IsNullOrEmpty(bookmarkName.Trim()))
                return false;
            else
                return existingNames.All(tmpName => !tmpName.Equals(bookmarkName, StringComparison.Ordinal));
        }
    }
}