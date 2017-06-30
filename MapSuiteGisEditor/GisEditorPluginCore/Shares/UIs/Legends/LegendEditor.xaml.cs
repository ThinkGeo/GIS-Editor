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


using System.Reflection;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for LegendEditor.xaml
    /// </summary>
    public partial class LegendEditor : Window
    {
        private LegendAdornmentLayerViewModel legendEntity;
        private bool hideNameAndLocationPanel;

        public LegendEditor(LegendAdornmentLayerViewModel legendEntity)
        {
            this.legendEntity = legendEntity;
            InitializeComponent();
            DataContext = legendEntity;
            HideNameAndLocationPanel = false;
            HelpContainer.Content = HelpResourceHelper.GetHelpButton("LegendEditorHelp", HelpButtonMode.Default);
            Messenger.Default.Register<bool>(this, legendEntity, (message) =>
            {
                DialogResult = message;
            });
            this.Unloaded += (s, e) => Messenger.Default.Unregister(this);
        }

        public bool HideNameAndLocationPanel
        {
            get { return hideNameAndLocationPanel; }
            set
            {
                hideNameAndLocationPanel = value;
                if (hideNameAndLocationPanel)
                {
                    NamePanel.Visibility = Visibility.Collapsed;
                    LocationPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    NamePanel.Visibility = Visibility.Visible;
                    LocationPanel.Visibility = Visibility.Visible;
                }
            }
        }

        [Obfuscation]
        private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            legendEntity.PreviewWidth = (int)e.NewSize.Width;
            legendEntity.PreviewHeight = (int)e.NewSize.Height;
        }

        [Obfuscation]
        private void EditMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (legendEntity.IsLegendItemOperationEnabled)
            {
                legendEntity.EditCommand.Execute(null);
            }
        }
    }
}