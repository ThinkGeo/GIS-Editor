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
using System.Linq;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class FileInfoExtension
    {
        public static bool IsInUse(this FileInfo file)
        {
            bool isInUseByotherProcesses = IsFileInUse(file);
            bool isLoadedInGisEditor = false;

            string[] layerFileExtensions = { "shp", "tif", "ecw", "jp2", "sid", "csv", "grd" };
            if (layerFileExtensions.Any(extension => file.Extension.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            {
                isLoadedInGisEditor = IsFileLoadedInEditor(file);
            }

            // it's being used by other processes or it's loaded in the gis editor
            return isInUseByotherProcesses || isLoadedInGisEditor;
        }

        private static bool IsFileInUse(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }

            //the file is unavailable because it is:
            //still being written to
            //or being processed by another thread
            //or does not exist (has already been processed)
            catch (IOException ioException)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ioException.Message, new ExceptionInfo(ioException));
                return true;
            }
            catch (UnauthorizedAccessException unauthorizedAccessException)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, unauthorizedAccessException.Message, new ExceptionInfo(unauthorizedAccessException));
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        private static bool IsFileLoadedInEditor(FileInfo file)
        {
            bool result = false;

            if (GisEditor.ActiveMap != null)
            {
                var layerOverlays = GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>().ToArray();

                var shpLayers = layerOverlays.SelectMany(layerOverlay => layerOverlay.Layers.OfType<ShapeFileFeatureLayer>());
                bool loadedAsShp = shpLayers.Any(shpLayer => shpLayer.ShapePathFilename == file.FullName);

                var csvLayers = layerOverlays.SelectMany(layerOverlay => layerOverlay.Layers.OfType<CsvFeatureLayer>());
                bool loadedAsCsv = csvLayers.Any(csvLayer => csvLayer.DelimitedPathFilename == file.FullName);

                //these layers all have a property called PathFilename 
                //that indicartes the path of the corresponding file
                var otherLayers = layerOverlays.SelectMany(layerOverlay => layerOverlay.Layers.Where(layer =>
                {
                    return layer is NativeImageRasterLayer ||
                        layer is MrSidRasterLayer ||
                        layer is EcwRasterLayer ||
                        layer is GeoTiffRasterLayer ||
                        layer is Jpeg2000RasterLayer;
                }));
                bool loaded = false;
                foreach (var item in otherLayers)
                {
                    var fileName = item.GetType().GetProperty("PathFilename").GetValue(item, null).ToString();
                    if (fileName.Equals(file.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        loaded = true;
                        break;
                    }
                }
                result = loadedAsCsv || loadedAsShp || loaded;
            }

            return result;
        }
    }
}
