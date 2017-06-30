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

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for GeneralWindow.xaml
    /// </summary>
    public partial class GeneralWindow : Window
    {
        public event RoutedEventHandler OkButtonClicking;

        protected virtual void OnOkButtonClicking(RoutedEventArgs args)
        {
            RoutedEventHandler handler = OkButtonClicking;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        public GeneralWindow()
            : this(null)
        { }

        public GeneralWindow(FrameworkElement helpButtonContent)
        {
            InitializeComponent();

            if (helpButtonContent != null)
            {
                HelpContainer.Content = helpButtonContent;
                HelpContainer.Visibility = Visibility.Visible;
            }
        }

        [Obfuscation]
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            OnOkButtonClicking(e);
            if (!e.Handled)
            {
                DialogResult = true;
            }
        }
    }
}