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


using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class TexturePicker : UserControl, INotifyPropertyChanged
    {
        private TexturePickerViewModel viewModel;

        public event MouseButtonEventHandler SelectedItemDoubleClick;

        public TexturePicker()
        {
            InitializeComponent();
            viewModel = new TexturePickerViewModel();
            DataContext = viewModel;
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { viewModel.PropertyChanged += value; }
            remove { viewModel.PropertyChanged -= value; }
        }

        public Brush SelectedBrush
        {
            get { return viewModel.SelectedBrush; }
            set { viewModel.SelectedBrush = value; }
        }

        public ObservableCollection<ImageBrush> ImageBrushes { get { return viewModel.ImageBrushes; } }

        protected virtual void OnSelectedItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var handler = SelectedItemDoubleClick;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void ListBoxItem_MouseDoubleClickEventHandler(object sender, MouseButtonEventArgs e)
        {
            OnSelectedItemDoubleClick(sender, e);
        }
    }
}