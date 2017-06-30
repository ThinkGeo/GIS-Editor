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
using System.IO;
using System.Windows;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class ShapeFileDataSourceResolveTool : DataSourceResolveTool
    {
        public ShapeFileDataSourceResolveTool(string extensionFilter)
            : base(extensionFilter)
        { }

        protected override bool CanResolveDataSourceCore
        {
            get { return true; }
        }

        protected override bool IsDataSourceAvailableCore(Layer layer)
        {
            ShapeFileFeatureLayer shapeFileLayer = layer as ShapeFileFeatureLayer;
            bool isDataSourceAvailable = true;
            if (shapeFileLayer != null)
            {
                bool shpExsit = File.Exists(shapeFileLayer.ShapePathFilename);
                bool shxExsit = File.Exists(Path.ChangeExtension(shapeFileLayer.ShapePathFilename, "shx"));
                bool dbfExsit = File.Exists(Path.ChangeExtension(shapeFileLayer.ShapePathFilename, "dbf"));
                isDataSourceAvailable = shpExsit && shxExsit && dbfExsit;
            }
            return isDataSourceAvailable;
        }

        protected override void ResolveDataSourceCore(Layer layer)
        {
            ShapeFileFeatureLayer shapeFileLayer = layer as ShapeFileFeatureLayer;
            if (shapeFileLayer != null)
            {
                bool shpExsit = File.Exists(shapeFileLayer.ShapePathFilename);
                if (!shpExsit)
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Filter = ExtensionFilter;
                    if (openFileDialog.ShowDialog().GetValueOrDefault())
                    {
                        string fileName = openFileDialog.FileName;
                        shapeFileLayer.ShapePathFilename = fileName;
                    }
                }
                bool shxExsit = File.Exists(Path.ChangeExtension(shapeFileLayer.ShapePathFilename, "shx"));
                if (!shxExsit)
                {
                    MessageBox.Show("Cannot find SHX file, please check and try again.");
                }
                bool dbfExsit = File.Exists(Path.ChangeExtension(shapeFileLayer.ShapePathFilename, "dbf"));
                if (!dbfExsit)
                {
                    MessageBox.Show("Cannot find DBF file, please check and try again.");
                }
            }
        }
    }
}
