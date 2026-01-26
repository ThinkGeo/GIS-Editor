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
using System.Globalization;
using System.Windows.Controls;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class ServerFeatureLayerSettingsUserControl : SettingUserControl
    {
        private const int MinTimeout = 1;
        private const int MaxTimeout = 1000;
        private const int DefaultTimeout = 20;

        public ServerFeatureLayerSettingsUserControl()
        {
            Title = "Server";
            InitializeComponent();
        }

        public int PostgreTimeoutInSecond
        {
            get { return GetTimeoutValue(TimeoutTextBox); }
            set { SetTimeoutValue(TimeoutTextBox, value); }
        }

        public int SQLTimeoutInSecond
        {
            get { return GetTimeoutValue(SQLTimeoutTextBox); }
            set { SetTimeoutValue(SQLTimeoutTextBox, value); }
        }

        private static int GetTimeoutValue(TextBox textBox)
        {
            if (textBox == null) return DefaultTimeout;

            if (!int.TryParse(textBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return DefaultTimeout;
            }

            return Math.Max(MinTimeout, Math.Min(MaxTimeout, value));
        }

        private static void SetTimeoutValue(TextBox textBox, int value)
        {
            if (textBox == null) return;

            value = Math.Max(MinTimeout, Math.Min(MaxTimeout, value));
            textBox.Text = value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
