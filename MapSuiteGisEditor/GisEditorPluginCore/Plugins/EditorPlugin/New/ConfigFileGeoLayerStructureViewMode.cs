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
using System.IO;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class ConfigFileGeoLayerStructureViewMode : ConfigLayerStructureViewModel
    {
        public ConfigFileGeoLayerStructureViewMode(FeatureLayer featureLayer)
            : base(featureLayer)
        {
            LayerPlugin = new FileGeoDatabaseFeatureLayerPlugin();
            string gisEditorFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "MapSuiteGisEditor");
            string tempFolderPath = Path.Combine(gisEditorFolder, "Output");
            if (!Directory.Exists(tempFolderPath))
            {
                Directory.CreateDirectory(tempFolderPath);
            }
            LayerUri = new Uri(tempFolderPath);
        }

        public string GetInvalidMessage()
        {
            string message = string.Empty;
            if (string.IsNullOrEmpty(LayerName))
            {
                message = "Layer name can't be empty.";
            }
            if (!ValidateFileName(LayerName))
            {
                message = "A file name can't contain any of the following characters:" + Environment.NewLine + "\\ / : * ? \" < > |";
            }
            else if (string.IsNullOrEmpty(TableName))
            {
                message = "Table name can't be empty.";
            }
            else if (ColumnListItemSource.Count == 0)
            {
                message = "There must be at least one column.";
            }
            else if (!Directory.Exists(LayerUri.LocalPath))
            {
                message = "Folder path is invalid.";
            }
            return message;
        }

        private bool ValidateFileName(string fileName)
        {
            return !(fileName.Contains("\\")
                || fileName.Contains("/")
                || fileName.Contains(":")
                || fileName.Contains("*")
                || fileName.Contains("?")
                || fileName.Contains("\"")
                || fileName.Contains("<")
                || fileName.Contains(">")
                || fileName.Contains("|")
                );
        }
    }
}