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
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.Reflection;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    [Obfuscation]
    internal class ProjectionSelectionViewModel : ViewModelBase
    {
        private IProjectionViewModel selectedViewModel;
        private string textContent;
        private string checkBoxContent;
        private bool applyForAll;
        [NonSerialized]
        private RelayCommand cancelCommand;
        [NonSerialized]
        private ObservedCommand okCommand;

        public ProjectionSelectionViewModel(string textContent, string checkBoxContent)
        {
            this.textContent = textContent;
            this.checkBoxContent = checkBoxContent;
        }

        public bool IsReadOnly
        {
            get
            {
                if (SelectedViewModel != null)
                {
                    return SelectedViewModel.IsReadOnly;
                }
                else return true;
            }
        }

        public string TextContent
        {
            get { return textContent; }
            set
            {
                textContent = value;
                RaisePropertyChanged(()=>TextContent);
            }
        }

        public string CheckBoxContent
        {
            get { return checkBoxContent; }
            set
            {
                checkBoxContent = value;
                RaisePropertyChanged(()=>CheckBoxContent);
            }
        }

        public Visibility CheckBoxVisibility
        {
            get { return string.IsNullOrEmpty(CheckBoxContent) ? Visibility.Collapsed : Visibility.Visible; }
        }

        public Visibility TextVisibility
        {
            get { return string.IsNullOrEmpty(TextContent) ? Visibility.Collapsed : Visibility.Visible; }
        }

        public bool ApplyForAll
        {
            get { return applyForAll; }
            set
            {
                applyForAll = value;
                RaisePropertyChanged(()=>ApplyForAll);
            }
        }

        internal IProjectionViewModel SelectedViewModel
        {
            get { return selectedViewModel; }
            set
            {
                selectedViewModel = value;
                RaisePropertyChanged(()=>SelectedViewModel);
                RaisePropertyChanged(()=>SelectedProj4Parameter);
                RaisePropertyChanged(()=>IsReadOnly);
            }
        }

        public string SelectedProj4Parameter
        {
            get
            {
                return SelectedViewModel == null ? "" : SelectedViewModel.SelectedProj4ProjectionParameters;
            }
            set
            {
                if (SelectedViewModel != null)
                {
                    SelectedViewModel.SelectedProj4ProjectionParameters = value;
                    RaisePropertyChanged(()=>SelectedProj4Parameter);
                }
            }
        }

        public RelayCommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new RelayCommand(() =>
                    {
                        Messenger.Default.Send(false, this);
                    });
                }
                return cancelCommand;
            }
        }

        public ObservedCommand OkCommand
        {
            get
            {
                if (okCommand == null)
                {
                    okCommand = new ObservedCommand(() =>
                    {
                        Messenger.Default.Send(true, this);
                    }, () => !string.IsNullOrEmpty(SelectedProj4Parameter)
                    );
                }
                return okCommand;
            }
        }
    }
}