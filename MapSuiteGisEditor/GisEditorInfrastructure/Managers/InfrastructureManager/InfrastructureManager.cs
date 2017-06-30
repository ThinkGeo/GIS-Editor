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


using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Manager of all options in explorer, including options of plugins, infrastructures and shell.
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(InfrastructureManager))]
    public class InfrastructureManager : Manager
    {
        private static readonly string settingFolderName = "MapSuiteGisEditor";
        private static string globalSettingsPathFileName;
        private static string projectSettingsPathFileName;

        private string editonName;
        private string settingsPath;
        private string temporaryPath;
        private Collection<Manager> managers;
        private InfrastructureSettingUserControl settingUI;
        private InfrastructureSettingViewModel settingViewModel;

        /// <summary>
        /// Initialize an instance of OptionManager.
        /// </summary>
        public InfrastructureManager()
        {
            settingViewModel = new InfrastructureSettingViewModel();
        }

        /// <summary>
        /// Gets or sets the settings path.
        /// </summary>
        /// <value>
        /// The settings path.
        /// </value>
        public string SettingsPath
        {
            get
            {
                if (string.IsNullOrEmpty(settingsPath))
                {
                    string localApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    settingsPath = Path.Combine(localApplicationDataPath, settingFolderName);
                    if (!Directory.Exists(settingsPath))
                    {
                        Directory.CreateDirectory(settingsPath);
                    }
                }
                return settingsPath;
            }
            set { settingsPath = value; }
        }

        /// <summary>
        /// Gets or sets the name of the edition.
        /// </summary>
        /// <value>
        /// The name of the edition.
        /// </value>
        public string EditionName
        {
            get
            {
                return editonName;
            }
            set
            {
                editonName = value;
            }
        }

        public ObservableCollection<string> PluginDirectories
        {
            get
            {
                return settingViewModel.PluginDirectories;
            }
        }

        /// <summary>
        /// Gets or sets the temporary path.
        /// </summary>
        /// <value>
        /// The temporary path.
        /// </value>
        public string TemporaryPath
        {
            get
            {
                if (String.IsNullOrEmpty(temporaryPath))
                {
                    temporaryPath = GetTemporaryFolders().First(tmp => Directory.Exists(tmp));
                    temporaryPath = Path.Combine(temporaryPath, settingFolderName);
                    if (!Directory.Exists(temporaryPath))
                    {
                        Directory.CreateDirectory(temporaryPath);
                    }
                }

                return temporaryPath;
            }
            set { temporaryPath = value; }
        }

        /// <summary>
        /// Gets the managers.
        /// </summary>
        /// <returns></returns>
        public Collection<Manager> GetManagers()
        {
            if (managers == null)
            {
                managers = GetManagersCore();
                var tmpSettingsManager = managers.FirstOrDefault(m => m is InfrastructureManager);
                if (tmpSettingsManager != null)
                {
                    managers.Remove(tmpSettingsManager);
                }

                managers.Insert(0, this);
            }

            return new Collection<Manager>(managers);
        }

        /// <summary>
        /// Gets the managers core.
        /// </summary>
        /// <returns></returns>
        protected virtual Collection<Manager> GetManagersCore()
        {
            Collection<Manager> managers = new Collection<Manager>(PluginHelper.GetExportedPlugins<Manager>());
            return managers;
        }

        #region SaveSettings

        public void SaveSettings(IStorableSettings storableSettings)
        {
            SaveSettings(new IStorableSettings[] { storableSettings });
        }

        public void SaveSettings(IEnumerable<IStorableSettings> storableSettings)
        {
            SaveSettingsCore(storableSettings);
        }

        protected virtual void SaveSettingsCore(IEnumerable<IStorableSettings> storableSettings)
        {
            //if (File.Exists(GlobalsSettingsPathFileName)) File.Delete(GlobalsSettingsPathFileName);
            //if (File.Exists(ProjectSettingsPathFileName)) File.Delete(ProjectSettingsPathFileName);

            IonicZipFileAdapter globalsZipFileAdapter = null;
            IonicZipFileAdapter projectZipFileAdapter = null;

            bool needSaveGlobalsSettings = false;
            bool needSaveProjectSettings = false;

            try
            {
                globalsZipFileAdapter = new IonicZipFileAdapter(GlobalsSettingsPathFileName);
                projectZipFileAdapter = new IonicZipFileAdapter(ProjectSettingsPathFileName);
                foreach (var setting in storableSettings)
                {
                    var settings = setting.GetSettings();
                    if (settings.GlobalSettings.Count > 0) needSaveGlobalsSettings = true;
                    if (settings.ProjectSettings.Count > 0 && GisEditor.ProjectManager.IsLoaded) needSaveProjectSettings = true;

                    try
                    {
                        if (needSaveGlobalsSettings)
                        {
                            SettingAdapter globalsAdapter = new InfrastructureSettingAdapter(setting);
                            XElement globalsXmlElement = SettingAdapter.ToXml(settings.GlobalSettings, globalsAdapter.GetCoreType());
                            SaveSingleSettings(globalsZipFileAdapter, setting, globalsXmlElement);
                        }

                        if (needSaveProjectSettings)
                        {
                            SettingAdapter projectAdapter = new ProjectSettingAdapter(setting);
                            XElement projectXmlElement = SettingAdapter.ToXml(settings.ProjectSettings, projectAdapter.GetCoreType());
                            SaveSingleSettings(projectZipFileAdapter, setting, projectXmlElement);
                        }
                    }
                    catch (Exception ex)
                    {
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    }
                }

                if (needSaveGlobalsSettings) globalsZipFileAdapter.Save(GlobalsSettingsPathFileName);
                if (needSaveProjectSettings) projectZipFileAdapter.Save(ProjectSettingsPathFileName);
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            finally
            {
                if (globalsZipFileAdapter != null) globalsZipFileAdapter.Dispose();
                if (projectZipFileAdapter != null) projectZipFileAdapter.Dispose();
            }
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            return settings;
        }

        protected override SettingUserControl GetSettingsUICore()
        {
            if (settingUI == null)
            {
                settingUI = new InfrastructureSettingUserControl();
                settingUI.DataContext = settingViewModel;
            }

            return settingUI;
        }

        private static void SaveSingleSettings(ZipFileAdapter zipFileAdapter, IStorableSettings setting, XElement xmlElement)
        {
            if (xmlElement != null)
            {
                var stream = new MemoryStream();
                xmlElement.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);
                string currentSettingPathFileName = setting.GetType().FullName + ".xml";
                if (zipFileAdapter.GetEntryNames().Contains(currentSettingPathFileName))
                {
                    zipFileAdapter.RemoveEntry(currentSettingPathFileName);
                }
                zipFileAdapter.AddEntity(currentSettingPathFileName, stream);
            }
        }

        private void SaveXmlToFile(string pathFileName, XElement globalsSettingXml, bool needSaveGlobalsSettings)
        {
            if (needSaveGlobalsSettings)
            {
                CreateDirectoryIfNotExists(pathFileName);
                globalsSettingXml.Save(pathFileName);
            }
        }

        private static void CreateDirectoryIfNotExists(string pathFileName)
        {
            var folder = Path.GetDirectoryName(pathFileName);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        private static void ReplaceXmlIfExists(XElement globalsSettingXml, SettingAdapter adapter, XElement newElement)
        {
            if (newElement != null)
            {
                XElement oldElement = globalsSettingXml.Descendants("State").FirstOrDefault(x => x.Attribute("Name").Value.Equals(adapter.GetCoreType().FullName, StringComparison.Ordinal));
                if (oldElement == null) globalsSettingXml.Add(newElement);
                else oldElement.ReplaceWith(newElement);
            }
        }

        private XElement GetSettingXml(string settingsPathFileName)
        {
            XElement settingXml = null;
            if (File.Exists(settingsPathFileName)) settingXml = XElement.Load(settingsPathFileName);
            else settingXml = new XElement("MapSuiteGisEditor");
            return settingXml;
        }

        #endregion SaveSettings

        #region ApplySettings

        /// <summary>
        /// Applies the settings.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public void ApplySettings(IStorableSettings settings)
        {
            ApplySettings(new IStorableSettings[] { settings });
        }

        /// <summary>
        /// Applies the settings.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public void ApplySettings(IEnumerable<IStorableSettings> settings)
        {
            ApplySettingsCore(settings);
        }

        /// <summary>
        /// Applies the settings core.
        /// </summary>
        /// <param name="settings">The settings.</param>
        protected void ApplySettingsCore(IEnumerable<IStorableSettings> settings)
        {
            ApplySettingsInternal(settings);
        }

        private void ApplySettingsInternal(IEnumerable<IStorableSettings> settings)
        {
            IonicZipFileAdapter globalsZipAdapter = null;
            IonicZipFileAdapter projectZipAdapter = null;

            try
            {
                globalsZipAdapter = new IonicZipFileAdapter(GlobalsSettingsPathFileName);
                projectZipAdapter = new IonicZipFileAdapter(ProjectSettingsPathFileName);
                foreach (var item in settings)
                {
                    string typeFullName = item.GetType().FullName;
                    StorableSettings tmpSettings = new StorableSettings();
                    FillSettings(globalsZipAdapter, typeFullName, tmpSettings, s => s.GlobalSettings);
                    FillSettings(projectZipAdapter, typeFullName, tmpSettings, s => tmpSettings.ProjectSettings);
                    item.ApplySettings(tmpSettings);
                }
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            finally
            {
                if (globalsZipAdapter != null) globalsZipAdapter.Dispose();
                if (projectZipAdapter != null) projectZipAdapter.Dispose();
            }
        }

        private void FillSettings(IonicZipFileAdapter zipAdapter, string typeFullName, StorableSettings settings, Func<StorableSettings, Dictionary<string, string>> getSettingDictionary)
        {
            string entryName = typeFullName + ".xml";
            XElement xml = null;
            Stream xmlStream = zipAdapter.GetEntryStreamByName(entryName);
            if (xmlStream != null)
            {
                xml = XElement.Load(xmlStream);
            }

            if (xml != null)
            {
                var currentSettingStateXml = xml;
                Dictionary<string, string> sourceStateDictionary = SettingAdapter.FromXml(currentSettingStateXml);
                Dictionary<string, string> targetStateDictionary = getSettingDictionary(settings);
                foreach (var stateItem in sourceStateDictionary)
                {
                    targetStateDictionary.Add(stateItem.Key, stateItem.Value);
                }
            }
        }

        private string GlobalsSettingsPathFileName
        {
            get
            {
                globalSettingsPathFileName = Path.Combine(GisEditor.InfrastructureManager.SettingsPath, "MapSuiteGisEditor.settings");
                return globalSettingsPathFileName;
            }
        }

        private string ProjectSettingsPathFileName
        {
            get
            {
                if (string.IsNullOrEmpty(projectSettingsPathFileName))
                {
                    projectSettingsPathFileName = ProjectPluginManager.ProjectSettingsPathFileName;
                }
                return projectSettingsPathFileName;
            }
        }

        private static IEnumerable<string> GetTemporaryFolders()
        {
            yield return Environment.GetEnvironmentVariable("Temp");
            yield return Environment.GetEnvironmentVariable("Tmp");
            yield return @"c:\MapSuiteTemp";
            yield return @"\MapSuite";
        }

        #endregion ApplySettings

        #region CustomSettings

        private class ExtraSettings : IStorableSettings
        {
            private Dictionary<string, string> items;

            public ExtraSettings(Dictionary<string, string> items)
            {
                this.items = items;
            }

            public StorableSettings GetSettings()
            {
                StorableSettings settings = new StorableSettings();
                settings.GlobalSettings["Items"] = JsonConvert.SerializeObject(GetSettingsCore());
                return settings;
            }

            public void ApplySettings(StorableSettings storableSettings)
            {
                if (storableSettings != null && storableSettings.GlobalSettings.ContainsKey("Items"))
                {
                    ApplySettingsCore(JsonConvert.DeserializeObject<XElement>(storableSettings.GlobalSettings["Items"]));
                }
            }

            public SettingUserControl GetSettingsUI()
            {
                return null;
            }

            private XElement GetSettingsCore()
            {
                XElement xElement = new XElement("CustomStates");
                foreach (var item in items)
                {
                    xElement.Add(new XElement("Item"
                        , new XAttribute("Key", item.Key),
                        item.Value));
                }

                return xElement;
            }

            private void ApplySettingsCore(XElement stateXml)
            {
                items.Clear();
                if (stateXml != null)
                {
                    foreach (var xElement in stateXml.Descendants("Item"))
                    {
                        items.Add(xElement.Attribute("Key").Value, xElement.Value);
                    }
                }
            }
        }

        #endregion CustomSettings
    }
}