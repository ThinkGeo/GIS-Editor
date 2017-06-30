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
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DrawingToolItemModel : INotifyPropertyChanged
    {
        public event EventHandler Selected;
        public event PropertyChangedEventHandler PropertyChanged;

        private bool isEnabled;
        [NonSerialized]
        private BitmapImage imageSource;
        private string text;
        private bool isSelected;

        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                isEnabled = value;
                OnPropertyChanged("IsEnabled");
            }
        }

        public BitmapImage ImageSource
        {
            get { return imageSource; }
            set
            {
                imageSource = value;
                OnPropertyChanged("ImageSource");
            }
        }

        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                OnPropertyChanged("Text");
            }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                bool RaiseSelectedEvent = !isSelected && value;
                isSelected = value;
                OnPropertyChanged("IsSelected");
                if (RaiseSelectedEvent)
                {
                    OnSelected();
                }
            }
        }

        private void OnSelected()
        {
            EventHandler handler = Selected;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}