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


using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class Arrow : Control, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private GradientStop gradientStop;

        public Arrow() : this(Colors.Transparent, 0)
        { }

        public Arrow(Color color, double offset)
        {
            DefaultStyleKey = typeof(Arrow);
            gradientStop = new GradientStop(color, offset);
        }

        public Color Color
        {
            get { return gradientStop.Color; }
            set { gradientStop.Color = value; OnPropertyChanged("Color"); }
        }

        public double Offset
        {
            get { return gradientStop.Offset; }
            set { gradientStop.Offset = value; OnPropertyChanged("Offset"); }
        }

        public GradientStop GradientStop
        {
            get { return gradientStop; }
            set { gradientStop = value; }
        }

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
