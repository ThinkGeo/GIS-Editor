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
using System.Windows;
using System.Windows.Controls;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ErrorReportUIPlugin : UIPlugin
    {
        private static readonly string emailKey = "EmailKey";
        private ErrorReportSetting option;
        [NonSerialized]
        private ErrorReportOptionUserControl optionUI;
        private string email;

        public ErrorReportUIPlugin()
        {
            option = new ErrorReportSetting();
            Index = UIPluginOrder.ErrorReportPlugin;
            Description = GisEditor.LanguageManager.GetStringResource("ErrorReportUIPluginDescription");
        }

        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        protected override SettingUserControl GetSettingsUICore()
        {
            if (optionUI == null)
            {
                optionUI = new ErrorReportOptionUserControl();
                optionUI.DataContext = new ErrorReportSettingViewModel(option);
            }
            return optionUI;
        }

        protected override void LoadCore()
        {
            base.LoadCore();
            if (GisEditor.LoggerManager != null)
            {
                GisEditor.LoggerManager.Logged -= new System.EventHandler<LoggedLoggerManagerEventArgs>(LoggerManager_Logged);
                GisEditor.LoggerManager.Logged += new System.EventHandler<LoggedLoggerManagerEventArgs>(LoggerManager_Logged);
            }
        }

        protected override void UnloadCore()
        {
            base.UnloadCore();
            if (GisEditor.LoggerManager != null)
            {
                GisEditor.LoggerManager.Logged -= LoggerManager_Logged;
            }
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            if (!string.IsNullOrEmpty(Email)) settings.GlobalSettings[emailKey] = Email;

            foreach (var item in option.SaveState())
            {
                settings.GlobalSettings[item.Key] = item.Value;
            }
            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            option.LoadState(settings.GlobalSettings);
            if (settings.GlobalSettings.ContainsKey(emailKey))
            {
                Email = settings.GlobalSettings[emailKey];
            }
        }

        private void LoggerManager_Logged(object sender, LoggedLoggerManagerEventArgs e)
        {
            if (e.LoggerMessage.LoggerLevel == LoggerLevel.Error && e.LoggerMessage.Error != null)
            {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var error = new ErrorReport();
                        error.Message = e.LoggerMessage.Error.Message;
                        error.StackTrace = e.LoggerMessage.Error.StackTrace;
                        error.Source = e.LoggerMessage.Error.Source;
                        ErrorReportWindow window = new ErrorReportWindow(error);
                        window.NoticeErrorChanged += (s, arg) =>
                        {
                            var box = (CheckBox)s;
                            option.NeedReportUnhandledErrors = box.IsChecked.Value;
                        };

                        window.ShowDialog();
                    }));
                }
            }
        }
    }
}