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
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.GisEditor.Plugins.Properties;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class GoogleEarthHelper
    {
        #region KML stuff

        private static string googleEarthRegistry = Environment.Is64BitOperatingSystem ? @"SOFTWARE\Wow6432Node\Google\" : @"SOFTWARE\Google\";
        private static string googleEarthProRegistryPath = googleEarthRegistry + "Google Earth Pro";
        private static string googleEarthProRegistryKey = "InstallLocation";
        private static string googleEarthRegistryPath = googleEarthRegistry + "GoogleEarthPlugin";
        private static string googleEarthRegistryKey = string.Empty;

        public static MenuItem GetSaveAsKMLMenuItem()
        {
            MenuItem item = new MenuItem();
            item.Header = GisEditor.LanguageManager.GetStringResource("GisEarthHelperGoogleEarth");
            item.Icon = new Image() { Width = 16, Height = 16, Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/googleEarth.png", UriKind.RelativeOrAbsolute)) };

            MenuItem rasterItem = new MenuItem();
            rasterItem.Click += new RoutedEventHandler(SaveAsRasterKMLClick);
            rasterItem.Icon = new Image() { Width = 16, Height = 16, Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/rasterkml.png", UriKind.RelativeOrAbsolute)) };
            rasterItem.Header = GisEditor.LanguageManager.GetStringResource("GisEarthHelperSaveasrasterKML");

            MenuItem vectorItem = new MenuItem();
            vectorItem.Header = GisEditor.LanguageManager.GetStringResource("GisEarthHelperSaveasvectorKML");
            vectorItem.Icon = new Image() { Width = 16, Height = 16, Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/vectorkml.png", UriKind.RelativeOrAbsolute)) };
            vectorItem.Click += new RoutedEventHandler(SaveAsVectorKMLClick);

            MenuItem ogeItem = new MenuItem();
            //ogeItem.IsEnabled = !string.IsNullOrEmpty(GetGoogleEarthInstalledPath());
            ogeItem.Tag = "GoogleEarth";
            ogeItem.Header = GisEditor.LanguageManager.GetStringResource("GisEarthHelperOpeninGoogleEarth");
            ogeItem.Icon = new Image() { Width = 16, Height = 16, Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/googleEarth.png", UriKind.RelativeOrAbsolute)) };
            ogeItem.Click += new RoutedEventHandler(OpenGoogleEarthItemClick);

            MenuItem ogeProItem = new MenuItem();
            //ogeProItem.IsEnabled = !string.IsNullOrEmpty(GetGoogleEarthProInstalledPath());
            ogeProItem.Tag = "GoogleEarthPro";
            ogeProItem.Header = "Open in Google Earth Pro";
            ogeProItem.Icon = new Image() { Width = 16, Height = 16, Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/googleEarth.png", UriKind.RelativeOrAbsolute)) };
            ogeProItem.Click += new RoutedEventHandler(OpenGoogleEarthItemClick);

            if (GisEditor.ActiveMap != null && GisEditor.ActiveMap.Overlays.Count == 0)
            {
                item.IsEnabled = false;
            }

            item.Items.Add(rasterItem);
            item.Items.Add(vectorItem);
            item.Items.Add(ogeItem);
            item.Items.Add(ogeProItem);

            return item;
        }

        public static void OpenWithGoogleMenuitemClick(object sender, RoutedEventArgs e)
        {
            Collection<InMemoryFeatureLayer> addedlayers = new Collection<InMemoryFeatureLayer>();

            var selectionOverlay = GisEditor.SelectionManager.GetSelectionOverlay();
            if (selectionOverlay.HighlightFeatureLayer.InternalFeatures.All(i => !GisEditor.ActiveMap.CurrentExtent.Intersects(i)))
            {
                ShowOptionsIfNoSelectedFeatureInCurrentExtent(selectionOverlay.HighlightFeatureLayer.InternalFeatures);
                return;
            }

            Dictionary<FeatureLayer, GeoCollection<Feature>> selectedFeaturesGroup = selectionOverlay.GetSelectedFeaturesGroup();
            foreach (var item in selectedFeaturesGroup)
            {
                if (item.Value.Count > 0)
                {
                    InMemoryFeatureLayer layer = new InMemoryFeatureLayer();
                    if (item.Key.FeatureSource.Projection != null)
                    {
                        bool isClosed = false;
                        if (item.Key.FeatureSource.Projection.IsOpen)
                        {
                            item.Key.FeatureSource.Projection.Close();
                            isClosed = true;
                        }
                        layer.FeatureSource.Projection = item.Key.FeatureSource.Projection.CloneDeep();
                        if (isClosed)
                        {
                            item.Key.FeatureSource.Projection.Open();
                        }
                    }
                    else
                    {
                        Proj4Projection proj4 = new Proj4Projection();
                        proj4.ExternalProjectionParametersString = Proj4Projection.GetDecimalDegreesParametersString();
                        proj4.ExternalProjectionParametersString = Proj4Projection.GetDecimalDegreesParametersString();
                        layer.FeatureSource.Projection = proj4;
                    }
                    foreach (var zoomLevel in item.Key.ZoomLevelSet.CustomZoomLevels)
                    {
                        layer.ZoomLevelSet.CustomZoomLevels.Add(zoomLevel);
                    }
                    layer.SafeProcess(() =>
                    {
                        layer.EditTools.BeginTransaction();
                        foreach (Feature feature in item.Value)
                        {
                            layer.EditTools.Add(feature);
                        }
                        layer.EditTools.CommitTransaction();
                    });

                    addedlayers.Add(layer);
                }
            }

            string installedPath = ((MenuItem)sender).Tag.Equals("GoogleEarth") ? GetGoogleEarthInstalledPath() : GetGoogleEarthProInstalledPath();
            GisEditor.ActiveMap.OpenInGoogleEarth(installedPath, addedlayers.ToArray());
        }

        public static KmlParameter GetKmlParameter()
        {
            KmlParameter parameter = null;
            KmlConfigureWindow window = new KmlConfigureWindow();
            window.Owner = Application.Current.MainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (window.ShowDialog().GetValueOrDefault())
            {
                parameter = window.KmlParameter;
            }
            return parameter;
        }

        public static void SaveToKmlMenuitemClick(object sender, RoutedEventArgs e)
        {
            Collection<Feature> features = GisEditor.SelectionManager.GetSelectedFeatures();

            if (features.All(i => !GisEditor.ActiveMap.CurrentExtent.Intersects(i)))
            {
                ShowOptionsIfNoSelectedFeatureInCurrentExtent(features);
                return;
            }

            Collection<FeatureLayer> featureLayers = new Collection<FeatureLayer>();
            var featuresGroup = GisEditor.SelectionManager.GetSelectionOverlay().GetSelectedFeaturesGroup();
            Proj4Projection tempProjection = new Proj4Projection();
            tempProjection.InternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
            tempProjection.ExternalProjectionParametersString = Proj4Projection.GetWgs84ParametersString();
            tempProjection.SyncProjectionParametersString();

            try
            {
                tempProjection.Open();
                foreach (var item in featuresGroup)
                {
                    if (item.Value.Count > 0)
                    {
                        InMemoryFeatureLayer layer = new InMemoryFeatureLayer();
                        ZoomLevelSet sourceZoomLevelSet = item.Key.ZoomLevelSet;
                        try
                        {
                            string tempXml = GisEditor.Serializer.Serialize(sourceZoomLevelSet);
                            ZoomLevelSet targetZoomLevelSet = (ZoomLevelSet)GisEditor.Serializer.Deserialize(tempXml);
                            layer.ZoomLevelSet = targetZoomLevelSet;

                            layer.Open();
                            layer.EditTools.BeginTransaction();
                            foreach (var feature in item.Value)
                            {
                                Feature newFeature = tempProjection.ConvertToExternalProjection(feature);
                                layer.EditTools.Add(newFeature);
                            }
                            layer.EditTools.CommitTransaction();

                            if (!item.Key.IsOpen) item.Key.Open();
                            foreach (var column in item.Key.QueryTools.GetColumns())
                            {
                                layer.Columns.Add(column);
                            }

                            featureLayers.Add(layer);
                        }
                        catch (Exception ex)
                        {
                            GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            finally
            {
                tempProjection.Close();
            }

            KmlParameter parameter = GetKmlParameter();
            //System.Windows.Forms.SaveFileDialog sf = new System.Windows.Forms.SaveFileDialog();
            //sf.Filter = "(*.kml)|*.kml|(*.kmz)|*.kmz";
            //sf.FileName = string.Format("{0}-{1}", "KmlExportFile", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
            if (parameter != null)
            {
                if (Path.GetExtension(parameter.PathFileName).ToUpper() == ".KML")
                {
                    SaveKmlData(featureLayers, parameter);
                }
                else if (Path.GetExtension(parameter.PathFileName).ToUpper() == ".KMZ")
                {
                    StringBuilder builder = GetKmlDataBuilder(featureLayers);
                    PlatformGeoCanvas canvas = new PlatformGeoCanvas();
                    System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap((int)GisEditor.ActiveMap.ActualWidth, (int)GisEditor.ActiveMap.ActualHeight);
                    RectangleShape drawingExtent = GetDrawingExtentInWgs84();
                    canvas.BeginDrawing(bitmap, drawingExtent, GeographyUnit.DecimalDegree);
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
                    string kmlPath = Path.ChangeExtension(parameter.PathFileName, ".kml");
                    string pngPath = Path.ChangeExtension(parameter.PathFileName, ".png");

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

                    File.WriteAllText(kmlPath, builder.ToString());
                    File.WriteAllBytes(pngPath, bitmapArray);

                    var zipFileAdapter = ZipFileAdapterManager.CreateInstance();
                    zipFileAdapter.AddFileToZipFile(kmlPath, "");
                    zipFileAdapter.AddFileToZipFile(pngPath, "");
                    zipFileAdapter.Save(Path.ChangeExtension(parameter.PathFileName, ".kmz"));
                    File.Delete(kmlPath);
                    File.Delete(pngPath);
                }

                string mentionedString = GisEditor.LanguageManager.GetStringResource("KMLFileHasSavedSuccessText");
                if (MessageBox.Show(mentionedString, "Open in Google Earth", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    string proInstalledPath = GetGoogleEarthProInstalledPath();
                    OpenKmlFileWithGoogleEarth(string.IsNullOrEmpty(proInstalledPath) ? GetGoogleEarthInstalledPath() : proInstalledPath
                        , parameter.PathFileName);
                }
            }
        }

        private static RectangleShape GetDrawingExtentInWgs84()
        {
            Proj4Projection proj = new Proj4Projection();
            proj.InternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
            proj.ExternalProjectionParametersString = Proj4Projection.GetDecimalDegreesParametersString();
            proj.Open();
            RectangleShape drawingExtent = proj.ConvertToExternalProjection(GisEditor.ActiveMap.CurrentExtent);
            proj.Close();

            return drawingExtent;
        }

        private static void ShowOptionsIfNoSelectedFeatureInCurrentExtent(Collection<Feature> features)
        {
            MessageBoxResult result = MessageBox.Show(string.Format(GisEditor.LanguageManager.GetStringResource("GoogleEarthAnyFeaturesSelected")), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxInfoCaption"), MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                GisEditor.ActiveMap.CurrentExtent = ExtentHelper.GetBoundingBoxOfItems(features);
                GisEditor.ActiveMap.Refresh();
            }
        }

        private static void SaveAsRasterKMLClick(object sender, RoutedEventArgs e)
        {
            GisEditor.ActiveMap.SaveAsRasterKML();
        }

        private static void SaveAsVectorKMLClick(object sender, RoutedEventArgs e)
        {
            GisEditor.ActiveMap.SaveAsVectorKML();
        }

        // We first save it as raster file to TEMP folder and then launch it
        private static void OpenGoogleEarthItemClick(object sender, RoutedEventArgs e)
        {
            List<FeatureLayer> featureLayers = GisEditor.ActiveMap.GetFeatureLayers(true).Select(l =>
            {
                FeatureLayer featureLayer = null;
                lock (l)
                {
                    if (l.IsOpen) l.Close();
                    featureLayer = (FeatureLayer)l.CloneDeep();
                    l.Open();
                }
                return featureLayer;
            }).ToList();

            AnnotationTrackInteractiveOverlay annotationOverlay = GisEditor.ActiveMap.InteractiveOverlays.OfType<AnnotationTrackInteractiveOverlay>().FirstOrDefault();
            if (annotationOverlay != null && annotationOverlay.TrackShapeLayer.InternalFeatures.Count > 0)
            {
                featureLayers.Add(annotationOverlay.TrackShapeLayer);
            }
            MeasureTrackInteractiveOverlay measureOverlay = GisEditor.ActiveMap.InteractiveOverlays.OfType<MeasureTrackInteractiveOverlay>().FirstOrDefault();
            if (measureOverlay != null && measureOverlay.ShapeLayer.MapShapes.Count > 0)
            {
                measureOverlay.TrackShapeLayer.Close();
                InMemoryFeatureLayer featureLayer = (InMemoryFeatureLayer)measureOverlay.TrackShapeLayer.CloneDeep();
                measureOverlay.TrackShapeLayer.Open();
                foreach (var item in measureOverlay.ShapeLayer.MapShapes)
                {
                    featureLayer.InternalFeatures.Add(item.Value.Feature);
                }
                featureLayers.Add(featureLayer);
            }

            string installedPath = ((MenuItem)sender).Tag.Equals("GoogleEarth") ? GetGoogleEarthInstalledPath() : GetGoogleEarthProInstalledPath();
            GisEditor.ActiveMap.OpenInGoogleEarth(installedPath, featureLayers.ToArray());
        }

        public static void OpenKmlFileWithGoogleEarth(string kmlPathFilename, string googleEarthInstalledPath, Action showGoogleEarthNotInstalledMessage = null, Action showGoogleEarthExecutableNotExistMessage = null)
        {
            if (string.IsNullOrEmpty(googleEarthInstalledPath))
            {
                //we prompt up a window let user select where the tool is. 
                ChooseGoogleEarthWindow window = new ChooseGoogleEarthWindow();
                window.Owner = Application.Current.MainWindow;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (window.ShowDialog().GetValueOrDefault())
                {
                    string path = window.GoogleEarthPath;
                    if (File.Exists(path))
                    {
                        RunProcess(path, "\"" + kmlPathFilename + "\"");
                    }
                    else
                    {
                        if (showGoogleEarthExecutableNotExistMessage != null) showGoogleEarthExecutableNotExistMessage();
                        else MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GoogleEarthIsNotExecutableFile"), GoogleEarthResources.NoneGoogleEarthExecutableFileCaption);
                    }
                }
            }
            else
            {
                if (File.Exists(googleEarthInstalledPath + "\\googleearth.exe"))
                {
                    RunProcess(googleEarthInstalledPath + "\\googleearth.exe", "\"" + kmlPathFilename + "\"");
                }
                else
                {
                    if (showGoogleEarthExecutableNotExistMessage != null) showGoogleEarthExecutableNotExistMessage();
                    else MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GoogleEarthIsNotExecutableFile"), GoogleEarthResources.NoneGoogleEarthExecutableFileCaption);
                }
            }
        }

        public static string GetGoogleEarthProInstalledPath()
        {
            var result = GetGoogleEarthProPath();
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            result = GetGoogleEarthPath();
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            return GetGoogleEarthInstalledPath(googleEarthProRegistryPath, googleEarthProRegistryKey);
        }

        public static string GetGoogleEarthInstalledPath()
        {
            var result = GetGoogleEarthPath();
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            result = GetGoogleEarthProPath();
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            return GetGoogleEarthInstalledPath(googleEarthRegistryPath, googleEarthRegistryKey);
        }

        private static string GetGoogleEarthProPath()
        {
            //Search for Google Earth Pro
            var programFilesX86Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var googleX86Path = Path.Combine(programFilesX86Path, "Google", "Google Earth Pro");
            var googlePath = Path.Combine(programFilesPath, "Google", "Google Earth Pro");
            List<string> paths = new List<string>();
            paths.Add(googleX86Path);
            paths.Add(googlePath);
            foreach (var item in paths)
            {
                if (Directory.Exists(item))
                {
                    try
                    {
                        var files = Directory.GetFiles(item, "googleearth.exe", SearchOption.AllDirectories);
                        if (files.Count() > 0)
                        {
                            return Path.GetDirectoryName(files[0]);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            return null;
        }

        private static string GetGoogleEarthPath()
        {
            //Search for Google Earth
            var programFilesX86Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var googleX86Path = Path.Combine(programFilesX86Path, "Google", "Google Earth");
            var googlePath = Path.Combine(programFilesPath, "Google", "Google Earth");
            List<string> paths = new List<string>();
            paths.Add(googleX86Path);
            paths.Add(googlePath);
            foreach (var item in paths)
            {
                if (Directory.Exists(item))
                {
                    try
                    {
                        var files = Directory.GetFiles(item, "googleearth.exe", SearchOption.AllDirectories);
                        if (files.Count() > 0)
                        {
                            return Path.GetDirectoryName(files[0]);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            return null;
        }

        private static string GetGoogleEarthInstalledPath(string registryPath, string key)
        {
            string result = null;

            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(registryPath);
            if (registryKey != null)
            {
                Console.WriteLine(registryKey);
                string keyValue = registryKey.GetValue(key, string.Empty).ToString();
                Console.WriteLine(keyValue);
                if (!string.IsNullOrEmpty(keyValue))
                {
                    result = new DirectoryInfo(keyValue).Parent.FullName + "\\client"; ;
                    Console.WriteLine(result);
                }
            }

            return result;
        }

        private static void RunProcess(string processFileName, string fileName)
        {
            Process.Start(processFileName, fileName);
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

        public static void SaveKmlData(IEnumerable<FeatureLayer> featureLayers, KmlParameter parameter)
        {
            StringBuilder builder = new StringBuilder();
            GeoCanvas kmlCanvas = new KmlGeoCanvas();
            if (parameter.Is3DKml)
            {
                kmlCanvas = new Kml3DGeoCanvas();
                ((Kml3DGeoCanvas)kmlCanvas).ZHeight = parameter.ZHeight;
            }
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
            using (StreamWriter sw = new StreamWriter(parameter.PathFileName))
            {
                sw.Write(builder.ToString());
                sw.Close();
            }
        }

        #endregion KML stuff
    }
}