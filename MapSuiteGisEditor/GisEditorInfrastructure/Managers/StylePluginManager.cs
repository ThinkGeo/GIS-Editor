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


using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(StylePluginManager))]
    public class StylePluginManager : PluginManager
    {
        private static readonly string fileFormat = ".tgsty";

        private Collection<string> folders;

        /// <summary>
        /// Initializes a new instance of the <see cref="StylePluginManager" /> class.
        /// </summary>
        public StylePluginManager()
            : base()
        {
            UseWizard = true;
            folders = new Collection<string>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether [use wizard].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use wizard]; otherwise, <c>false</c>.
        /// </value>
        public bool UseWizard { get; set; }

        internal Collection<string> Folders
        {
            get { return folders; }
        }

        /// <summary>
        /// Gets the plugins core.
        /// </summary>
        /// <returns></returns>
        protected override Collection<Plugin> GetPluginsCore()
        {
            var result = new Collection<Plugin>();
            var exportedPlugins = CollectPlugins<StylePlugin>().OrderBy(l => l.Index).OfType<StylePlugin>();
            var exportedPluginGroup = exportedPlugins.GroupBy(p => p.GetDefaultStyle().GetType());

            foreach (var item in exportedPluginGroup)
            {
                int itemCount = item.Count();
                if (itemCount == 1)
                {
                    result.Add(item.First());
                }
                else if (itemCount > 1)
                {
                    var customPlugin = item.FirstOrDefault(i => !i.GetType().Assembly.Location.Equals(DefaultPluginPathFileName, StringComparison.OrdinalIgnoreCase));
                    if (customPlugin != null)
                    {
                        result.Add(customPlugin);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the style plugins.
        /// </summary>
        /// <returns></returns>
        public Collection<StylePlugin> GetStylePlugins()
        {
            return new Collection<StylePlugin>(GetPlugins().Cast<StylePlugin>().ToList());
        }

        public Collection<T> GetActiveStylePlugins<T>() where T : StylePlugin
        {
            return new Collection<T>(GetActiveStylePlugins().OfType<T>().ToList());
        }

        /// <summary>
        /// Gets the style plugins.
        /// </summary>
        /// <returns></returns>
        public Collection<StylePlugin> GetActiveStylePlugins()
        {
            var activePlugins = from p in GetStylePlugins()
                                where p.IsActive
                                orderby p.Index
                                select p;

            return new Collection<StylePlugin>(activePlugins.ToList());
        }

        /// <summary>
        /// Applies the settings core.
        /// </summary>
        /// <param name="settings">The settings.</param>
        protected override void ApplySettingsCore(StorableSettings settings)
        {
            bool useWizard = false;
            base.ApplySettingsCore(settings);
            if (settings.GlobalSettings.ContainsKey("UseWizard") && bool.TryParse(settings.GlobalSettings["UseWizard"], out useWizard))
            {
                UseWizard = useWizard;
            }
            if (settings.GlobalSettings.ContainsKey("StyleLibraryFolders"))
            {
                try
                {
                    folders.Clear();
                    var xEl = XDocument.Parse(settings.GlobalSettings["StyleLibraryFolders"]);
                    foreach (var folderElement in xEl.Descendants("Folder"))
                    {
                        if (Directory.Exists(folderElement.Value))
                        {
                            folders.Add(folderElement.Value);
                        }
                    }
                }
                catch (Exception exception)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, exception.Message, new ExceptionInfo(exception));
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
            settings.GlobalSettings["UseWizard"] = UseWizard.ToString();
            if (folders.Count > 0)
            {
                XElement rootXEl = new XElement("Folders");
                foreach (var folder in folders)
                {
                    rootXEl.Add(new XElement("Folder", folder));
                }
                settings.GlobalSettings["StyleLibraryFolders"] = rootXEl.ToString();
            }
            return settings;
        }

        /// <summary>
        /// Gets the style plugin by style.
        /// </summary>
        /// <param name="style">The style.</param>
        /// <returns></returns>
        public StylePlugin GetStylePluginByStyle(Style style)
        {
            return GetStylePluginByStyleCore(style);
        }

        /// <summary>
        /// Gets the style plugin by style core.
        /// </summary>
        /// <param name="style">The style.</param>
        /// <returns></returns>
        protected virtual StylePlugin GetStylePluginByStyleCore(Style style)
        {
            var pluginsToGet = GetActiveStylePlugins();

            var provider = pluginsToGet.FirstOrDefault(tmpProvider
                => tmpProvider.GetDefaultStyle().GetType().Equals(style.GetType()) && !tmpProvider.IsDefault);

            if (provider == null)
            {
                provider = pluginsToGet.FirstOrDefault(tmpProvider
                    => style.GetType().IsSubclassOf(tmpProvider.GetDefaultStyle().GetType()) && !tmpProvider.IsDefault);
            }

            if (provider == null)
            {
                provider = pluginsToGet.FirstOrDefault(tmpProvider
                    => tmpProvider.GetDefaultStyle().GetType().Equals(style.GetType()) && tmpProvider.IsDefault);
            }

            if (provider == null)
            {
                provider = pluginsToGet.FirstOrDefault(tmpProvider
                   => style.GetType().IsSubclassOf(tmpProvider.GetDefaultStyle().GetType()) && tmpProvider.IsDefault);
            }

            return provider;
        }

        /// <summary>
        /// Gets the style plugins.
        /// </summary>
        /// <param name="styleCategories">The style categories.</param>
        /// <returns></returns>
        public Collection<StylePlugin> GetStylePlugins(StyleCategories styleCategories)
        {
            return GetStylePluginsCore(styleCategories);
        }

        /// <summary>
        /// Gets the style plugins core.
        /// </summary>
        /// <param name="styleCategories">The style categories.</param>
        /// <returns></returns>
        protected virtual Collection<StylePlugin> GetStylePluginsCore(StyleCategories styleCategories)
        {
            Collection<StylePlugin> stylePlugins = new Collection<StylePlugin>();
            var activeStylePlugins = GetActiveStylePlugins();
            foreach (var stylePlugin in activeStylePlugins.Where(p => styleCategories.HasFlag(p.StyleCategories)).OrderBy(p => p.Index))
            {
                stylePlugins.Add(stylePlugin);
            }

            return stylePlugins;
        }

        public StyleBuilderWindow GetStyleBuiderUI(StyleBuilderArguments arguments)
        {
            return GetStyleBuiderUICore(arguments);
        }

        public StyleBuilderWindow GetStyleBuiderUI()
        {
            return GetStyleBuiderUI(null);
        }

        protected virtual StyleBuilderWindow GetStyleBuiderUICore(StyleBuilderArguments arguments)
        {
            StyleBuilderWindow window = new StyleBuilderWindow();
            window.StyleBuilderArguments = arguments;
            return window;
        }

        /// <summary>
        /// Gets the default style plugin.
        /// </summary>
        /// <param name="styleProviderTypes">The style provider types.</param>
        /// <returns></returns>
        public StylePlugin GetDefaultStylePlugin(StyleCategories styleProviderTypes)
        {
            return GetDefaultStylePluginCore(styleProviderTypes);
        }

        /// <summary>
        /// Gets the default style plugin core.
        /// </summary>
        /// <param name="styleProviderTypes">The style provider types.</param>
        /// <returns></returns>
        protected virtual StylePlugin GetDefaultStylePluginCore(StyleCategories styleProviderTypes)
        {
            var allSupportedProviders = GetStylePlugins(styleProviderTypes);
            var styleProvider = allSupportedProviders.FirstOrDefault(p => p.IsDefault);
            if (styleProvider == null)
            {
                styleProvider = allSupportedProviders.FirstOrDefault();
            }

            return styleProvider;
        }

        /// <summary>
        /// Gets the style layer list item.
        /// </summary>
        /// <param name="style">The style.</param>
        /// <returns></returns>
        public StyleLayerListItem GetStyleLayerListItem(Style style)
        {
            if (style is CompositeStyle)
            {
                var componentStyle = style as CompositeStyle;
                var componentStyleItem = new ComponentStyleItem(componentStyle);
                componentStyleItem.ConcreteObjectUpdated += (s, e) =>
                {
                    var styleItem = s as StyleLayerListItem;
                    if (styleItem.ConcreteObject == componentStyle)
                    {
                        componentStyle.Styles.Clear();
                        foreach (var item in (s as StyleLayerListItem).Children)
                        {
                            componentStyle.Styles.Insert(0, item.ConcreteObject as Style);
                        }
                    }
                };
                return componentStyleItem;
            }
            else
                return GetStyleLayerListItemCore(style);
        }

        /// <summary>
        /// Gets the style layer list item core.
        /// </summary>
        /// <param name="style">The style.</param>
        /// <returns></returns>
        protected virtual StyleLayerListItem GetStyleLayerListItemCore(Style style)
        {
            StylePlugin stylePlugin = GetStylePluginByStyle(style);
            if (stylePlugin != null)
            {
                return stylePlugin.GetStyleLayerListItem(style);
            }
            else return null;
        }

        /// <summary>
        /// Edits the style.
        /// </summary>
        /// <param name="styleBuilderArguments">The style builder arguments.</param>
        /// <returns></returns>
        public StyleBuilderResult EditStyle(StyleBuilderArguments styleBuilderArguments)
        {
            return EditStyleCore(styleBuilderArguments);
        }

        /// <summary>
        /// Edits the style core.
        /// </summary>
        /// <param name="styleBuilderArguments">The style builder arguments.</param>
        /// <param name="selectedConcreteObject">The selected concrete object.</param>
        /// <returns></returns>
        protected virtual StyleBuilderResult EditStyleCore(StyleBuilderArguments styleBuilderArguments)
        {
            StyleBuilderResult styleEditResult = new StyleBuilderResult();
            StyleBuilderWindow styleBuilder = new StyleBuilderWindow(styleBuilderArguments);
            styleBuilder.IsSubStyleReadonly = styleBuilderArguments.IsSubStyleReadonly;
            styleEditResult.Canceled = !styleBuilder.ShowDialog().Value;
            styleEditResult.FromZoomLevelIndex = styleBuilder.StyleBuilderResult.FromZoomLevelIndex;
            styleEditResult.ToZoomLevelIndex = styleBuilder.StyleBuilderResult.ToZoomLevelIndex;
            styleEditResult.CompositeStyle = styleBuilder.StyleBuilderResult.CompositeStyle;
            return styleEditResult;
        }

        /// <summary>
        /// Saves the style to library.
        /// </summary>
        /// <param name="compositeStyle">The composite style.</param>
        /// <param name="lowerScale">The lower scale.</param>
        /// <param name="upperScale">The upper scale.</param>
        public void SaveStyleToLibrary(CompositeStyle compositeStyle, double lowerScale, double upperScale)
        {
            SaveStyleToLibraryCore(compositeStyle, lowerScale, upperScale);
        }

        /// <summary>
        /// Saves the style to library core.
        /// </summary>
        /// <param name="compositeStyle">The composite style.</param>
        /// <param name="lowerScale">The lower scale.</param>
        /// <param name="upperScale">The upper scale.</param>
        protected virtual void SaveStyleToLibraryCore(CompositeStyle compositeStyle, double lowerScale, double upperScale)
        {
            string savePath = StyleLibraryViewModel.StyleLibraryFolder;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = savePath;
            saveFileDialog.FileName = compositeStyle.Name + fileFormat;
            saveFileDialog.Filter = "ThinkGeo style files (*.tgsty)|*.tgsty";
            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                var folder = Path.GetDirectoryName(saveFileDialog.FileName);
                if (!folder.StartsWith(StyleLibraryViewModel.BaseFolder, StringComparison.InvariantCultureIgnoreCase) && !GisEditor.StyleManager.Folders.Contains(folder))
                    GisEditor.StyleManager.Folders.Add(folder);
                var styleXml = GisEditor.Serializer.Serialize(compositeStyle);
                var rootXml = new XElement("Style"
                    , new XElement("LowerScale", lowerScale)
                    , new XElement("UpperScale", upperScale)
                    , XElement.Parse(styleXml));

                rootXml.Save(saveFileDialog.FileName);
            }
        }
    }
}