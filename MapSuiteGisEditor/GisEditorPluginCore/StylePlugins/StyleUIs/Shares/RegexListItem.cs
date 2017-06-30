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
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class RegexListItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NonSerialized]
        private Image image;
        private string matchValue;
        private RegexItem regexItem;
        private string styleType;
        private string id;

        public RegexListItem()
        {
            Id = Guid.NewGuid().ToString();
        }

        public Image Image { get { return image; } set { image = value; OnPropertyChanged("Image"); } }

        public string MatchValue { get { return matchValue; } set { matchValue = value; OnPropertyChanged("MatchValue"); } }

        public RegexItem RegexItem
        {
            get { return regexItem; }
            set { regexItem = value; OnPropertyChanged("RegexItem"); }
        }

        public string StyleType { get { return styleType; } set { styleType = value; OnPropertyChanged("StyleType"); } }

        public string Id { get { return id; } set { id = value; OnPropertyChanged("Id"); } }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}