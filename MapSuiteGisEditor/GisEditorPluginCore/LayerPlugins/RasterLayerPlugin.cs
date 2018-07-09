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
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public abstract class RasterLayerPlugin : LayerPlugin
    {
        [Obfuscation]
        private bool requireWorldFile;

        public RasterLayerPlugin()
        {
        }

        public bool RequireWorldFile
        {
            get { return requireWorldFile; }
            protected set { requireWorldFile = value; }
        }

        protected override Type GetLayerTypeCore()
        {
            return null;
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return null;
        }

        protected override Collection<Layer> GetLayersCore(GetLayersParameters getLayersParameters)
        {
            Collection<Layer> layers = base.GetLayersCore(getLayersParameters);

            foreach (var uri in getLayersParameters.LayerUris)
            {
                RasterLayer layer = GetRasterLayer(uri);

                if (layer != null)
                {
                    layer.UpperThreshold = double.MaxValue;
                    layer.LowerThreshold = 0d;
                    layer.Name = Path.GetFileNameWithoutExtension(uri.LocalPath);

                    layer.Open();
                    if (!layer.HasProjectionText)
                    {
                        string proj4PathFileName = Path.ChangeExtension(uri.LocalPath, "prj");
                        string proj4Parameter = LayerPluginHelper.GetProj4ProjectionParameter(proj4PathFileName);
                        if (!string.IsNullOrEmpty(proj4Parameter))
                            layer.InitializeProj4Projection(proj4Parameter);
                    }
                    else
                    {
                        string proj = layer.GetProjectionText().Trim();
                        if (!string.IsNullOrEmpty(proj))
                            layer.InitializeProj4Projection(proj);
                    }

                    layer.Close();

                    layers.Add(layer);
                }
            }

            return layers;
        }

        protected abstract RasterLayer GetRasterLayer(Uri uri);

        private static string GetWorldFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            string newExtension = String.Concat(extension.Remove(extension.Length - 2, 1), 'w');
            return Path.ChangeExtension(fileName, newExtension);
        }

        private static void WarningToSave(RectangleShape bbox, int width, int height, string worldFileName)
        {
            var result = System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("Jp2RasterFileLayerPluginJp2WorldFileText"), GisEditor.LanguageManager.GetStringResource("GdiPlusRasterFileLayerPluginCreateWorldCaption"), System.Windows.Forms.MessageBoxButtons.YesNo);
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                WorldFile worldFile = new WorldFile(bbox, width, height);
                StringBuilder sBuilder = new StringBuilder();
                sBuilder.AppendLine(worldFile.HorizontalResolution.ToString(CultureInfo.InvariantCulture));
                sBuilder.AppendLine(worldFile.RotationRow.ToString(CultureInfo.InvariantCulture));
                sBuilder.AppendLine(worldFile.RotationColumn.ToString(CultureInfo.InvariantCulture));
                sBuilder.AppendLine(worldFile.VerticalResolution.ToString(CultureInfo.InvariantCulture));
                sBuilder.AppendLine(worldFile.UpperLeftX.ToString(CultureInfo.InvariantCulture));
                sBuilder.AppendLine(worldFile.UpperLeftY.ToString(CultureInfo.InvariantCulture));

                File.WriteAllText(worldFileName, sBuilder.ToString());
            }
        }
    }
}