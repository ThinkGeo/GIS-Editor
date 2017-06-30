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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.GisEditor.Plugins.Properties;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class GisEditorWpfMapExtension
    {
        private static Collection<string> shapeFileInBuildingIndex;
        private static Dictionary<GisEditorWpfMap, bool> isFirstAddedLayers;
        private const string overlayNamePattern = "(?<=Layer Group) \\d+";

        static GisEditorWpfMapExtension()
        {
            shapeFileInBuildingIndex = new Collection<string>();
            isFirstAddedLayers = new Dictionary<GisEditorWpfMap, bool>();
        }

        public static Collection<string> FeatureLayersInBuildingIndex
        {
            get
            {
                return shapeFileInBuildingIndex;
            }
        }

        public static Dictionary<string, string> GetUnitDictionary()
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();

            AddAbbrDistanceStrings(DistanceUnit.Feet, dic);
            AddAbbrDistanceStrings(DistanceUnit.Kilometer, dic);
            AddAbbrDistanceStrings(DistanceUnit.Meter, dic);
            AddAbbrDistanceStrings(DistanceUnit.Mile, dic);
            AddAbbrDistanceStrings(DistanceUnit.NauticalMile, dic);
            AddAbbrDistanceStrings(DistanceUnit.UsSurveyFeet, dic);
            AddAbbrDistanceStrings(DistanceUnit.Yard, dic);

            return dic;
        }

        private static void AddAbbrDistanceStrings(DistanceUnit distanceUnit, Dictionary<string, string> distanceAbbrs)
        {
            string key = String.Format(CultureInfo.InvariantCulture, " {0}", distanceUnit.ToString());
            string value = String.Format(CultureInfo.InvariantCulture, " {0}", GisEditorWpfMapExtension.GetAbbreviateDistanceUnit(distanceUnit));
            distanceAbbrs[key] = value;
        }

        //from http://englishplus.com/grammar/00000058.htm
        public static string GetAbbreviateDistanceUnit(DistanceUnit distanceUnit)
        {
            switch (distanceUnit)
            {
                case DistanceUnit.Meter:
                    return "m";
                case DistanceUnit.Feet:
                    return "ft.";
                case DistanceUnit.Kilometer:
                    return "km";
                case DistanceUnit.Mile:
                    return "mi.";
                case DistanceUnit.UsSurveyFeet:
                    return "us-ft.";
                case DistanceUnit.Yard:
                    return "yd.";
                case DistanceUnit.NauticalMile:
                    return "nmi.";
                default:
                    return "unkown.";
            }
        }

        public static void OpenInGoogleEarth(this GisEditorWpfMap map, string googleEarthInstalledPath, params FeatureLayer[] featureLayers)
        {
            string tempFileName = Path.ChangeExtension(Path.GetTempFileName(), ".kmz");
            SaveFeatureLayersToKMZ(featureLayers, tempFileName);

            GoogleEarthHelper.OpenKmlFileWithGoogleEarth(tempFileName, googleEarthInstalledPath, () =>
            {
                if (MessageBox.Show(GoogleEarthResources.NoneGoogleEarthInstalled, GoogleEarthResources.NoneGoogleEarthInstalledCaption, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    System.Windows.Forms.SaveFileDialog sf = new System.Windows.Forms.SaveFileDialog();
                    sf.Filter = "(*.kmz)|*.kmz";
                    sf.FileName = "KmlExportFile" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    if (sf.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        File.Copy(tempFileName, Path.ChangeExtension(sf.FileName, ".kmz"));
                        File.Delete(tempFileName);
                    }
                }
            }, () =>
            {
                if (MessageBox.Show(GoogleEarthResources.NoneGoogleEarthExecutableFile, GoogleEarthResources.NoneGoogleEarthExecutableFileCaption, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    System.Windows.Forms.SaveFileDialog sf = new System.Windows.Forms.SaveFileDialog();
                    sf.Filter = "(*.kmz)|*.kmz";
                    sf.FileName = "KmlExportFile" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    if (sf.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        File.Copy(tempFileName, Path.ChangeExtension(sf.FileName, ".kmz"));
                        File.Delete(tempFileName);
                    }
                }
            });
        }

        private static void SaveFeatureLayersToKMZ(FeatureLayer[] featureLayers, string path)
        {
            PlatformGeoCanvas canvas = new PlatformGeoCanvas();
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap((int)GisEditor.ActiveMap.ActualWidth, (int)GisEditor.ActiveMap.ActualHeight);
            Proj4Projection proj = new Proj4Projection();
            proj.InternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
            proj.ExternalProjectionParametersString = Proj4Projection.GetDecimalDegreesParametersString();
            proj.Open();

            canvas.BeginDrawing(bitmap, proj.ConvertToExternalProjection(GisEditor.ActiveMap.CurrentExtent), GeographyUnit.DecimalDegree);
            featureLayers.ForEach(l =>
            {
                Proj4ProjectionInfo projectionInfo = l.GetProj4ProjectionInfo();
                if (projectionInfo != null)
                {
                    projectionInfo.ExternalProjectionParametersString = Proj4Projection.GetWgs84ParametersString();
                    projectionInfo.SyncProjectionParametersString();
                }

                l.Open();
                l.Draw(canvas, new Collection<SimpleCandidate>());
            });
            canvas.EndDrawing();


            string kmlPath = Path.ChangeExtension(path, ".kml");
            string pngPath = Path.ChangeExtension(path, ".png");

            if (File.Exists(kmlPath))
            {
                File.Delete(kmlPath);
            }
            if (File.Exists(pngPath))
            {
                File.Delete(pngPath);
            }

            MemoryStream stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            byte[] bitmapArray = stream.GetBuffer();
            StringBuilder builder = GetKMLStringBuilder(proj.ConvertToExternalProjection(GisEditor.ActiveMap.CurrentExtent), Path.GetFileName(pngPath));

            File.WriteAllText(kmlPath, builder.ToString());
            File.WriteAllBytes(pngPath, bitmapArray);

            var zipFileAdapter = ZipFileAdapterManager.CreateInstance();
            zipFileAdapter.AddFileToZipFile(kmlPath, "");
            zipFileAdapter.AddFileToZipFile(pngPath, "");
            zipFileAdapter.Save(Path.ChangeExtension(path, ".kmz"));
            File.Delete(kmlPath);
            File.Delete(pngPath);
        }

        public static void SaveAsVectorKML(this GisEditorWpfMap map)
        {
            IEnumerable<FeatureLayer> featureLayers = GisEditor.ActiveMap.GetFeatureLayers(true).Select(l =>
            {
                FeatureLayer featureLayer = null;
                lock (l)
                {
                    if (l.IsOpen) l.Close();
                    featureLayer = (FeatureLayer)l.CloneDeep();
                    l.Open();
                }
                return featureLayer;
            });

            KmlParameter parameter = GoogleEarthHelper.GetKmlParameter();
            if (parameter != null)
            {
                if (Path.GetExtension(parameter.PathFileName).ToUpper() == ".KML")
                {
                    GoogleEarthHelper.SaveKmlData(featureLayers, parameter);
                }
                else if (Path.GetExtension(parameter.PathFileName).ToUpper() == ".KMZ")
                {
                    SaveFeatureLayersToKMZ(featureLayers.ToArray(), parameter.PathFileName);

                    //StringBuilder builder = GetKmlDataBuilder(featureLayers);
                    //GdiPlusGeoCanvas canvas = new GdiPlusGeoCanvas();
                    //System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap((int)GisEditor.ActiveMap.ActualWidth, (int)GisEditor.ActiveMap.ActualHeight);
                    //Proj4Projection proj = new Proj4Projection();
                    //proj.InternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
                    //proj.ExternalProjectionParametersString = Proj4Projection.GetDecimalDegreesParametersString();
                    //proj.Open();

                    //canvas.BeginDrawing(bitmap, proj.ConvertToExternalProjection(GisEditor.ActiveMap.CurrentExtent), GeographyUnit.DecimalDegree);
                    //featureLayers.ForEach(l =>
                    //{
                    //    Proj4ProjectionInfo projectionInfo = l.GetProj4ProjectionInfo();
                    //    if (projectionInfo != null)
                    //    {
                    //        projectionInfo.ExternalProjectionParametersString = Proj4Projection.GetWgs84ParametersString();
                    //        projectionInfo.SyncProjectionParametersString();
                    //    }

                    //    l.Open();
                    //    l.Draw(canvas, new Collection<SimpleCandidate>());
                    //});
                    //canvas.EndDrawing();
                    //string kmlPath = Path.ChangeExtension(parameter.PathFileName, ".kml");
                    //string pngPath = Path.ChangeExtension(parameter.PathFileName, ".png");

                    //if (File.Exists(kmlPath))
                    //{
                    //    File.Delete(kmlPath);
                    //}
                    //if (File.Exists(pngPath))
                    //{
                    //    File.Delete(pngPath);
                    //}

                    //MemoryStream stream = new MemoryStream();
                    //bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    //byte[] bitmapArray = stream.GetBuffer();

                    //File.WriteAllText(kmlPath, builder.ToString());
                    //File.WriteAllBytes(pngPath, bitmapArray);

                    //var zipFileAdapter = ZipFileAdapterManager.CreateInstance();
                    //zipFileAdapter.AddFileToZipFile(kmlPath, "");
                    //zipFileAdapter.AddFileToZipFile(pngPath, "");
                    //zipFileAdapter.Save(Path.ChangeExtension(parameter.PathFileName, ".kmz"));
                    //File.Delete(kmlPath);
                    //File.Delete(pngPath);
                }

                if (File.Exists(parameter.PathFileName))
                {
                    AskToOpenGE(parameter.PathFileName);
                }
            }
        }

        private static StringBuilder GetKmlDataBuilder(IEnumerable<FeatureLayer> featureLayers)
        {
            StringBuilder builder = new StringBuilder();
            KmlGeoCanvas kmlCanvas = new KmlGeoCanvas();

            Proj4Projection proj = new Proj4Projection();
            proj.InternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
            proj.ExternalProjectionParametersString = Proj4Projection.GetDecimalDegreesParametersString();
            proj.Open();

            kmlCanvas.BeginDrawing(builder, proj.ConvertToExternalProjection(GisEditor.ActiveMap.CurrentExtent), GeographyUnit.DecimalDegree);
            featureLayers.ForEach(l =>
            {
                Proj4ProjectionInfo projectionInfo = l.GetProj4ProjectionInfo();
                if (projectionInfo != null)
                {
                    projectionInfo.ExternalProjectionParametersString = Proj4Projection.GetWgs84ParametersString();
                    projectionInfo.SyncProjectionParametersString();
                }

                l.Open();
                l.Draw(kmlCanvas, new Collection<SimpleCandidate>());
            });
            kmlCanvas.EndDrawing();
            return builder;
        }

        public static void SaveAsRasterKML(this GisEditorWpfMap map)
        {
            IEnumerable<FeatureLayer> featureLayers = GisEditor.ActiveMap.GetFeatureLayers(true).Select(l =>
            {
                FeatureLayer featureLayer = null;
                lock (l)
                {
                    if (l.IsOpen) l.Close();
                    featureLayer = (FeatureLayer)l.CloneDeep();
                    l.Open();
                }
                return featureLayer;
            });
            System.Windows.Forms.SaveFileDialog sf = new System.Windows.Forms.SaveFileDialog();
            sf.Filter = "(*.kmz)|*.kmz";
            sf.FileName = "KmzExportFile" + DateTime.Now.ToString("yyyyMMddHHmmss");
            if (sf.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PlatformGeoCanvas canvas = new PlatformGeoCanvas();
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap((int)GisEditor.ActiveMap.ActualWidth, (int)GisEditor.ActiveMap.ActualHeight);
                Proj4Projection proj = new Proj4Projection();
                proj.InternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
                proj.ExternalProjectionParametersString = Proj4Projection.GetDecimalDegreesParametersString();
                proj.Open();

                canvas.BeginDrawing(bitmap, proj.ConvertToExternalProjection(GisEditor.ActiveMap.CurrentExtent), GeographyUnit.DecimalDegree);
                featureLayers.ForEach(l =>
                {
                    Proj4ProjectionInfo projectionInfo = l.GetProj4ProjectionInfo();
                    if (projectionInfo != null)
                    {
                        projectionInfo.ExternalProjectionParametersString = Proj4Projection.GetWgs84ParametersString();
                        projectionInfo.SyncProjectionParametersString();
                    }

                    l.Open();
                    l.Draw(canvas, new Collection<SimpleCandidate>());
                });
                canvas.EndDrawing();
                string kmlPath = Path.ChangeExtension(sf.FileName, ".kml");
                string pngPath = Path.ChangeExtension(sf.FileName, ".png");

                if (File.Exists(kmlPath))
                {
                    File.Delete(kmlPath);
                }
                if (File.Exists(pngPath))
                {
                    File.Delete(pngPath);
                }

                MemoryStream stream = new MemoryStream();
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                byte[] bitmapArray = stream.GetBuffer();
                StringBuilder builder = GetKMLStringBuilder(proj.ConvertToExternalProjection(GisEditor.ActiveMap.CurrentExtent), Path.GetFileName(pngPath));

                File.WriteAllText(kmlPath, builder.ToString());
                File.WriteAllBytes(pngPath, bitmapArray);

                var zipFileAdapter = ZipFileAdapterManager.CreateInstance();
                zipFileAdapter.AddFileToZipFile(kmlPath, "");
                zipFileAdapter.AddFileToZipFile(pngPath, "");
                zipFileAdapter.Save(Path.ChangeExtension(sf.FileName, ".kmz"));
                File.Delete(kmlPath);
                File.Delete(pngPath);

                AskToOpenGE(sf.FileName);
            }
        }

        private static void AskToOpenGE(string FilePath)
        {
            string mentionedString = "The KML file has been saved successfully, do you want to open it in Google Earth?";
            if (MessageBox.Show(mentionedString, "Open in Google Earth", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                string googleRegistryPath = @"SOFTWARE\Google\GoogleEarthPlugin";
                string googleRegistryKey = "";
                if (Environment.Is64BitOperatingSystem)
                {
                    googleRegistryPath = @"SOFTWARE\Wow6432Node\Google\GoogleEarthPlugin";
                }

                if (RegistryKeyExist(googleRegistryPath, googleRegistryKey))
                {
                    RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(googleRegistryPath);
                    string keyValue = registryKey.GetValue(googleRegistryKey, string.Empty).ToString();
                    string path = Directory.GetParent(keyValue).FullName + "\\client";
                    if (File.Exists(path + "\\googleearth.exe"))
                    {
                        RunProcess(path + "\\googleearth.exe", "\"" + FilePath + "\"");
                    }
                    else
                    {
                        MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GisEditorWpfMapExtensionNotExecutableText"), GoogleEarthResources.NoneGoogleEarthExecutableFileCaption);
                    }
                }
                else
                {
                    MessageBox.Show("\"Google Earth\" is not insatlled on local machine, please download at \"http://www.google.com/earth/download/ge/agree.html\"", GoogleEarthResources.NoneGoogleEarthInstalledCaption);
                }
            }
        }

        private static void RunProcess(string processFileName, string fileName)
        {
            Process.Start(processFileName, fileName);
        }

        private static bool RegistryKeyExist(string registryPath, string keyName)
        {
            bool exist = false;
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(registryPath);

            if (registryKey != null)
            {
                string keyValue = registryKey.GetValue(keyName, string.Empty).ToString();

                if (keyValue != string.Empty)
                {
                    exist = true;
                }
            }

            return exist;
        }

        private static StringBuilder GetKMLStringBuilder(RectangleShape boundingBox, string mapFilePath)
        {
            StringBuilder kmlStringBuilder = new StringBuilder();
            kmlStringBuilder.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            kmlStringBuilder.AppendLine(@"<kml xmlns=""http://www.opengis.net/kml/2.2"">");
            kmlStringBuilder.AppendLine(@"<GroundOverlay>");
            kmlStringBuilder.AppendLine(@"<color>ffffffff</color>");
            kmlStringBuilder.AppendLine(@"<Icon>");
            kmlStringBuilder.AppendFormat(@"<href>{0}</href>", mapFilePath);
            kmlStringBuilder.AppendLine(@"</Icon>");

            kmlStringBuilder.AppendLine(@"<LatLonBox>");

            kmlStringBuilder.AppendLine(string.Format("<north>{0}</north>", boundingBox.UpperLeftPoint.Y));
            kmlStringBuilder.AppendLine(string.Format("<south>{0}</south>", boundingBox.LowerRightPoint.Y));
            kmlStringBuilder.AppendLine(string.Format("<east>{0}</east>", boundingBox.LowerRightPoint.X));
            kmlStringBuilder.AppendLine(string.Format("<west>{0}</west>", boundingBox.UpperLeftPoint.X));

            kmlStringBuilder.AppendLine(@"</LatLonBox>");
            kmlStringBuilder.AppendLine(@"</GroundOverlay>");

            kmlStringBuilder.AppendLine(@"</kml>");

            return kmlStringBuilder;
        }

        public static Collection<LayerOverlay> FindLayerOverlayContaining(this GisEditorWpfMap map, FeatureLayer featureLayer)
        {
            Collection<LayerOverlay> results = new Collection<LayerOverlay>();
            foreach (LayerOverlay overlay in map.Overlays.OfType<LayerOverlay>())
            {
                if (overlay.Layers.OfType<FeatureLayer>().Any<FeatureLayer>(
                    layer => layer == featureLayer))
                {
                    results.Add(overlay);
                    continue;
                }
            }
            return results;
        }

        public static Collection<LayerOverlay> FindLayerOverlayContaining(this GisEditorWpfMap map, string featureLayerName)
        {
            Collection<LayerOverlay> results = new Collection<LayerOverlay>();
            foreach (LayerOverlay overlay in map.Overlays.OfType<LayerOverlay>())
            {
                if (overlay.Layers.OfType<FeatureLayer>().Any<FeatureLayer>(
                    layer => layer.Name == featureLayerName))
                {
                    results.Add(overlay);
                    continue;
                }
            }
            return results;
        }

        public static System.Windows.Controls.Image GetPrintingImage(this GisEditorWpfMap map, int imageWidth, int imageHeight)
        {
            var bitmap = map.GetBitmap(imageWidth, imageHeight, MapResizeMode.PreserveScaleAndCenter);
            MemoryStream stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();

            System.Windows.Controls.Image image = new System.Windows.Controls.Image { Source = bitmapImage };
            image.Measure(new System.Windows.Size(imageWidth, imageHeight));
            image.Arrange(new Rect(0, 0, imageWidth, imageHeight));
            return image;
        }

        /// <summary>
        /// This method returns a bitmap of all the layers on the map.
        /// </summary>
        /// <returns>A bitmap that is rendered by current layers on the map.</returns>
        public static System.Drawing.Bitmap GetBitmap(this GisEditorWpfMap map)
        {
            return map.GetBitmap((int)map.ActualWidth, (int)map.ActualHeight, MapResizeMode.PreserveScale);
        }

        /// <summary>
        /// This method returns a bitmap of all the layers on the map.
        /// </summary>
        /// <param name="newScreenWidth">A float value indicates the screen width for the bitmap.</param>
        /// <param name="newScreenHeight">A float value indicates the screen height for the bitmap.</param>
        /// <returns>A bitmap that is rendered by current layers on the map.</returns>
        public static System.Drawing.Bitmap GetBitmap(this GisEditorWpfMap map, int screenWidth, int screenHeight)
        {
            return map.GetBitmap(screenWidth, screenHeight, MapResizeMode.PreserveScale);
        }

        /// <summary>
        /// This method returns a bitmap of all the layers on the map.
        /// </summary>
        /// <param name="newScreenWidth">A float value indicates the screen width for the bitmap.</param>
        /// <param name="newScreenHeight">A float value indicates the screen height for the bitmap.</param>
        /// <param name="mapResizeMode">A mode that to define how to calculate the new next by the passed screen width/height.</param>
        /// <returns>A bitmap that is rendered by current layers on the map.</returns>
        public static System.Drawing.Bitmap GetBitmap(this GisEditorWpfMap map, int newScreenWidth, int newScreenHeight, MapResizeMode mapResizeMode)
        {
            double currentResovl = map.CurrentResolution;
            double newWorldWidth = currentResovl * (double)newScreenWidth;
            double newWorldHeight = currentResovl * (double)newScreenHeight;
            RectangleShape outputExtent = map.CurrentExtent;
            if (mapResizeMode == MapResizeMode.PreserveScale)
            {
                PointShape newUpperLeftPoint = (PointShape)map.CurrentExtent.UpperLeftPoint.CloneDeep();
                PointShape newLowerRightPoint = new PointShape(newUpperLeftPoint.X + newWorldWidth, newUpperLeftPoint.Y - newWorldHeight);
                outputExtent = new RectangleShape(newUpperLeftPoint, newLowerRightPoint);
            }
            else if (mapResizeMode == MapResizeMode.PreserveScaleAndCenter)
            {
                PointShape newCenter = map.CurrentExtent.GetCenterPoint();
                PointShape newUpperLeftPoint = new PointShape(newCenter.X - newWorldWidth * .5, newCenter.Y + newWorldHeight * .5);
                PointShape newLowerRightPoint = new PointShape(newCenter.X + newWorldWidth * .5, newCenter.Y - newWorldHeight * .5);
                outputExtent = new RectangleShape(newUpperLeftPoint, newLowerRightPoint);
            }

            return GetBitmap(map, newScreenWidth, newScreenHeight, outputExtent);
        }

        public static System.Drawing.Bitmap GetBitmap(this GisEditorWpfMap map, int newScreenWidth, int newScreenHeight, RectangleShape outputExtent)
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(newScreenWidth, newScreenHeight);
            PlatformGeoCanvas geoCanvas = new PlatformGeoCanvas();
            geoCanvas.BeginDrawing(bitmap, outputExtent, map.MapUnit);
            foreach (Overlay overlay in map.Overlays)
            {
                if (overlay is LayerOverlay) map.DrawOverlay((LayerOverlay)overlay, geoCanvas);
                else if (overlay is OpenStreetMapOverlay) map.DrawOverlay((OpenStreetMapOverlay)overlay, geoCanvas);
                else if (overlay is WorldMapKitMapOverlay) map.DrawOverlay((WorldMapKitMapOverlay)overlay, geoCanvas);
                else if (overlay is WmsOverlay) map.DrawOverlay((WmsOverlay)overlay, geoCanvas, map.ActualWidth, map.ActualHeight);
                else if (overlay is BingMapsOverlay) map.DrawOverlay((BingMapsOverlay)overlay, geoCanvas);
            }

            geoCanvas.EndDrawing();
            return bitmap;
        }

        public static void DrawOverlay(this GisEditorWpfMap map, WmsOverlay wmsOverlay, GeoCanvas geoCanvas, double actualWidth, double actualHeight)
        {
            if (wmsOverlay.ServerUris.Count > 0)
            {
                Uri uri = wmsOverlay.GetRequestUris(geoCanvas.CurrentWorldExtent).FirstOrDefault();
                if (uri != null)
                {
                    UriBuilder uriBuilder = new UriBuilder(uri);
                    string query = uri.Query;
                    query = query.Replace("width=" + actualWidth, "width=" + geoCanvas.Width);
                    query = query.Replace("height=" + actualHeight, "width=" + geoCanvas.Height);
                    uriBuilder.Query = query;

                    System.Net.WebRequest webRequest = System.Net.HttpWebRequest.Create(uriBuilder.Uri);
                    webRequest.Proxy = wmsOverlay.WebProxy;
                    if (wmsOverlay.TimeoutInSeconds > 0) webRequest.Timeout = wmsOverlay.TimeoutInSeconds * 1000;

                    System.Net.WebResponse webResponse = (System.Net.HttpWebResponse)webRequest.GetResponse();
                    System.Drawing.Bitmap wmsBitmap = null;
                    GeoImage geoImage = null;

                    try
                    {
                        Stream stream = webResponse.GetResponseStream();
                        wmsBitmap = new System.Drawing.Bitmap(stream);
                        MemoryStream imageStream = new MemoryStream();
                        wmsBitmap.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
                        geoImage = new GeoImage(imageStream);
                        geoCanvas.DrawScreenImage(geoImage,
                            geoCanvas.Width * .5f,
                            geoCanvas.Height * .5f,
                            geoCanvas.Width,
                            geoCanvas.Height, DrawingLevel.LevelOne, 0f, 0f, 0f);
                    }
                    catch (Exception ex)
                    {
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    }
                    finally
                    {
                        if (webResponse != null) { webResponse.Close(); webResponse = null; }
                        if (wmsBitmap != null) { wmsBitmap.Dispose(); wmsBitmap = null; }
                        if (geoImage != null) { geoImage.Dispose(); geoImage = null; }
                    }
                }
            }
        }

        public static void DrawOverlay(this GisEditorWpfMap map, BingMapsOverlay bingOverlay, GeoCanvas geoCanvas)
        {
            BingMapsLayer bingLayer = new BingMapsLayer(bingOverlay.ApplicationId, (Layers.BingMapsMapType)bingOverlay.MapType);
            bingLayer.DrawingExceptionMode = DrawingExceptionMode.DrawException;
            bingLayer.Proxy = bingOverlay.Proxy;
            bingLayer.ProjectionFromSphericalMercator = bingOverlay.ProjectionFromSphericalMercator;
            bingLayer.TimeoutInSeconds = bingOverlay.TimeoutInSeconds;
            bingLayer.SafeProcess(() =>
            {
                bingLayer.Draw(geoCanvas, new Collection<SimpleCandidate>());
            });
        }

        public static void DrawOverlay(this GisEditorWpfMap map, WorldMapKitMapOverlay worldMapKitWmsWpfOverlay, GeoCanvas geoCanvas)
        {
            WorldMapKitLayer wmkLayer = new WorldMapKitLayer(worldMapKitWmsWpfOverlay.ClientId, worldMapKitWmsWpfOverlay.PrivateKey);

            wmkLayer.DrawingExceptionMode = worldMapKitWmsWpfOverlay.DrawingExceptionMode;
            wmkLayer.LowerThreshold = 1;
            wmkLayer.UpperThreshold = double.MaxValue;
            wmkLayer.WebProxy = worldMapKitWmsWpfOverlay.WebProxy;
            wmkLayer.Projection = worldMapKitWmsWpfOverlay.Projection;
            wmkLayer.TimeoutInSecond = worldMapKitWmsWpfOverlay.TimeoutInSeconds;
            wmkLayer.SafeProcess(() =>
            {
                wmkLayer.Draw(geoCanvas, new Collection<SimpleCandidate>());
            });
        }

        public static void DrawOverlay(this GisEditorWpfMap map, LayerOverlay layerOverlay, GeoCanvas geoCanvas)
        {
            Collection<SimpleCandidate> simpleCandidates = new Collection<SimpleCandidate>();
            foreach (Layer layer in layerOverlay.Layers)
            {
                lock (layer)
                {
                    layer.SafeProcess(() =>
                    {
                        layer.Draw(geoCanvas, simpleCandidates);
                    });
                }
            }
        }

        public static void DrawOverlay(this GisEditorWpfMap map, OpenStreetMapOverlay osmOverlay, GeoCanvas geoCanvas)
        {
            OpenStreetMapLayer osmLayer = new OpenStreetMapLayer(osmOverlay.WebProxy);
            osmLayer.SafeProcess(() =>
            {
                osmLayer.Draw(geoCanvas, new Collection<SimpleCandidate>());
            });
        }

        public static void DisableInteractiveOverlays(this GisEditorWpfMap map)
        {
            map.DisableInteractiveOverlaysExclude(new Collection<InteractiveOverlay>());
        }

        public static void DisableInteractiveOverlaysExclude(this GisEditorWpfMap map, InteractiveOverlay overlayToExclude)
        {
            Collection<InteractiveOverlay> interactiveOverlaysToExclude = new Collection<InteractiveOverlay>();
            if (overlayToExclude != null) interactiveOverlaysToExclude.Add(overlayToExclude);

            map.DisableInteractiveOverlaysExclude(interactiveOverlaysToExclude);
        }

        public static void DisableInteractiveOverlaysExclude(this GisEditorWpfMap map, IEnumerable<InteractiveOverlay> overlaysToExclude)
        {
            var overlaysNeedDisactive = map.InteractiveOverlays.Except(overlaysToExclude);
            foreach (var overlay in overlaysNeedDisactive)
            {
                overlay.Disable();
            }
        }

        public static void ReprojectMap(GisEditorWpfMap map, string oldParameters, string newParameters)
        {
            ReprojectWMKOverlays(map, oldParameters, newParameters);
            ReprojectWMSOverlays(map, oldParameters, newParameters);
            ReprojectWMSRasterLayers(map, oldParameters, newParameters);
            ReprojectFeatureLayers(map, oldParameters, newParameters);
            ReprojectRasterLayers(map, oldParameters, newParameters);
            //ReprojectGraticuleLayer(map, oldParameters, newParameters);
            ReprojectMeasureOverlay(map, oldParameters, newParameters);
            ReprojectSelectOverlay(map, oldParameters, newParameters);
            UpdateUnitAndExtent(map, oldParameters, newParameters);
        }

        private static void ReprojectRasterLayers(GisEditorWpfMap map, string oldParameters, string newParameters)
        {
            List<RasterLayer> rasterLayers = map.Overlays.OfType<LayerOverlay>().SelectMany(o => o.Layers).OfType<RasterLayer>().Where(l => !(l is WmsRasterLayer)).ToList();
            foreach (RasterLayer rasterLayer in rasterLayers)
            {
                if (rasterLayer.ImageSource.Projection == null)
                {
                    //TODO: we need consider the old parameters here. Raster layer has different world file, 
                    // which might contains original projection.
                    string internalProjectionString = oldParameters;
                    rasterLayer.SafeProcess(() =>
                    {
                        if (rasterLayer.HasProjectionText)
                        {
                            internalProjectionString = rasterLayer.GetProjectionText();
                        }
                    });
                    Proj4Projection projection = new Proj4Projection();
                    projection.InternalProjectionParametersString = internalProjectionString;
                    projection.ExternalProjectionParametersString = newParameters;
                    rasterLayer.ImageSource.Projection = projection;
                    rasterLayer.ImageSource.Projection.Open();
                }
                else
                {
                    Proj4ProjectionInfo projectionInfo = Proj4ProjectionInfo.CreateInstance(rasterLayer.ImageSource.Projection);
                    if (projectionInfo != null)
                    {
                        projectionInfo.ExternalProjectionParametersString = newParameters;
                        rasterLayer.ImageSource.Projection = projectionInfo.Projection;
                        if (rasterLayer.IsOpen)
                        {
                            rasterLayer.ImageSource.Projection.Close();
                            rasterLayer.ImageSource.Projection.Open();
                        }
                    }
                }
            }
        }

        private static void ReprojectSelectOverlay(GisEditorWpfMap map, string oldParameters, string newParameters)
        {
            var selectOverlay = map.InteractiveOverlays.OfType<SelectionTrackInteractiveOverlay>().FirstOrDefault();
            if (selectOverlay != null)
            {
                Proj4Projection managedProj4Projection = new Proj4Projection(oldParameters, newParameters);
                managedProj4Projection.Open();
                var newFeatures = new Collection<Feature>();
                var originalFeatures = selectOverlay.HighlightFeatureLayer.InternalFeatures.ToArray();
                selectOverlay.HighlightFeatureLayer.InternalFeatures.Clear();
                foreach (var feature in originalFeatures)
                {
                    var newFeature = managedProj4Projection.ConvertToExternalProjection(feature);
                    selectOverlay.HighlightFeatureLayer.InternalFeatures.Add(newFeature);
                }

                selectOverlay.HighlightFeatureLayer.BuildIndex();
                managedProj4Projection.Close();
            }
        }

        private static void ReprojectMeasureOverlay(GisEditorWpfMap map, string oldParameters, string newParameters)
        {
            var measureOverlay = map.InteractiveOverlays.OfType<MeasureTrackInteractiveOverlay>().FirstOrDefault();
            if (measureOverlay != null)
            {
                Proj4Projection managedProj4Projection = new Proj4Projection(oldParameters, newParameters);
                managedProj4Projection.Open();
                var newFeatures = new Collection<Feature>();
                foreach (var mapShape in measureOverlay.ShapeLayer.MapShapes)
                {
                    var newFeature = managedProj4Projection.ConvertToExternalProjection(mapShape.Value.Feature);
                    mapShape.Value.Feature = newFeature;
                }
                managedProj4Projection.Close();
            }
        }

        public static bool AddStyleToLayerWithStyleWizard(IEnumerable<Layer> layers, bool replaceStyle = false)
        {
            bool addedStyle = false;
            var newLayers = layers.ToArray();
            foreach (var tmpLayer in newLayers)
            {
                var shapeFileFeatureLayer = tmpLayer as FeatureLayer;
                if (shapeFileFeatureLayer != null
                    && newLayers.Length == 1)
                {
                    var styleWizardWindow = GisEditor.ControlManager.GetUI<StyleWizardWindow>();
                    styleWizardWindow.StyleCategories = StylePluginHelper.GetStyleCategoriesByFeatureLayer(shapeFileFeatureLayer);
                    styleWizardWindow.StyleCategories = styleWizardWindow.StyleCategories ^ StyleCategories.Composite;
                    styleWizardWindow.StyleCategories = styleWizardWindow.StyleCategories ^ StyleCategories.Label;

                    if ((styleWizardWindow as System.Windows.Window).ShowDialog().GetValueOrDefault())
                    {
                        if (styleWizardWindow.StyleWizardResult.StylePlugin != null)
                        {
                            if (GisEditor.ActiveMap != null) GisEditor.ActiveMap.ActiveLayer = shapeFileFeatureLayer;

                            StyleBuilderArguments arguments = new StyleBuilderArguments();
                            arguments.FeatureLayer = shapeFileFeatureLayer;
                            arguments.AvailableStyleCategories = StylePluginHelper.GetStyleCategoriesByFeatureLayer(shapeFileFeatureLayer);
                            StylePlugin styleProvider = styleWizardWindow.StyleWizardResult.StylePlugin;

                            arguments.AppliedCallback = new Action<StyleBuilderResult>(args =>
                            {
                                if (args.CompositeStyle != null)
                                {
                                    if (replaceStyle)
                                    {
                                        foreach (var customZoomLevel in shapeFileFeatureLayer.ZoomLevelSet.CustomZoomLevels)
                                        {
                                            customZoomLevel.CustomStyles.Clear();
                                        }
                                    }
                                    AddNewStyleToLayer(shapeFileFeatureLayer, args.CompositeStyle, args.FromZoomLevelIndex, args.ToZoomLevelIndex);
                                }
                            });

                            var newStyle = styleProvider.GetDefaultStyle();
                            newStyle.Name = styleProvider.Name;
                            CompositeStyle componentStyle = new CompositeStyle();
                            componentStyle.Name = shapeFileFeatureLayer.Name;
                            componentStyle.Styles.Add(newStyle);
                            arguments.StyleToEdit = componentStyle;

                            arguments.FillRequiredColumnNames();
                            var styleResult = GisEditor.StyleManager.EditStyle(arguments);
                            if (!styleResult.Canceled)
                            {
                                componentStyle = (CompositeStyle)styleResult.CompositeStyle;
                                arguments.AppliedCallback(styleResult);
                                addedStyle = true;
                            }
                        }
                        else
                        {
                            StyleWizardViewModel viewModel = styleWizardWindow.DataContext as StyleWizardViewModel;
                            if (viewModel != null && viewModel.TargetObject != null && viewModel.TargetObject.SelectedStyleCategory is LibraryStyleCheckableItemModel)
                            {
                                StyleLibraryWindow styleLibraryWindow = new StyleLibraryWindow();
                                if (styleLibraryWindow.ShowDialog().Value)
                                {
                                    if (styleLibraryWindow.Result != null && !styleLibraryWindow.Result.Canceled)
                                    {
                                        if (replaceStyle)
                                        {
                                            foreach (var customZoomLevel in shapeFileFeatureLayer.ZoomLevelSet.CustomZoomLevels)
                                            {
                                                customZoomLevel.CustomStyles.Clear();
                                            }
                                        }
                                        AddNewStyleToLayer(shapeFileFeatureLayer
                                            , styleLibraryWindow.Result.CompositeStyle
                                            , styleLibraryWindow.Result.FromZoomLevelIndex
                                            , styleLibraryWindow.Result.ToZoomLevelIndex);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return addedStyle;
        }

        public static void AddLayersBySettings(this GisEditorWpfMap map, IEnumerable<Layer> layers
            , bool useStyleWizard = false)
        {
            AddLayersParameters parameters = new AddLayersParameters();
            foreach (var layer in layers)
            {
                parameters.LayersToAdd.Add(layer);
            }

            if (useStyleWizard && GisEditor.StyleManager.UseWizard)
            {
                parameters.LayersAdded = (p) => AddStyleToLayerWithStyleWizard(p.LayersToAdd, true);
            }

            Layer layerToAdd = parameters.LayersToAdd.FirstOrDefault();
            string proj4ProjectionParameters = layerToAdd.GetInternalProj4ProjectionParameters();
            if (string.IsNullOrEmpty(proj4ProjectionParameters))
            {
                string description = GisEditor.LanguageManager.GetStringResource("selectAProjectionForAllLayersDescription");
                ProjectionWindow projectionWindow = new ProjectionWindow(GisEditor.ActiveMap.DisplayProjectionParameters
                    , description
                    , "");
                if (projectionWindow.ShowDialog().GetValueOrDefault())
                {
                    proj4ProjectionParameters = projectionWindow.Proj4ProjectionParameters;
                }
            }
            parameters.Proj4ProjectionParameters = proj4ProjectionParameters;

            GisEditor.ActiveMap.AddLayersToActiveOverlay(parameters);
        }

        public static Collection<FeatureLayer> GetFeatureLayers(this GisEditorWpfMap map, SimpleShapeType shapeType)
        {
            return map.GetFeatureLayers(true, shapeType);
        }

        public static Collection<FeatureLayer> GetFeatureLayers(this GisEditorWpfMap map, bool visibleLayersOnly, SimpleShapeType shapeType)
        {
            Collection<FeatureLayer> result = new Collection<FeatureLayer>();
            Collection<FeatureLayer> allFeatureLayers = map.GetFeatureLayers(visibleLayersOnly);

            if (shapeType == SimpleShapeType.Unknown) return allFeatureLayers;

            allFeatureLayers.Distinct().ForEach(l =>
            {
                if (GisEditor.LayerManager.GetFeatureSimpleShapeType(l) == shapeType) result.Add(l);
            });
            return result;
        }

        private static void AddNewStylesToLayer(FeatureLayer featureLayer, IEnumerable<Styles.Style> styles)
        {
            featureLayer.DrawingQuality = DrawingQuality.CanvasSettings;
            if (!styles.All(s => s is TextStyle))
            {
                featureLayer.ZoomLevelSet.CustomZoomLevels.ForEach(z => z.CustomStyles.Clear());
            }

            foreach (var style in styles)
            {
                AddNewStyleToLayer(featureLayer, style, 1, GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Count);
            }
        }

        private static void AddNewStyleToLayer(FeatureLayer featureLayer, Styles.Style style, int from, int to)
        {
            for (int i = 0; i < GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Count; i++)
            {
                var tmpZoomLevel = featureLayer.ZoomLevelSet.CustomZoomLevels[i];
                if (i >= from - 1 && i <= to - 1)
                {
                    tmpZoomLevel.CustomStyles.Add(style);
                }
            }
        }

        private static string GetProj4ProjectionParameterFromLayer(Layer layer)
        {
            string proj4Parameter = string.Empty;

            if (layer != null)
            {
                RasterLayer rasterLayer = layer as RasterLayer;
                FeatureLayer featureLayer = layer as FeatureLayer;
                if (rasterLayer != null)
                {
                    proj4Parameter = rasterLayer.GetInternalProj4ProjectionParameter();
                    if (string.IsNullOrEmpty(proj4Parameter)) proj4Parameter = Proj4Projection.GetEpsgParametersString(4326);
                }
                else if (featureLayer != null)
                {
                    Proj4Projection projection = (featureLayer.FeatureSource.Projection) as Proj4Projection;
                    if (projection == null)
                    {
                        string projection4326String = Proj4Projection.GetEpsgParametersString(4326);
                        projection = new Proj4Projection(projection4326String, projection4326String);
                        projection.SyncProjectionParametersString();
                        featureLayer.FeatureSource.Projection = projection;
                        projection.Open();
                    }

                    proj4Parameter = projection.InternalProjectionParametersString;
                }
            }

            return proj4Parameter;
        }

        private static void RefreshCachesAndZoomToExtent(GisEditorWpfMap map, RectangleShape worldExtent, bool needToRedraw, bool useCache, bool zoomToExtentOfNewlyAddedLayer)
        {
            if (map.ActiveOverlay != null)
            {
                if (map.ActiveOverlay is TileOverlay)
                {
                    TileOverlay tileOverlay = (TileOverlay)map.ActiveOverlay;
                    if (useCache) tileOverlay.RefreshCache(RefreshCacheMode.ApplyNewCache);
                    else tileOverlay.RefreshCache(RefreshCacheMode.DisableCache);
                }

                if (worldExtent != null && (isFirstAddedLayers[map] || zoomToExtentOfNewlyAddedLayer))
                {
                    map.CurrentExtent = worldExtent;
                }

                if (isFirstAddedLayers[map] || needToRedraw)
                {
                    map.Refresh();
                }
            }
        }

        private static string GetLayerOverlayName(GisEditorWpfMap map)
        {
            string overlayName = "Layer Group {0}";
            int maxValue = 0;
            foreach (var layerOverlay in map.Overlays.OfType<LayerOverlay>())
            {
                string match = Regex.Match(layerOverlay.Name, overlayNamePattern).Value;
                int currentValue = 0;
                if (!string.IsNullOrEmpty(match) && int.TryParse(match, out currentValue))
                {
                    maxValue = maxValue > currentValue ? maxValue : currentValue;
                }
            }
            return string.Format(CultureInfo.InvariantCulture, overlayName, maxValue + 1);
        }

        private static void UpdateUnitAndExtent(GisEditorWpfMap map, string oldParameters, string newParameters)
        {
            double currentScale = map.CurrentScale;
            PointShape currentCenter = map.CurrentExtent.GetCenterPoint();

            if (map.DisplayProjectionParameters != null)
            {
                var mainProj4 = new Proj4Projection();
                mainProj4.InternalProjectionParametersString = oldParameters;
                mainProj4.ExternalProjectionParametersString = newParameters;
                mainProj4.SyncProjectionParametersString();
                mainProj4.Open();

                try
                {
                    currentCenter = mainProj4.ConvertToExternalProjection(currentCenter) as PointShape;
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
            }

            //map.DisplayProjectionParameters = proj4Parameters;
            GeographyUnit targetUnit = GisEditorWpfMap.GetGeographyUnit(newParameters);
            if (map.MapUnit != targetUnit) map.MapUnit = targetUnit;
            map.CurrentExtent = MapHelper.CalculateExtent(currentCenter, currentScale, targetUnit, map.ActualWidth, map.ActualHeight);
            map.RefreshForce();
        }

        //private static void ReprojectGraticuleLayer(GisEditorWpfMap map, string oldParameters, string newParameters)
        //{
        //    GraticuleAdornmentLayer graticuleLayer = map.AdornmentOverlay.Layers.OfType<GraticuleAdornmentLayer>().FirstOrDefault();
        //    if (graticuleLayer != null)
        //    {
        //        Proj4Projection projection = new Proj4Projection(oldParameters, newParameters);
        //        projection.Open();
        //        graticuleLayer.Projection = projection;
        //    }
        //}

        private static void ReprojectFeatureLayers(GisEditorWpfMap map, string oldParameters, string newParameters)
        {
            foreach (var item in map.GetFeatureLayers())
            {
                ReprojectFeatureLayer(newParameters, oldParameters, item);
            }
        }

        private static void ReprojectWMKOverlays(WpfMap map, string oldParameters, string newParameters)
        {
            var wmk = (from overlay in map.Overlays.OfType<WorldMapKitMapOverlay>()
                       select overlay).FirstOrDefault();
            if (wmk != null)
            {
                GeographyUnit unit = GisEditorWpfMap.GetGeographyUnit(newParameters);
                switch (unit)
                {
                    case GeographyUnit.DecimalDegree:
                        if (wmk.Projection != Layers.WorldMapKitProjection.DecimalDegrees)
                            wmk.Projection = Layers.WorldMapKitProjection.DecimalDegrees;
                        break;

                    case GeographyUnit.Meter:
                        if (wmk.Projection != Layers.WorldMapKitProjection.SphericalMercator)
                            wmk.Projection = Layers.WorldMapKitProjection.SphericalMercator;
                        break;
                }
                wmk.DrawingExceptionMode = DrawingExceptionMode.DrawException;

                wmk.RefreshCache();
            }
        }

        private static void ReprojectWMSOverlays(GisEditorWpfMap map, string oldParameters, string newParameters)
        {
            var allWMSOverlays = from overlay in map.Overlays.OfType<WmsOverlay>()
                                 select overlay;

            foreach (var wmsOverlay in allWMSOverlays)
            {
                var uri = wmsOverlay.ServerUris.FirstOrDefault();
                if (uri != null)
                {
                    WmsRasterLayer wmsRasterLayer = new WmsRasterLayer(uri);

                    Collection<string> serverCrss = new Collection<string>();
                    wmsRasterLayer.SafeProcess(() =>
                    {
                        serverCrss = wmsRasterLayer.GetServerCrss();
                    });

                    foreach (var crs in serverCrss)
                    {
                        if (CrsProj4Equivalent(crs, newParameters))
                        {
                            wmsOverlay.Projection = crs;
                            break;
                        }
                    }
                }
            }
        }

        private static void ReprojectWMSRasterLayers(GisEditorWpfMap map, string oldParameters, string newParameters)
        {
            var allWMSRasterLayers = from overlay in map.Overlays.OfType<LayerOverlay>()
                                     from layer in overlay.Layers.OfType<WmsRasterLayer>()
                                     select layer;

            foreach (var wmsRasterLayer in allWMSRasterLayers)
            {
                Collection<string> serverCrss = new Collection<string>();
                wmsRasterLayer.SafeProcess(() =>
                {
                    serverCrss = wmsRasterLayer.GetServerCrss();
                });

                //wmsRasterLayer.Open();
                foreach (var crs in serverCrss)
                {
                    if (CrsProj4Equivalent(crs, newParameters))
                    {
                        wmsRasterLayer.Crs = crs;
                        break;
                    }
                }
            }
        }

        private static bool CrsProj4Equivalent(string crs, string proj4Parameters)
        {
            int index = crs.IndexOf(":");
            if (index != -1)
            {
                crs = crs.Substring(index + 1);
                int srid;
                int.TryParse(crs, out srid);

                string crsParameters = Proj4Projection.GetEpsgParametersString(srid).Replace(" ", string.Empty).Replace("+", " +").Trim();
                return !ManagedProj4ProjectionExtension.CanProject(crsParameters, proj4Parameters);
            }
            return false;
        }

        private static void ReprojectFeatureLayer(string targetProj4Parameters, string sourceProj4Projection, FeatureLayer layer)
        {
            if (layer.FeatureSource.Projection != null)
            {
                Proj4Projection originProj4 = (Proj4Projection)layer.FeatureSource.Projection;
                originProj4.ExternalProjectionParametersString = targetProj4Parameters;
                originProj4.SyncProjectionParametersString();
            }
            else
            {
                ShapeFileFeatureLayer shpLayer = layer as ShapeFileFeatureLayer;
                if (shpLayer != null)
                {
                    string prjPath = Path.GetDirectoryName(shpLayer.ShapePathFilename) + "\\" + Path.GetFileNameWithoutExtension(shpLayer.ShapePathFilename) + ".prj";
                    if (File.Exists(prjPath))
                    {
                        string originParameters = Proj4Projection.ConvertPrjToProj4(File.ReadAllText(prjPath));
                        var projection = new Proj4Projection(originParameters, targetProj4Parameters);
                        projection.SyncProjectionParametersString();
                        shpLayer.FeatureSource.Projection = projection;
                    }
                    else
                    {
                        var projection = new Proj4Projection(sourceProj4Projection, targetProj4Parameters);
                        projection.SyncProjectionParametersString();
                        shpLayer.FeatureSource.Projection = projection;
                    }
                }
                else
                {
                    var projection = new Proj4Projection(sourceProj4Projection, targetProj4Parameters);
                    projection.SyncProjectionParametersString();
                    layer.FeatureSource.Projection = projection;
                }
            }

            lock (layer)
            {
                layer.Close();
                layer.FeatureSource.Projection.Close();
                layer.Open();
            }
        }

        private static void RefreshForce(this GisEditorWpfMap map)
        {
            var mapArguments = map.GetMapArguments();
            map.Overlays.ForEach(o =>
            {
                var tileOverlay = o as TileOverlay;

                if (tileOverlay != null && tileOverlay.TileCache != null)
                {
                    tileOverlay.RefreshCache(RefreshCacheMode.ApplyNewCache);
                }

                o.MapArguments = mapArguments;
                o.Refresh();
            });

            map.InteractiveOverlays.ForEach(o =>
            {
                o.MapArguments = mapArguments;
                o.Refresh();
            });
        }

        public static void ShowIdentifyFeaturesWindow(this GisEditorWpfMap map, PointShape identifyPoint)
        {
            var offset = map.CurrentResolution * 4;
            var rectangle = new RectangleShape(identifyPoint.X - offset, identifyPoint.Y + offset, identifyPoint.X + offset, identifyPoint.Y - offset);

            ShowIdentifyFeaturesWindow(map, rectangle);
        }

        public static void ShowIdentifyFeaturesWindow(this GisEditorWpfMap map, IEnumerable<Feature> features, FeatureLayer featureLayer)
        {
            if (map.SelectionOverlay != null)
            {
                bool hasError = false;
                Dictionary<FeatureLayer, Collection<Feature>> featureGroups = new Dictionary<FeatureLayer, Collection<Feature>>();

                string errorMessage = string.Empty;
                if (!featureLayer.IsOpen) { featureLayer.Open(); }

                try
                {
                    var boundingBox = GisEditor.ActiveMap.CurrentExtent;
                    var screenWidth = GisEditor.ActiveMap.ActualWidth;

                    // Get features whose data have been filtered by filters.
                    ZoomLevel currentDrawingZoomLevel = featureLayer.ZoomLevelSet.GetZoomLevelForDrawing(boundingBox, screenWidth, map.MapUnit);
                    IEnumerable<string> filtersInStyle = GetFiltersInStyle(currentDrawingZoomLevel);
                    filtersInStyle = null;

                    if (CalculatedDbfColumn.CalculatedColumns.ContainsKey(featureLayer.FeatureSource.Id)
                        && CalculatedDbfColumn.CalculatedColumns[featureLayer.FeatureSource.Id].Count > 0)
                    {
                        CalculatedDbfColumn.UpdateCalculatedRecords(CalculatedDbfColumn.CalculatedColumns[featureLayer.FeatureSource.Id], features, GisEditor.ActiveMap.DisplayProjectionParameters);
                    }

                    features = FixedColumnValuesByColumnType(features, featureLayer);
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));

                    hasError = true;
                    if (ex.InnerException != null)
                    {
                        errorMessage = ex.InnerException.Message;
                    }
                    else
                    {
                        errorMessage = ex.Message;
                    }
                }

                IdentifyingFeaturesMapEventArgs e = new IdentifyingFeaturesMapEventArgs(new Collection<Feature>(features.ToList()), false);
                map.OnIdentifyingFeatures(e);
                if (e.Cancel)
                {
                    return;
                }

                //add found features to the collection
                if (features != null)
                {
                    Collection<Feature> distinctedFeatures = new Collection<Feature>();
                    foreach (var item in features)
                    {
                        if (!distinctedFeatures.Any(d => d.Id.Equals(item.Id)))
                        {
                            distinctedFeatures.Add(item);
                        }
                    }
                    if (!featureGroups.ContainsKey(featureLayer) && distinctedFeatures.Count > 0)
                    {
                        featureGroups[featureLayer] = new Collection<Feature>();
                    }

                    distinctedFeatures.ForEach((feature) =>
                    {
                        feature.Tag = featureLayer;
                        featureGroups[featureLayer].Add(feature);
                    });
                }

                map.SelectionOverlay.HighlightFeatureLayer.Open();

                if (featureGroups.Count > 0)
                {
                    map.SelectionOverlay.HighlightFeatureLayer.InternalFeatures.Clear();
                    map.SelectionOverlay.StandOutHighlightFeatureLayer.InternalFeatures.Clear();
                    lock (map.SelectionOverlay.HighlightFeatureLayer.FeatureSource)
                    {
                        if (!map.SelectionOverlay.HighlightFeatureLayer.FeatureSource.IsInTransaction)
                        {
                            map.SelectionOverlay.HighlightFeatureLayer.FeatureSource.BeginTransaction();
                        }

                        foreach (var item in featureGroups)
                        {
                            foreach (var tmpFeature in item.Value)
                            {
                                Feature newFeature = map.SelectionOverlay.CreateHighlightFeature(tmpFeature, item.Key);
                                if (newFeature.IsValid())
                                {
                                    map.SelectionOverlay.HighlightFeatureLayer.EditTools.Add(newFeature);
                                }
                            }
                        }

                        map.SelectionOverlay.HighlightFeatureLayer.FeatureSource.CommitTransaction();
                    }

                    IdentifiedFeaturesMapEventArgs identifiedArgs = new IdentifiedFeaturesMapEventArgs(map.SelectionOverlay.HighlightFeatureLayer.InternalFeatures);
                    map.OnIdentifiedFeatures(identifiedArgs);

                    if (map.SelectionOverlay.HighlightFeatureLayer.InternalFeatures.Count > 0)
                    {
                        map.SelectionOverlay.Refresh();
                    }

#if !GISEditorUnitTest
                    var identifyWindow = FeatureInfoWindow.Instance;
                    identifyWindow.Show(DockWindowPosition.Default);
                    identifyWindow.Refresh(featureGroups);
#endif
                }
#if !GISEditorUnitTest
                else
                {
                    if (hasError)
                    {
                        string message = GisEditor.LanguageManager.GetStringResource("GisEditorMapExtensionQueryFaildText");
                        if (!string.IsNullOrEmpty(errorMessage))
                        {
                            message += string.Format(CultureInfo.InvariantCulture, "\r\n{0}", errorMessage);
                        }
                        System.Windows.Forms.MessageBox.Show(message, "Failed", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GisEditorMapExtensionNoFeaturesText"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxInfoCaption"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                    }
                }
#endif
            }
        }

        public static void ShowIdentifyFeaturesWindow(this GisEditorWpfMap map, RectangleShape identifyRectangle)
        {
            if (map.SelectionOverlay != null)
            {
                bool hasError = false;
                Dictionary<FeatureLayer, Collection<Feature>> featureGroups = new Dictionary<FeatureLayer, Collection<Feature>>();

                string errorMessage = string.Empty;
                foreach (FeatureLayer featureLayer in map.SelectionOverlay.FilteredLayers)
                {
                    if (!featureLayer.IsOpen) { featureLayer.Open(); }

                    Collection<Feature> features = null;
                    try
                    {
                        var boundingBox = GisEditor.ActiveMap.CurrentExtent;
                        var screenWidth = GisEditor.ActiveMap.ActualWidth;

                        // Get features whose data have been filtered by filters.
                        //ZoomLevel currentDrawingZoomLevel = featureLayer.ZoomLevelSet.GetZoomLevelForDrawing(boundingBox, screenWidth, map.MapUnit);
                        //IEnumerable<string> filtersInStyle = GetFiltersInStyle(currentDrawingZoomLevel);
                        var resultFeatures = featureLayer.FeatureSource.SpatialQuery(identifyRectangle, QueryType.Intersects, featureLayer.GetDistinctColumnNames());
                        var visibleFeatures = resultFeatures.GetVisibleFeatures(featureLayer.ZoomLevelSet, boundingBox, screenWidth, map.MapUnit);

                        if (CalculatedDbfColumn.CalculatedColumns.ContainsKey(featureLayer.FeatureSource.Id)
                            && CalculatedDbfColumn.CalculatedColumns[featureLayer.FeatureSource.Id].Count > 0)
                        {
                            CalculatedDbfColumn.UpdateCalculatedRecords(CalculatedDbfColumn.CalculatedColumns[featureLayer.FeatureSource.Id], visibleFeatures, GisEditor.ActiveMap.DisplayProjectionParameters);
                        }

                        Collection<Feature> fixedFeatures = FixedColumnValuesByColumnType(visibleFeatures, featureLayer);
                        features = new Collection<Feature>(fixedFeatures.ToList());
                        if (features.Count == 0)
                        {
                            ShapeFileFeatureLayer shpLayer = featureLayer as ShapeFileFeatureLayer;
                            if (shpLayer != null && shpLayer.FeatureIdsToExclude.Count > 0)
                            {
                                List<string> tempIds = shpLayer.FeatureIdsToExclude.ToList();
                                shpLayer.FeatureIdsToExclude.Clear();
                                Collection<Feature> excludeFeatures = shpLayer.FeatureSource.GetFeaturesByIds(tempIds, ReturningColumnsType.AllColumns);
                                foreach (var item in excludeFeatures)
                                {
                                    bool isIntersect = item.GetShape().Intersects(identifyRectangle);
                                    if (isIntersect)
                                    {
                                        features.Add(item);
                                    }
                                }
                                foreach (var item in tempIds)
                                {
                                    shpLayer.FeatureIdsToExclude.Add(item);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                        if (features != null)
                        {
                            features.Clear();
                        }

                        hasError = true;
                        if (ex.InnerException != null)
                        {
                            errorMessage = ex.InnerException.Message;
                        }
                        else
                        {
                            errorMessage = ex.Message;
                        }
                    }

                    IdentifyingFeaturesMapEventArgs e = new IdentifyingFeaturesMapEventArgs(features, false);
                    map.OnIdentifyingFeatures(e);
                    if (e.Cancel)
                    {
                        return;
                    }

                    //add found features to the collection
                    if (features != null)
                    {
                        Collection<Feature> distinctedFeatures = new Collection<Feature>();
                        foreach (var item in features)
                        {
                            if (!distinctedFeatures.Any(d => d.Id.Equals(item.Id)))
                            {
                                distinctedFeatures.Add(item);
                            }
                        }
                        if (!featureGroups.ContainsKey(featureLayer) && distinctedFeatures.Count > 0)
                        {
                            featureGroups[featureLayer] = new Collection<Feature>();
                        }

                        distinctedFeatures.ForEach((feature) =>
                        {
                            feature.Tag = featureLayer;
                            featureGroups[featureLayer].Add(feature);
                            //selectedFeaturesEntities.Add(new HighlightedFeatureEntity(feature, featureLayer.Name));
                        });
                    }
                }

                if (map.FeatureLayerEditOverlay != null)
                {
                    map.FeatureLayerEditOverlay.EditShapesLayer.Open();
                    var visibleFeatures = map.FeatureLayerEditOverlay.EditShapesLayer.FeatureSource.SpatialQuery(identifyRectangle, QueryType.Intersects, map.FeatureLayerEditOverlay.EditShapesLayer.GetDistinctColumnNames());
                    if (map.FeatureLayerEditOverlay.EditTargetLayer != null && featureGroups.ContainsKey(map.FeatureLayerEditOverlay.EditTargetLayer))
                    {
                        foreach (var item in visibleFeatures)
                        {
                            if (!featureGroups[map.FeatureLayerEditOverlay.EditTargetLayer].Any(f => f.Id == item.Id))
                            {
                                featureGroups[map.FeatureLayerEditOverlay.EditTargetLayer].Add(item);
                            }
                        }
                    }
                    else if (visibleFeatures.Count > 0 && map.FeatureLayerEditOverlay.EditTargetLayer != null)
                    {
                        featureGroups[map.FeatureLayerEditOverlay.EditTargetLayer] = visibleFeatures;
                    }
                }

                map.SelectionOverlay.HighlightFeatureLayer.Open();

                if (featureGroups.Count > 0)
                {
                    map.SelectionOverlay.HighlightFeatureLayer.InternalFeatures.Clear();
                    map.SelectionOverlay.StandOutHighlightFeatureLayer.InternalFeatures.Clear();
                    lock (map.SelectionOverlay.HighlightFeatureLayer.FeatureSource)
                    {
                        if (!map.SelectionOverlay.HighlightFeatureLayer.FeatureSource.IsInTransaction)
                        {
                            map.SelectionOverlay.HighlightFeatureLayer.FeatureSource.BeginTransaction();
                        }

                        foreach (var item in featureGroups)
                        {
                            foreach (var tmpFeature in item.Value)
                            {
                                Feature newFeature = map.SelectionOverlay.CreateHighlightFeature(tmpFeature, item.Key);
                                if (newFeature.IsValid())
                                {
                                    //if (!newFeature.ColumnValues.ContainsKey("FeatureId"))
                                    //{
                                    //    string featureId = newFeature.Id;

                                    //    if (featureId.Contains(SelectionTrackInteractiveOverlay.FeatureIdSeparator))
                                    //    {
                                    //        featureId = featureId.Split(new string[] { SelectionTrackInteractiveOverlay.FeatureIdSeparator }, StringSplitOptions.RemoveEmptyEntries)[0];
                                    //    }

                                    //    newFeature.ColumnValues.Add("FeatureId", featureId);
                                    //}
                                    map.SelectionOverlay.HighlightFeatureLayer.EditTools.Add(newFeature);
                                }
                            }
                        }

                        //foreach (HighlightedFeatureEntity entity in selectedFeaturesEntities)
                        //{
                        //    Feature newFeature = map.SelectionOverlay.CreateHighlightFeature(entity.Feature, (FeatureLayer)entity.Feature.Tag);
                        //    if (newFeature.IsValid())
                        //    {
                        //        map.SelectionOverlay.HighlightFeatureLayer.EditTools.Add(newFeature);
                        //    }
                        //}

                        map.SelectionOverlay.HighlightFeatureLayer.FeatureSource.CommitTransaction();
                    }

                    IdentifiedFeaturesMapEventArgs identifiedArgs = new IdentifiedFeaturesMapEventArgs(map.SelectionOverlay.HighlightFeatureLayer.InternalFeatures);
                    map.OnIdentifiedFeatures(identifiedArgs);

                    if (map.SelectionOverlay.HighlightFeatureLayer.InternalFeatures.Count > 0)
                    {
                        map.SelectionOverlay.Refresh();
                    }

#if !GISEditorUnitTest
                    var identifyWindow = FeatureInfoWindow.Instance;
                    identifyWindow.Show(DockWindowPosition.Default);
                    identifyWindow.Refresh(featureGroups);
#endif
                }
#if !GISEditorUnitTest
                else
                {
                    if (hasError)
                    {
                        string message = GisEditor.LanguageManager.GetStringResource("GisEditorMapExtensionQueryFaildText");
                        if (!string.IsNullOrEmpty(errorMessage))
                        {
                            message += string.Format(CultureInfo.InvariantCulture, "\r\n{0}", errorMessage);
                        }
                        System.Windows.Forms.MessageBox.Show(message, "Failed", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GisEditorMapExtensionNoFeaturesText"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxInfoCaption"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                    }
                }
#endif
            }
        }

        private static IEnumerable<string> GetFiltersInStyle(ZoomLevel currentDrawingZoomLevel)
        {
            Collection<string> filters = null; // new Collection<string>();
            IEnumerable<Styles.Style> styles = GetStylesForDrawing(currentDrawingZoomLevel);

            string allFilters = string.Empty;
            foreach (var item in styles.SelectMany(s => s.Filters))
            {
                allFilters += string.Format(CultureInfo.InvariantCulture, " || ({0})", item);
            }
            allFilters = allFilters.Trim(' ', '|');

            if (!string.IsNullOrEmpty(allFilters))
            {
                filters = new Collection<string>();
                filters.Add(allFilters);
            }
            return filters;
        }

        private static IEnumerable<Styles.Style> GetStylesForDrawing(ZoomLevel zoomLevel)
        {
            if (zoomLevel.CustomStyles.Count > 0)
            {
                foreach (var item in zoomLevel.CustomStyles)
                {
                    yield return item;
                }
            }
            else
            {
                yield return zoomLevel.DefaultAreaStyle;
                yield return zoomLevel.DefaultLineStyle;
                yield return zoomLevel.DefaultPointStyle;
                yield return zoomLevel.DefaultTextStyle;
            }
        }

        // fixed T/F to True/False, double 1 to 1.00000000
        private static Collection<Feature> FixedColumnValuesByColumnType(IEnumerable<Feature> visibleFeatures, FeatureLayer featureLayer)
        {
            Collection<Feature> features = new Collection<Feature>();

            Collection<FeatureSourceColumn> columns = new Collection<FeatureSourceColumn>();
            featureLayer.SafeProcess(() =>
            {
                columns = featureLayer.FeatureSource.GetColumns(GettingColumnsType.All);
            });

            foreach (var feature in visibleFeatures)
            {
                Dictionary<string, string> columnValues = new Dictionary<string, string>();
                foreach (var item in feature.ColumnValues)
                {
                    FeatureSourceColumn column = columns.FirstOrDefault(c => c.ColumnName.Equals(item.Key));
                    if (column != null)
                    {
                        switch (column.TypeName)
                        {
                            case "Null":
                                columnValues[item.Key] = item.Value;
                                break;
                            case "String":
                                columnValues[item.Key] = item.Value;
                                break;
                            case "Double":
                                string doubleValue = item.Value;

                                if (doubleValue.Contains("."))
                                {
                                    string[] array = doubleValue.Split('.');
                                    if (array.Length == 2)
                                    {
                                        for (int i = 0; i < 8 - array[1].Length; i++)
                                        {
                                            doubleValue += "0";
                                        }
                                    }
                                }
                                else
                                {
                                    doubleValue += ".";
                                    for (int i = 0; i < 8; i++)
                                    {
                                        doubleValue += "0";
                                    }
                                }

                                columnValues[item.Key] = doubleValue;
                                break;
                            case "Logical":
                                string logicalValue = item.Value;

                                switch (logicalValue.ToUpperInvariant())
                                {
                                    case "T":
                                        logicalValue = true.ToString();
                                        break;
                                    case "F":
                                        logicalValue = false.ToString();
                                        break;
                                }

                                columnValues[item.Key] = logicalValue;
                                break;
                            case "Integer":
                                columnValues[item.Key] = item.Value;
                                break;
                            case "Memo":
                                columnValues[item.Key] = item.Value;
                                break;
                            case "Date":
                                columnValues[item.Key] = item.Value;
                                break;
                            case "DateTime":
                                columnValues[item.Key] = item.Value;
                                break;
                            case "IntegerInBinary":
                                columnValues[item.Key] = item.Value;
                                break;
                            case "DoubleInBinary":
                                columnValues[item.Key] = item.Value;
                                break;
                            case "Character":
                                columnValues[item.Key] = item.Value;
                                break;
                            case "Float":
                                string floatValue = item.Value;

                                if (floatValue.Contains("."))
                                {
                                    string[] array = floatValue.Split('.');
                                    if (array.Length == 2)
                                    {
                                        for (int i = 0; i < 8 - array[1].Length; i++)
                                        {
                                            floatValue += "0";
                                        }
                                    }
                                }
                                else
                                {
                                    floatValue += ".";
                                    for (int i = 0; i < 8; i++)
                                    {
                                        floatValue += "0";
                                    }
                                }

                                columnValues[item.Key] = floatValue;
                                break;
                            case "Numeric":
                                columnValues[item.Key] = item.Value;
                                break;
                            default:
                                columnValues[item.Key] = item.Value;
                                break;
                        }
                    }
                    else
                    {
                        columnValues[item.Key] = item.Value;
                    }
                }

                feature.ColumnValues.Clear();
                foreach (var item in columnValues)
                {
                    feature.ColumnValues[item.Key] = item.Value;
                }

                features.Add(feature);
            }

            return features;
        }

        private static Collection<Feature> GetNearestVisibleFeatures(PointShape identifyPoint, FeatureLayer featureLayer, GeographyUnit mapUnit, double resolution)
        {
            if (!featureLayer.IsOpen)
            {
                featureLayer.Open();
            }

            var searchTolerance = 12 * resolution;
            var searchBbox = new RectangleShape(identifyPoint.X - searchTolerance, identifyPoint.Y + searchTolerance, identifyPoint.X + searchTolerance, identifyPoint.Y - searchTolerance);
            var featuresInsideCurrentBoundingBox = featureLayer.QueryTools.GetFeaturesInsideBoundingBox(searchBbox, featureLayer.GetDistinctColumnNames());
            var boundingBox = GisEditor.ActiveMap.CurrentExtent;
            var screenWidth = GisEditor.ActiveMap.ActualWidth;
            var visibleFeatures = featuresInsideCurrentBoundingBox.GetVisibleFeatures(featureLayer.ZoomLevelSet, boundingBox, screenWidth, mapUnit);

            return new Collection<Feature>(visibleFeatures.ToList());
        }
    }
}