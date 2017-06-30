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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public abstract class WizardShareObject : OutputUserControlViewModel
    {
        private string tempPath;
        private string description;

        protected WizardShareObject()
        {
            tempPath = "LongRunningTasks";
        }

        protected string TempPath
        {
            get { return tempPath; }
        }

        public TaskPlugin GetTaskPlugin()
        {
            var taskPlugin = GetTaskPluginCore();
            taskPlugin.Load();
            description = string.IsNullOrEmpty(taskPlugin.Description) ? taskPlugin.Name : taskPlugin.Description;
            if (taskPlugin != null)
            {
                using (MemoryStream pluginStream = new MemoryStream())
                {
                    BinaryFormatter serializer = new BinaryFormatter();
                    serializer.Serialize(pluginStream, taskPlugin);
                    pluginStream.Seek(0, SeekOrigin.Begin);
                    taskPlugin = serializer.Deserialize(pluginStream) as TaskPlugin;
                }
            }

            return taskPlugin;
        }

        protected virtual TaskPlugin GetTaskPluginCore()
        {
            return null;
        }

        public void LoadToMap(ExceptionInfo errorInfo)
        {
            GisEditorMessageBox messageBox = new GisEditorMessageBox(MessageBoxButton.YesNo);
            messageBox.Title = GisEditor.LanguageManager.GetStringResource("NavigatePluginAddToMap");
            messageBox.Message = String.Format(CultureInfo.InvariantCulture, "{0} is completed, do you want to add to map?", description);
            messageBox.ErrorMessage = errorInfo.Message;
            messageBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            messageBox.Owner = Application.Current.MainWindow;
            var result = messageBox.ShowDialog().Value;
            if (result)
            {
                LoadToMapCore();
            }
        }

        protected virtual void LoadToMapCore()
        { }
    }
}