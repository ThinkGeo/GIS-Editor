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

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    public class CheckableItemViewModel<T> : INotifyPropertyChanged
    {
        private T result;
        private string name;
        private string aliasName;
        private bool isChecked;
        private Func<T, string> generateNameFunc;

        public CheckableItemViewModel()
            : this(default(T), false, null)
        { }

        public CheckableItemViewModel(T value)
            : this(value, false, null)
        { }

        public CheckableItemViewModel(T value, bool isChecked, Func<T, string> getNameFunc)
        {
            this.generateNameFunc = getNameFunc;
            this.isChecked = isChecked;
            this.result = value;
            this.GenerateName(value);
        }

        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                RaisePropertyChanged("IsChecked");
            }
        }

        public string Name
        {
            get { return name; }
            protected set
            {
                name = value;
                RaisePropertyChanged("Name");
            }
        }

        public string AliasName
        {
            get { return aliasName; }
            set { aliasName = value; }
        }

        public virtual T Value
        {
            get { return result; }
            set
            {
                result = value;
                GenerateName(value);
            }
        }

        private void GenerateName(T value)
        {
            if (generateNameFunc != null) Name = generateNameFunc(value);
            else if (value != null) Name = value.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}