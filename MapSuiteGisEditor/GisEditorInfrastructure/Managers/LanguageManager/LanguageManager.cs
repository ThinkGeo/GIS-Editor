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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    ///
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(LanguageManager))]
    public class LanguageManager : Manager, INotifyPropertyChanged
    {
        private static string defaultLanguageUri = "/GisEditorInfrastructure;component/languages/en/GisEditorStringResources.xaml";
        private static Dictionary<FrameworkElement, string> localizationElements;
        private static Dictionary<string, ResourceDictionary> resourceDictionaries;
        private static Collection<string> rightToLeftLanguages;

        private FlowDirection flowDirection;

        static LanguageManager()
        {
            InitializeRightToLeftLanguage();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LanguageManager" /> class.
        /// </summary>
        public LanguageManager()
        {
            LanguageSetting.Instance.Language = new CultureInfo("en");
            SetCurrentLanguage(LanguageSetting.Instance.Language);
            localizationElements = new Dictionary<FrameworkElement, string>();
            resourceDictionaries = new Dictionary<string, ResourceDictionary>();
        }

        public FlowDirection FlowDirection
        {
            get { return flowDirection; }
            set
            {
                flowDirection = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("FlowDirection"));
                }
            }
        }

        /// <summary>
        /// Gets the current language.
        /// </summary>
        /// <returns></returns>
        public CultureInfo GetCurrentLanguage() { return GetCurrentLanguageCore(); }

        /// <summary>
        /// Gets the current language core.
        /// </summary>
        /// <returns></returns>
        protected virtual CultureInfo GetCurrentLanguageCore()
        {
            return LanguageSetting.Instance.Language;
        }

        /// <summary>
        /// Sets the current language.
        /// </summary>
        /// <param name="language">The language.</param>
        public void SetCurrentLanguage(CultureInfo language)
        {
            SetCurrentLanguageCore(language);
        }

        /// <summary>
        /// Sets the current language core.
        /// </summary>
        /// <param name="language">The language.</param>
        protected virtual void SetCurrentLanguageCore(CultureInfo language)
        {
            SetFlowDirection(language);
            GetDefaultLanguageUris().ForEach(tempUri => SetDefaultResource(tempUri));

            LanguageSetting.Instance.Language = language;
            var currentLanguageFolderPath = Path.Combine(PluginHelper.GetEntryPath(), "languages", LanguageSetting.Instance.Language.Name);

            if (Application.Current == null) return;

            var nonDefaultResources = Application.Current.Resources.MergedDictionaries
                .Where(md => md.Source.OriginalString.Contains("/languages/") && !GetDefaultLanguageUris().Any(tempUri => md.Source.OriginalString.Equals(tempUri))).ToArray();
            foreach (ResourceDictionary item in nonDefaultResources)
            {
                Application.Current.Resources.MergedDictionaries.Remove(item);
            }

            if (Directory.Exists(currentLanguageFolderPath))
            {
                foreach (var item in Directory.GetFiles(currentLanguageFolderPath, "*.xaml", SearchOption.AllDirectories))
                {
                    Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(item.Replace(@"\", "/"), UriKind.RelativeOrAbsolute) });
                }
            }
        }

        /// <summary>
        /// Gets the available languages.
        /// </summary>
        /// <returns></returns>
        public Collection<CultureInfo> GetAvailableLanguages()
        {
            return GetAvailableLanguagesCore();
        }

        /// <summary>
        /// Gets the available languages core.
        /// </summary>
        /// <returns></returns>
        protected virtual Collection<CultureInfo> GetAvailableLanguagesCore()
        {
            return LanguageSetting.Instance.GetAvailableLanguages();
        }

        /// <summary>
        /// Gets the string resource.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public string GetStringResource(string key)
        {
            return GetStringResourceCore(key);
        }

        /// <summary>
        /// Gets the string resource core.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        protected virtual string GetStringResourceCore(string key)
        {
            if (Application.Current != null && Application.Current.Resources.Contains(key)) return Application.Current.Resources[key].ToString();
            else
            {
                var currentLanguageFolderPath = Path.Combine(PluginHelper.GetEntryPath(), "languages", LanguageSetting.Instance.Language.Name);
                ResourceDictionary rd = new ResourceDictionary();
                if (Directory.Exists(currentLanguageFolderPath))
                {
                    foreach (var item in Directory.GetFiles(currentLanguageFolderPath, "*.xaml", SearchOption.AllDirectories))
                    {
                        rd.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(item.Replace(@"\", "/"), UriKind.RelativeOrAbsolute) });
                    }
                    if (rd.Contains(key)) return rd[key].ToString();
                    else return string.Empty;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the settings core.
        /// </summary>
        /// <returns></returns>
        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            settings.GlobalSettings["Language"] = GetCurrentLanguage().LCID.ToString();
            return settings;
        }

        /// <summary>
        /// Applies the settings core.
        /// </summary>
        /// <param name="settings">The settings.</param>
        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            PluginHelper.RestoreInteger(settings.GlobalSettings, "Language", v =>
                {
                    LanguageSetting.Instance.Language = new CultureInfo(v);
                    SetCurrentLanguage(LanguageSetting.Instance.Language);
                });
        }

        private static IEnumerable<string> GetDefaultLanguageUris()
        {
            yield return defaultLanguageUri;
        }

        private static void SetDefaultResource(string defaultUri)
        {
            if (Application.Current != null)
            {
                var defaultDictionary = Application.Current.Resources.MergedDictionaries.FirstOrDefault(md => md.Source.OriginalString.Equals(defaultUri));
                if (defaultDictionary == null)
                {
                    var defaultLanguageResource = new ResourceDictionary();
                    defaultLanguageResource.Source = new Uri(defaultUri, UriKind.RelativeOrAbsolute);
                    Application.Current.Resources.MergedDictionaries.Add(defaultLanguageResource);
                }
            }
        }

        private void SetFlowDirection(CultureInfo language)
        {
            if (Application.Current != null)
            {
                FlowDirection flowDirection = FlowDirection.LeftToRight;
                if (rightToLeftLanguages.Any(s => s.Equals(language.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    flowDirection = FlowDirection.RightToLeft;
                }
                //Application.Current.Windows.OfType<Window>().ForEach(w => w.FlowDirection = flowDirection);

                FlowDirection = flowDirection;
            }
        }

        private static void InitializeRightToLeftLanguage()
        {
            rightToLeftLanguages = new Collection<string>();
            rightToLeftLanguages.Add("ar");
            rightToLeftLanguages.Add("iw");
            rightToLeftLanguages.Add("syr");
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}