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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class TaskMonitorViewModel : ViewModelBase
    {
        private bool isPinned;
        [NonSerialized]
        private ImageSource pinIcon;
        [NonSerialized]
        private RelayCommand<TaskViewModel> cancelTaskCommand;
        private ObservableCollection<TaskViewModel> runningTasks;
        [NonSerialized]
        private RelayCommand<TaskbarNotifier> pinTaskNotifierCommand;
        [NonSerialized]
        private RelayCommand<TaskbarNotifier> openNotifyWindowCommand;
        [NonSerialized]
        private RelayCommand<TaskbarNotifier> hideTaskNotifierCommand;
        [NonSerialized]
        private RelayCommand<TaskbarNotifier> closeNotifierTrayCommand;

        public TaskMonitorViewModel()
        {
            isPinned = true;
            runningTasks = new ObservableCollection<TaskViewModel>();
            pinIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Window_Pinned.png", UriKind.RelativeOrAbsolute));
        }

        public ImageSource PinIcon
        {
            get { return pinIcon; }
            set
            {
                pinIcon = value;
                RaisePropertyChanged(()=>PinIcon);
            }
        }

        public RelayCommand<TaskViewModel> CancelTaskCommand
        {
            get
            {
                if (cancelTaskCommand == null)
                {
                    cancelTaskCommand = new RelayCommand<TaskViewModel>(viewModel =>
                    {
                        GisEditor.TaskManager.UpdateTask(viewModel.TaskToken, TaskCommand.Cancel);
                    });
                }
                return cancelTaskCommand;
            }
        }

        public ObservableCollection<TaskViewModel> RunningTasks
        {
            get { return runningTasks; }
        }

        public RelayCommand<TaskbarNotifier> HideTaskNotifierCommand
        {
            get
            {
                if (hideTaskNotifierCommand == null)
                {
                    hideTaskNotifierCommand = new RelayCommand<TaskbarNotifier>(win =>
                    {
                        win.ForceHidden();
                    });
                }
                return hideTaskNotifierCommand;
            }
        }

        public RelayCommand<TaskbarNotifier> OpenNotifyWindowCommand
        {
            get
            {
                if (openNotifyWindowCommand == null)
                {
                    openNotifyWindowCommand = new RelayCommand<TaskbarNotifier>(win =>
                    {
                        win.Notify();
                    });
                }
                return openNotifyWindowCommand;
            }
        }

        public RelayCommand<TaskbarNotifier> PinTaskNotifierCommand
        {
            get
            {
                if (pinTaskNotifierCommand == null)
                {
                    pinTaskNotifierCommand = new RelayCommand<TaskbarNotifier>(win =>
                    {
                        isPinned = !isPinned;
                        if (isPinned)
                        {
                            PinIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Window_Pinned.png", UriKind.RelativeOrAbsolute));
                        }
                        else
                        {
                            PinIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Window_Unpinned.png", UriKind.RelativeOrAbsolute));
                        }

                        win.Pin(isPinned);
                    });
                }
                return pinTaskNotifierCommand;
            }
        }

        public RelayCommand<TaskbarNotifier> CloseNotifierTrayCommand
        {
            get
            {
                if (closeNotifierTrayCommand == null)
                {
                    closeNotifierTrayCommand = new RelayCommand<TaskbarNotifier>(win =>
                    {
                        win.Close();
                    });
                }
                return closeNotifierTrayCommand;
            }
        }
    }
}