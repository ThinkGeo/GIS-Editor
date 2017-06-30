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
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class TaskViewModel : ViewModelBase
    {
        private int progress;
        private string name;
        private string taskToken;
        private string description;

        public TaskViewModel()
            : this(string.Empty)
        { }

        public TaskViewModel(string taskToken)
        {
            this.taskToken = taskToken;
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

        public string Description
        {
            get { return description; }
            set
            {
                description = value;
                RaisePropertyChanged(()=>Description);
            }
        }

        public string TaskToken
        {
            get { return taskToken; }
            set
            {
                taskToken = value;
                RaisePropertyChanged(()=>TaskToken);
            }
        }

        public int Progress
        {
            get { return progress; }
            set
            {
                if (progress != value)
                {
                    progress = value;
                    RaisePropertyChanged(()=>Progress);
                    RaisePropertyChanged(()=>IsIndeterminate);
                }
            }
        }

        public bool IsIndeterminate
        {
            get { return Progress == 0; }
        }
    }
}