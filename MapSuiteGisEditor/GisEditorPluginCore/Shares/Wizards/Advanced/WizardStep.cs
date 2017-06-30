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
using System.ComponentModel;
using System.Windows.Controls;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public abstract class WizardStep<T> : INotifyPropertyChanged where T : class
    {
        private bool isCurrent;
        [NonSerialized]
        private UserControl content;
        private string title;

        /// <summary>
        /// Step 1 for example.
        /// </summary>
        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                if (String.IsNullOrEmpty(Header)) Header = title;
            }
        }

        /// <summary>
        /// Display as the title of the user control.
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Discription for the tree list on the left dock.
        /// </summary>
        public string Description { get; set; }

        public UserControl Content
        {
            get { return content; }
            set { content = value; OnPropertyChanged("Content"); }
        }

        public object Tag { get; set; }

        public bool IsCurrent
        {
            get { return isCurrent; }
            set { isCurrent = value; OnPropertyChanged("IsCurrent"); }
        }

        public WizardViewModel<T> Parent { get; set; }

        public void Enter(T previousStatus)
        {
            EnterCore(previousStatus);
        }

        protected virtual void EnterCore(T parameter) { }

        public bool Leave(T parameter)
        {
            return LeaveCore(parameter);
        }

        protected virtual bool LeaveCore(T parameter) { return true; }

        public bool CanMoveToNext()
        {
            return CanMoveToNextCore();
        }

        protected virtual bool CanMoveToNextCore()
        {
            return true;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
