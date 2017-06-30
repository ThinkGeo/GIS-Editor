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


using System.IO;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class FileGeoDatabaseDataSourceResolveTool : DataSourceResolveTool
    {
        public FileGeoDatabaseDataSourceResolveTool()
            : base()
        { }

        protected override bool CanResolveDataSourceCore
        {
            get { return true; }
        }

        protected override bool IsDataSourceAvailableCore(Layer layer)
        {
            FileGeoDatabaseFeatureLayer geoDBLayer = (FileGeoDatabaseFeatureLayer)layer;
            return Directory.Exists(geoDBLayer.PathName);
        }

        protected override void ResolveDataSourceCore(Layer layer)
        {
            FileGeoDatabaseFeatureLayer geoDBLayer = (FileGeoDatabaseFeatureLayer)layer;
            FolderHelper.OpenFolderBrowserDialog((tmpDialog, tmpResult) =>
            {
                if (tmpResult == System.Windows.Forms.DialogResult.OK)
                {
                    geoDBLayer.PathName = tmpDialog.SelectedPath;
                }
            }, tmpDialog =>
            {
                tmpDialog.Description = GisEditor.LanguageManager.GetStringResource("FileGeoSelectFolderLabel");
            });
        }
    }
}
