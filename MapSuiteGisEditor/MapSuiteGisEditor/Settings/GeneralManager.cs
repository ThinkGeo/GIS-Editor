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
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;
using System.Xml.Linq;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// GeneralOption Model
    /// ViewModel: GeneralOptionViewModel
    /// View: GeneralOptionUserControl
    /// </summary>
    [Serializable]
    [Obfuscation]
    public class GeneralManager : Manager
    {
        private static readonly string autoSaveKey = "AutoSaveKey";
        private static readonly string mouseCoordinateKey = typeof(MouseCoordinateType).FullName;
        private static readonly string windowLocationKey = "WindowLocation";
        private static readonly string recentProjectFilesKey = "RecentProjectFiles";
        private static readonly string alwaysShowHintsKey = "ShowHints";
        private static readonly string currentExtentKey = "CurrentExtent";
        private static readonly string ThreadMaxCountKey = "ThreadMaxCount";
        private static readonly string ThreadMinCountKey = "ThreadMinCount";
        private static readonly string IsDisplayAutoSaveKey = "DisplayAutoSave";

        private CultureInfo originalSelectedLanguage;
        private MouseCoordinateType originalSelectedMouseCoordinate;
        private SettingUserControl generalSettingUI;
        private TimeSpan originalAutoSaveTimeSpan;
        private Theme originalTheme;
        private bool originalAutoSave;
        private string windowLocation;
        private string recentProjectFiles;
        private Collection<double> scales;
        private int threadMinCount;
        private int threadMaxCount;
        private bool isDisplayAutoSave;

        //private Dictionary<string, bool> showHints;

        public GeneralManager()
        {
            //showHints = new Dictionary<string, bool>();
            DisplayLanguage = GisEditor.LanguageManager.GetCurrentLanguage();
            originalSelectedLanguage = DisplayLanguage;
            originalSelectedMouseCoordinate = MouseCoordinateType;
            AutoSave = true;
            originalAutoSave = AutoSave;
            AutoSaveInterval = TimeSpan.FromMinutes(5);
            originalAutoSaveTimeSpan = AutoSaveInterval;
            Theme = GisEditor.DockWindowManager.Theme;
            originalTheme = Theme;
            recentProjectFiles = "";
            threadMaxCount = 100;
            threadMinCount = 50;
            isDisplayAutoSave = true;
            scales = new Collection<double>();
            Application.Current.MainWindow.Tag = new Dictionary<string, string>();
        }

        [DataMember]
        public MouseCoordinateType MouseCoordinateType { get; set; }

        [DataMember]
        public Theme Theme
        {
            get { return GisEditor.DockWindowManager.Theme; }
            set { GisEditor.DockWindowManager.Theme = value; }
        }

        [DataMember]
        public bool AutoSave
        {
            get { return GisEditor.ProjectManager.CanAutoBackup; }
            set { GisEditor.ProjectManager.CanAutoBackup = value; }
        }

        public bool IsDisplayAutoSave
        {
            get { return isDisplayAutoSave; }
            set { isDisplayAutoSave = value; }
        }

        [DataMember]
        public TimeSpan AutoSaveInterval
        {
            get { return GisEditor.ProjectManager.AutoBackupInterval; }
            set { GisEditor.ProjectManager.AutoBackupInterval = value; }
        }

        [DataMember]
        public int ThreadMinCount
        {
            get { return threadMinCount; }
            set
            {
                threadMinCount = value;
                ThreadPool.SetMinThreads(threadMinCount, threadMinCount);
            }
        }

        [DataMember]
        public int ThreadMaxCount
        {
            get { return threadMaxCount; }
            set
            {
                threadMaxCount = value;
                ThreadPool.SetMaxThreads(threadMaxCount, threadMaxCount);
            }
        }

        [DataMember]
        public CultureInfo DisplayLanguage
        {
            get { return GisEditor.LanguageManager.GetCurrentLanguage(); }
            set { GisEditor.LanguageManager.SetCurrentLanguage(value); }
        }

        [DataMember]
        public string WindowLocation
        {
            get { return windowLocation; }
            set { windowLocation = value; }
        }

        [DataMember]
        public string RecentProjectFiles
        {
            get { return recentProjectFiles; }
            set { recentProjectFiles = value; }
        }

        [DataMember]
        public Collection<double> Scales
        {
            get { return scales; }
        }

        protected override SettingUserControl GetSettingsUICore()
        {
            if (generalSettingUI == null)
            {
                generalSettingUI = new GeneralSettingUserControl();
                generalSettingUI.DataContext = new GeneralSettingViewModel(this);
            }

            return generalSettingUI;
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            settings.GlobalSettings[autoSaveKey] = AutoSave.ToString();
            settings.GlobalSettings[mouseCoordinateKey] = MouseCoordinateType.ToString();
            settings.GlobalSettings[windowLocationKey] = windowLocation;
            settings.GlobalSettings[recentProjectFilesKey] = recentProjectFiles;
            settings.GlobalSettings[ThreadMaxCountKey] = ThreadMaxCount.ToString();
            settings.GlobalSettings[ThreadMinCountKey] = ThreadMinCount.ToString();
            settings.GlobalSettings[IsDisplayAutoSaveKey] = IsDisplayAutoSave.ToString();

            if (Application.Current != null && Application.Current.MainWindow.Tag is Dictionary<string, string>)
            {
                settings.GlobalSettings[alwaysShowHintsKey] = (Application.Current.MainWindow.Tag as Dictionary<string, string>)["ShowHintSettings"];
                settings.GlobalSettings[currentExtentKey] = (Application.Current.MainWindow.Tag as Dictionary<string, string>)[currentExtentKey];
            }
            XElement xml = new XElement("Scales");
            foreach (var scale in scales)
            {
                xml.Add(new XElement("scale", scale));
            }
            settings.GlobalSettings["Scales"] = xml.ToString();

            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            RestoreBool(settings.GlobalSettings, autoSaveKey, a => AutoSave = a);
            RestoreEnum<MouseCoordinateType>(settings.GlobalSettings, mouseCoordinateKey, m => MouseCoordinateType = m);
            RestoreString(settings.GlobalSettings, windowLocationKey, w => windowLocation = w);
            RestoreInt(settings.GlobalSettings, ThreadMaxCountKey, w => ThreadMaxCount = w);
            RestoreInt(settings.GlobalSettings, ThreadMinCountKey, w => ThreadMinCount = w);
            RestoreString(settings.GlobalSettings, recentProjectFilesKey, r => recentProjectFiles = r);
            RestoreBool(settings.GlobalSettings, IsDisplayAutoSaveKey, r => IsDisplayAutoSave = r);

            if (!settings.GlobalSettings.ContainsKey("ShowHints"))
            {
                settings.GlobalSettings["ShowHints"] = new XElement("ShowHints", "").ToString();
            }

            if (!settings.GlobalSettings.ContainsKey(currentExtentKey))
            {
                settings.GlobalSettings[currentExtentKey] = new XElement(currentExtentKey, "").ToString();
            }

            if (Application.Current != null && Application.Current.MainWindow.Tag is Dictionary<string, string>)
            {
                (Application.Current.MainWindow.Tag as Dictionary<string, string>)["ShowHintSettings"] = settings.GlobalSettings["ShowHints"];
                (Application.Current.MainWindow.Tag as Dictionary<string, string>)[currentExtentKey] = settings.GlobalSettings[currentExtentKey];
            }

            if (settings.GlobalSettings.ContainsKey("Scales"))
            {
                try
                {
                    var xEl = XDocument.Parse(settings.GlobalSettings["Scales"]);
                    scales.Clear();
                    foreach (var item in xEl.Descendants("scale"))
                    {
                        double scale = 0;
                        if (double.TryParse(item.Value, out scale)) scales.Add(scale);
                    }
                }
                catch { }
            }
        }

        private static void RestoreString(Dictionary<string, string> items, string key, Action<string> action)
        {
            if (items.ContainsKey(key))
            {
                action(items[key]);
            }
        }

        private static void RestoreLong(Dictionary<string, string> items, string key, Action<long> action)
        {
            RestoreString(items, key, str =>
            {
                long oldValue = 0;
                if (long.TryParse(str, out oldValue))
                {
                    action(oldValue);
                }
            });
        }

        private static void RestoreInt(Dictionary<string, string> items, string key, Action<int> action)
        {
            RestoreString(items, key, str =>
            {
                int oldValue = 0;
                if (int.TryParse(str, out oldValue))
                {
                    action(oldValue);
                }
            });
        }

        private static void RestoreBool(Dictionary<string, string> items, string key, Action<bool> action)
        {
            RestoreString(items, key, str =>
            {
                bool oldValue = false;
                if (bool.TryParse(str, out oldValue))
                {
                    action(oldValue);
                }
            });
        }

        private static void RestoreEnum<T>(Dictionary<string, string> items, string key, Action<T> action) where T : struct
        {
            RestoreString(items, key, str =>
            {
                T oldValue = default(T);
                if (Enum.TryParse<T>(str, out oldValue))
                {
                    action(oldValue);
                }
            });
        }
    }
}