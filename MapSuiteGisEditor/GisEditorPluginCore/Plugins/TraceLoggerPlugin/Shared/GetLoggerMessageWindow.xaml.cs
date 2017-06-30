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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for GetLoggerMessageWindow.xaml
    /// </summary>
    public partial class GetLoggerMessageWindow : Window
    {
        private Collection<LoggerMessage> resultLoggerMessages;
        private DateTime fromDateTime;
        private DateTime toDateTime;

        public GetLoggerMessageWindow()
        {
            InitializeComponent();

            resultLoggerMessages = new Collection<LoggerMessage>();
            okButton.IsEnabled = false;
            InitializeComboBox();
        }

        public DateTime FromDateTime
        {
            get { return fromDateTime; }
        }

        public DateTime ToDateTime
        {
            get { return toDateTime; }
        }

        public Collection<LoggerMessage> ResultLoggerMessages
        {
            get { return resultLoggerMessages; }
        }

        private void InitializeComboBox()
        {
            IEnumerable<LoggerPlugin> loggerPlugins = GisEditor.LoggerManager.GetActiveLoggerPlugins().Where(p => p.CanGetLoggerMessages);
            pluginComboBox.ItemsSource = loggerPlugins;

            startDatePicker.Value = DateTime.Now.AddMonths(-1);
            toDatePicker.Value = DateTime.Now;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SetResultLoggerMessages();
            DialogResult = true;
        }

        private void SetResultLoggerMessages()
        {
            LoggerPlugin loggerPlugin = (LoggerPlugin)pluginComboBox.SelectedItem;
            Collection<LoggerMessage> loggerMessages = loggerPlugin.GetLoggerMessages(fromDateTime, toDateTime);

            while (loggerMessages.Count > 100)
            {
                loggerMessages.RemoveAt(0);
            }

            foreach (LoggerMessage item in loggerMessages)
            {
                resultLoggerMessages.Add(item);
            }
        }

        [Obfuscation]
        private void PluginComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckOkButtonIsEnable();
        }

        //[Obfuscation]
        //private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    fromDateTime = e.AddedItems.Cast<DateTime>().FirstOrDefault();
        //    CheckOkButtonIsEnable();
        //}

        //[Obfuscation]
        //private void ToDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    toDateTime = e.AddedItems.Cast<DateTime>().FirstOrDefault();
        //    CheckOkButtonIsEnable();
        //}

        private void CheckOkButtonIsEnable()
        {
            int day = DateTime.Compare(fromDateTime, toDateTime);
            errorText.Text = string.Empty;
            if (pluginComboBox.SelectedItem != null && fromDateTime.Year != 0001 && toDateTime.Year != 0001 && day >= 0)
            {
                errorText.Text = GisEditor.LanguageManager.GetStringResource("GetLoggerMessageWindowToDateText");
            }
            
            okButton.IsEnabled = pluginComboBox.SelectedItem != null && fromDateTime.Year != 0001 && toDateTime.Year != 0001 && day < 0;
        }

        [Obfuscation]
        private void StartDatePicker_ValueChanged(object sender, EventArgs e)
        {
            var dateTimePicker = sender as System.Windows.Forms.DateTimePicker;
            if (dateTimePicker != null)
            {
                fromDateTime = dateTimePicker.Value;
                CheckOkButtonIsEnable();
            }
        }

        [Obfuscation]
        private void ToDatePicker_SelectedDateChanged(object sender, EventArgs e)
        {
            var dateTimePicker = sender as System.Windows.Forms.DateTimePicker;
            if (dateTimePicker != null)
            {
                toDateTime = dateTimePicker.Value;
                CheckOkButtonIsEnable();
            }
        }
    }
}