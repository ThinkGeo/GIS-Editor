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
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represents a base class of Plugins in all GISEditor system. 
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(Plugin))]
    public abstract class Plugin : IStorableSettings
    {
        private static readonly string uriFormat = "/{0};component/{1}";
        private string name;
        private string author;
        private string description;

        [NonSerialized]
        private ImageSource smallIcon;

        [NonSerialized]
        private ImageSource largeIcon;

        private bool isRequired;
        private bool isActive;
        private int index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin" /> class.
        /// </summary>
        protected Plugin()
        {
            Author = "ThinkGeo LLC.";
            Name = GetType().Name;
            Description = Name;
            Index = Int32.MaxValue;
            IsActive = true;

            string iconUri = String.Format(CultureInfo.InvariantCulture
                , uriFormat
                , Path.GetFileNameWithoutExtension(typeof(Plugin).Assembly.Location)
                , "Images/Gear.png");

            SmallIcon = new BitmapImage(new Uri(iconUri, UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri(iconUri, UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// Gets or sets the name of plugin.
        /// </summary>
        /// <value>
        /// The name of plugin.
        /// </value>
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    name = GetType().Name;
                }
                return name;
            }
            protected set
            {
                name = value;
            }
        }

        /// <summary>
        /// Gets the identify of plugin.
        /// </summary>
        /// <value>
        /// The id of plugin.
        /// </value>
        public string Id { get { return String.Format(CultureInfo.InvariantCulture, "{0};{1}", GetType().FullName, GetType().Assembly.FullName); } }

        /// <summary>
        /// Gets or sets author information for plugin.
        /// </summary>
        public string Author
        {
            get { return author; }
            protected set { author = value; }
        }

        /// <summary>
        /// Gets or sets description for plugin.
        /// </summary>
        public string Description
        {
            get { return description; }
            protected set { description = value; }
        }

        /// <summary>
        /// Gets or sets small icon path for plugin.
        /// </summary>
        /// <remarks>
        /// Setting relative path to this property to discover image.
        /// Path should be like the form of [FolderName(optional)]/ImageFullName,
        /// such as "Images/NewContent.png" or "Content.png".
        /// Make sure that image you supplied compiling with Build Action Resource.
        /// </remarks>
        public ImageSource SmallIcon
        {
            get { return smallIcon; }
            protected set { smallIcon = value; }
        }

        /// <summary>
        /// Gets or sets large icon for plugin.
        /// </summary>
        /// <remarks>
        /// Setting relative path to this property to discover image.
        /// Path should be like the form of [FolderName(optional)]/ImageFullName,
        /// such as "Images/NewContent.png" or "Content.png".
        /// Make sure that image you supplied compiling with Build Action Resource.
        /// </remarks>
        [DataMember(IsRequired = false)]
        public ImageSource LargeIcon
        {
            get { return largeIcon; }
            protected set { largeIcon = value; }
        }

        /// <summary>
        /// Gets or sets the index of the plugin.
        /// </summary>
        /// <remarks>
        /// Plugins are orded by this property.
        /// </remarks>
        /// <value>
        /// The index.
        /// </value>
        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this plugin is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this plugin is active; otherwise, <c>false</c>.
        /// </value>
        public bool IsActive
        {
            get { return isActive; }
            set { isActive = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this plugin is required.
        /// </summary>
        /// <value>
        /// <c>true</c> if this plugin is required; otherwise, <c>false</c>.
        /// </value>
        public bool IsRequired
        {
            get { return isRequired; }
            set { isRequired = value; }
        }

        /// <summary>
        /// This method raises when load this plugin.
        /// </summary>
        public void Load()
        {
            try
            {
                LoadCore();
            }
            catch (TypeLoadException ex)
            {
                HandleTypeLoadException(ex);
            }
        }

        /// <summary>
        /// This method raises when load this plugin.
        /// </summary>
        protected virtual void LoadCore()
        { }

        /// <summary>
        /// This method raises when unload this plugin.
        /// </summary>
        public void Unload()
        {
            try
            {
                UnloadCore();
            }
            catch (TypeLoadException ex)
            {
                HandleTypeLoadException(ex);
            }
        }

        /// <summary>
        /// This method raises when unload this plugin.
        /// </summary>
        protected virtual void UnloadCore()
        { }

        /// <summary>
        /// Gets plugin settings to save.
        /// </summary>
        /// <returns>Plugin's settings to save.</returns>
        public StorableSettings GetSettings()
        {
            var settings = GetSettingsCore();
            settings.GlobalSettings["IsRequired"] = IsRequired.ToString();
            settings.GlobalSettings["IsActive"] = IsActive.ToString();
            settings.GlobalSettings["Index"] = Index.ToString();
            return settings;
        }

        /// <summary>
        /// Gets plugin settings to save.
        /// </summary>
        /// <returns>Plugin's settings to save.</returns>
        protected virtual StorableSettings GetSettingsCore()
        {
            return new StorableSettings();
        }

        /// <summary>
        /// Applies the settings to this plugin.
        /// </summary>
        /// <param name="settings">The settings to be applied to this plugin.</param>
        public void ApplySettings(StorableSettings settings)
        {
            //PluginHelper.RestoreBoolean(settings.GlobalSettings, "IsRequired", v => IsRequired = v);
            PluginHelper.RestoreBoolean(settings.GlobalSettings, "IsActive", v => IsActive = IsRequired || v);
            PluginHelper.RestoreInteger(settings.GlobalSettings, "Index", v => Index = v);

            ApplySettingsCore(settings);
        }

        /// <summary>
        /// Applies the settings to this plugin.
        /// </summary>
        /// <param name="settings">The settings to be applied to this plugin.</param>
        protected virtual void ApplySettingsCore(StorableSettings settings)
        {
        }

        /// <summary>
        /// Gets an UI that configures settings.
        /// </summary>
        /// <returns>A SettingUserControl that configures settings.</returns>
        public SettingUserControl GetSettingsUI()
        {
            return GetSettingsUICore();
        }

        /// <summary>
        /// Gets an UI that configures settings.
        /// </summary>
        /// <returns>A SettingUserControl that configures settings.</returns>
        protected virtual SettingUserControl GetSettingsUICore()
        {
            return null;
        }

        private static Dictionary<Plugin, Collection<string>> existedExceptions = new Dictionary<Plugin, Collection<string>>();

        internal void HandleTypeLoadException(TypeLoadException ex)
        {
            if (ex == null) return;

            string message = ex.Message;

            if (!existedExceptions.ContainsKey(this))
            {
                existedExceptions[this] = new Collection<string>();
            }

            if (!existedExceptions[this].Contains(message))
            {
                existedExceptions[this].Add(message);

                //string tempMessage1 = string.Format(CultureInfo.InvariantCulture, "A TypeLoadException is caught. This exception might cause by a breaking change of MapSuite APIs. It will break functionality of {0}. Please ", ex.Source);
                //string tempMessage2 = string.Format(CultureInfo.InvariantCulture, " to update corresponding assemblies. ");

                GisEditorMessageBox messageBox = new GisEditorMessageBox(MessageBoxButton.OK);
                messageBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                messageBox.Owner = Application.Current.MainWindow;
                messageBox.Title = "Info";
                messageBox.Message = string.Format(CultureInfo.InvariantCulture, "A TypeLoadException is caught. This exception might because \"{0}\" plugin's version is not match this GIS Editor or some plugin(s) don’t implement the required APIs. Please update the plugins to match this version of GIS Editor. Remove those plugins would also remove this exception.", ex.Source);

                //Run run = new Run("contact ThinkGeo");
                //Hyperlink hyperlink = new Hyperlink(run);
                //hyperlink.Click += (s, e) =>
                //{
                //    messageBox.Close();
                //    GisEditor.LoggerManager.Log(LoggerLevel.Error, message, ex);
                //};
                //messageBox.Inlines.Add(tempMessage1);
                //messageBox.Inlines.Add(hyperlink);
                //messageBox.Inlines.Add(tempMessage2);

                StringBuilder sb = new StringBuilder();
                sb.Append(String.Format(CultureInfo.InvariantCulture, "Source: \t{0}\r\n", ex.Source));
                sb.Append(String.Format(CultureInfo.InvariantCulture, "Message: \t{0}\r\n", ex.Message));
                sb.Append(String.Format(CultureInfo.InvariantCulture, "Stack: \t{0}\r\n", ex.StackTrace));
                messageBox.ErrorMessage = sb.ToString();

                messageBox.ToggleButtonClickAction = new Action(() => 
                {
                    messageBox.Close();
                    GisEditor.LoggerManager.Log(LoggerLevel.Error, message, ex);
                });

                messageBox.ShowDialog();
            }

            GisEditor.LoggerManager.Log(LoggerLevel.Debug, message, ex);
        }
    }
}