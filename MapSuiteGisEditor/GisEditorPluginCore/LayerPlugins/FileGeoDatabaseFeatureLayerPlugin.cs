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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class FileGeoDatabaseFeatureLayerPlugin : FeatureLayerPlugin
    {
        public FileGeoDatabaseFeatureLayerPlugin()
            : base()
        {
            Name = GisEditor.LanguageManager.GetStringResource("FileGeoPluginName");
            ExtensionFilterCore = "FileGeodatabase file *.gdb|*.gdb";
            Description = GisEditor.LanguageManager.GetStringResource("FileGeoPluginDescription");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_fileicon_vector.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dataformats_fgdb.png", UriKind.RelativeOrAbsolute));
            Index = LayerPluginOrder.FileGeoDatabaseFeatureLayerPlugin;

            DataSourceResolveToolCore = new FileGeoDatabaseDataSourceResolveTool();
        }

        protected override bool CanPageFeaturesEfficientlyCore
        {
            get
            {
                return true;
            }
            set
            {
                base.CanPageFeaturesEfficientlyCore = value;
            }
        }

        protected override bool CanCreateFeatureLayerCore
        {
            get
            {
                return true;
            }
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(FileGeoDatabaseFeatureLayer);
        }

        protected override string GetInternalProj4ProjectionParametersCore(FeatureLayer featureLayer)
        {
            FileGeoDatabaseFeatureLayer layer = featureLayer as FileGeoDatabaseFeatureLayer;
            if (layer != null)
            {
                return layer.GetInternalProj4ProjectionParametersString();
            }
            return base.GetInternalProj4ProjectionParametersCore(featureLayer);
        }

        protected override ConfigureFeatureLayerParameters GetConfigureFeatureLayerParametersCore(FeatureLayer featureLayer)
        {
            CreateFeatureLayerUserControl userControl = new ConfigFileGeoDataBaseUserControl(featureLayer);
            GeneralWindow window = new GeneralWindow();
            window.Tag = userControl;
            window.OkButtonClicking += Window_OkButtonClicking;
            window.Title = GisEditor.LanguageManager.GetStringResource("FileGeoWindowTitle");
            window.Owner = Application.Current.MainWindow;
            window.HelpContainer.Content = HelpResourceHelper.GetHelpButton("CreateNewShapefileHelp", HelpButtonMode.NormalButton);
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ResizeMode = ResizeMode.NoResize;
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.ContentUI.Content = userControl;

            ConfigureFeatureLayerParameters parameters = null;
            if (window.ShowDialog().GetValueOrDefault())
            {
                parameters = userControl.GetFeatureLayerInfo();
            }
            return parameters;
        }

        private static void Window_OkButtonClicking(object sender, RoutedEventArgs e)
        {
            GeneralWindow window = (GeneralWindow)sender;
            CreateFeatureLayerUserControl userControl = (CreateFeatureLayerUserControl)window.Tag;
            string message = userControl.InvalidMessage;
            if (!string.IsNullOrEmpty(message))
            {
                MessageBox.Show(message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Handled = true;
            }
            ConfigureFeatureLayerParameters parameters = userControl.GetFeatureLayerInfo();
            if (Directory.Exists(parameters.LayerUri.LocalPath))
            {
                string name = Path.GetFileName(parameters.LayerUri.LocalPath);
                string existMessage = string.Format(CultureInfo.InvariantCulture, "The destination already has a file named {0}, do you want to replace the file in destination?", name);
                MessageBoxResult result = MessageBox.Show(existMessage, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    e.Handled = true;
                }
                else
                {
                    try
                    {
                        Directory.Delete(parameters.LayerUri.LocalPath, true);
                    }
                    catch (Exception ex)
                    {
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    }
                }
            }
        }

        //protected override OutputWindow GetExportFeatureLayerUICore()
        //{
        //    CreateFileGeodatabaseWindow outputWindow = new CreateFileGeodatabaseWindow(string.Empty);
        //    outputWindow.DefaultPrefix = "ExportResult";
        //    outputWindow.ExtensionFilter = "FileGeodatabase file *.gdb|*.gdb";
        //    outputWindow.Extension = ".gdb";
        //    outputWindow.OutputMode = OutputMode.ToTemporary;

        //    return outputWindow;
        //}

        protected override ConfigureFeatureLayerParameters GetCreateFeatureLayerParametersCore(IEnumerable<FeatureSourceColumn> columns)
        {
            CreateFileGeodatabaseWindow outputWindow = new CreateFileGeodatabaseWindow();
            outputWindow.OutputMode = OutputMode.ToFile;
            outputWindow.DefaultPrefix = "ExportResult";
            outputWindow.ExtensionFilter = "FileGeodatabase file *.gdb|*.gdb";
            outputWindow.Extension = ".gdb";
            outputWindow.OutputMode = OutputMode.ToTemporary;
            outputWindow.Proj4ProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
            outputWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            outputWindow.Owner = Application.Current.MainWindow;
            ConfigureFeatureLayerParameters configureFeatureLayerParameters = null;
            if (outputWindow.ShowDialog().GetValueOrDefault())
            {
                configureFeatureLayerParameters = new ConfigureFeatureLayerParameters(outputWindow.LayerUri);
                configureFeatureLayerParameters.Proj4ProjectionParametersString = outputWindow.Proj4ProjectionParametersString;
                foreach (var item in outputWindow.CustomData)
                {
                    configureFeatureLayerParameters.CustomData[item.Key] = item.Value;
                }
            }
            return configureFeatureLayerParameters;
        }

        protected override UserControl GetPropertiesUICore(Layer layer)
        {
            UserControl propertiesUserControl = base.GetPropertiesUICore(layer);

            LayerPluginHelper.SaveFeatureIDColumns((FeatureLayer)layer, propertiesUserControl);

            return propertiesUserControl;
        }

        protected override FeatureLayer CreateFeatureLayerCore(ConfigureFeatureLayerParameters featureLayerStructureParameters)
        {
            string layerPath = LayerPluginHelper.GetLayerUriToSave(featureLayerStructureParameters.LayerUri, ExtensionFilter);
            string fileName = Path.GetFileNameWithoutExtension(layerPath);
            if (string.IsNullOrEmpty(layerPath))
            {
                return null;
            }
            if (fileName.FirstOrDefault() != null && Char.IsNumber(fileName.FirstOrDefault()))
            {
                MessageBox.Show("Table name can not start with number.");
                return null;
            }
            featureLayerStructureParameters.LayerUri = new Uri(layerPath);

            Collection<FeatureSourceColumn> columns = new Collection<FeatureSourceColumn>();

            System.Text.RegularExpressions.Regex regx = new System.Text.RegularExpressions.Regex(@"\W");
            foreach (var column in featureLayerStructureParameters.AddedColumns)
            {
                FeatureSourceColumn newColumn = column;
                if (newColumn.ColumnName.Equals("Null", StringComparison.Ordinal))
                {
                    newColumn.ColumnName = "Null_";
                }
                newColumn.ColumnName = regx.Replace(newColumn.ColumnName, string.Empty);
                columns.Add(newColumn);
            }

            string tableName = string.Empty;

            if (featureLayerStructureParameters.CustomData.ContainsKey("TableName"))
            {
                //if (createFeatureLayerParameters.Tag != null)
                //tableName = createFeatureLayerParameters.Tag.ToString().Split('|').First();
                tableName = featureLayerStructureParameters.CustomData["TableName"] as string;
            }
            else
            {
                tableName = Path.GetFileNameWithoutExtension(featureLayerStructureParameters.LayerUri.OriginalString);
            }

            FileGeoDatabaseFeatureLayer.CreateFileGeoDatabase(featureLayerStructureParameters.LayerUri.OriginalString);
            FileGeoDatabaseFeatureLayer.CreateTable(featureLayerStructureParameters.LayerUri.OriginalString, tableName, featureLayerStructureParameters.WellKnownType, columns);

            string prjPath = Path.ChangeExtension(featureLayerStructureParameters.LayerUri.OriginalString, "prj");
            File.WriteAllText(prjPath, Proj4Projection.ConvertProj4ToPrj(featureLayerStructureParameters.Proj4ProjectionParametersString));

            FileGeoDatabaseFeatureLayer fileGeoDatabaseFeatureLayer = new FileGeoDatabaseFeatureLayer(featureLayerStructureParameters.LayerUri.LocalPath, tableName);

            if (featureLayerStructureParameters.AddedFeatures.Count > 0)
            {
                fileGeoDatabaseFeatureLayer.Open();
                fileGeoDatabaseFeatureLayer.EditTools.BeginTransaction();
                foreach (var feature in featureLayerStructureParameters.AddedFeatures)
                {
                    fileGeoDatabaseFeatureLayer.EditTools.Add(feature);
                }
                fileGeoDatabaseFeatureLayer.EditTools.CommitTransaction();
                fileGeoDatabaseFeatureLayer.Close();
            }

            return fileGeoDatabaseFeatureLayer;
        }

        protected override Collection<IntermediateColumn> GetIntermediateColumnsCore(IEnumerable<FeatureSourceColumn> columns)
        {
            Collection<IntermediateColumn> resultColumns = new Collection<IntermediateColumn>();

            foreach (var column in columns)
            {
                IntermediateColumn geoColumn = new IntermediateColumn();
                geoColumn.MaxLength = column.MaxLength;
                geoColumn.ColumnName = column.ColumnName;

                switch (column.TypeName.ToUpperInvariant())
                {
                    case "DOUBLE":
                        geoColumn.IntermediateColumnType = IntermediateColumnType.Double;
                        break;

                    case "DATE":
                    case "DATETIME":
                        geoColumn.IntermediateColumnType = IntermediateColumnType.Date;
                        break;

                    case "INTEGER":
                    case "INT":
                    case "OID":
                        geoColumn.IntermediateColumnType = IntermediateColumnType.Integer;
                        break;

                    case "STRING":
                        geoColumn.IntermediateColumnType = IntermediateColumnType.String;
                        break;

                    default:
                        geoColumn.IntermediateColumnType = IntermediateColumnType.String;
                        geoColumn.MaxLength = 255;
                        break;
                }

                resultColumns.Add(geoColumn);
            }

            return resultColumns;
        }

        protected override SimpleShapeType GetFeatureSimpleShapeTypeCore(FeatureLayer featureLayer)
        {
            FileGeoDatabaseFeatureLayer layer = featureLayer as FileGeoDatabaseFeatureLayer;
            if (layer != null)
            {
                WellKnownType wellKnownType = WellKnownType.Invalid;
                layer.SafeProcess(() =>
                {
                    FileGeoDatabaseFeatureSource featureSource = (FileGeoDatabaseFeatureSource)layer.FeatureSource;
                    wellKnownType = featureSource.GetFirstFeaturesWellKnownType();
                });
                return MapHelper.GetSimpleShapeType(wellKnownType);
            }
            return SimpleShapeType.Unknown;
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return new Uri(layer.Cast<FileGeoDatabaseFeatureLayer>().PathName);
        }

        [Obsolete("This method is obsoleted, please call DataSourceResolver.IsDataSourceAvailable(Layer) instead.")]
        protected override bool IsDataSourceAvailableCore(Layer layer)
        {
            return DataSourceResolveTool.IsDataSourceAvailable(layer);
        }

        [Obsolete("This method is obsoleted, please call DataSourceResolver.ResolveDataSource(Layer) instead.")]
        protected override void ResolveDataSourceCore(Layer layer)
        {
            DataSourceResolveTool.ResolveDataSource(layer);
        }

        protected override Collection<Layer> GetLayersCore(GetLayersParameters getLayersParameters)
        {
            Collection<Layer> resultLayers = new Collection<Layer>();

            if (getLayersParameters.LayerUris.Count > 0)
            {
                foreach (Uri uri in getLayersParameters.LayerUris)
                {
                    AddLayerByFileGeodatabaseWindow(resultLayers, uri.LocalPath, getLayersParameters);
                }
            }
            else
            {
                FolderHelper.OpenFolderBrowserDialog((tmpDialog, tmpResult) =>
                {
                    if (tmpResult == System.Windows.Forms.DialogResult.OK)
                    {
                        AddLayerByFileGeodatabaseWindow(resultLayers, tmpDialog.SelectedPath);
                    }
                }, tmpDialog =>
                {
                    tmpDialog.Description = GisEditor.LanguageManager.GetStringResource("FileGeoSelectFolderLabel");
                });
            }

            return resultLayers;
        }

        private static void AddLayerByFileGeodatabaseWindow(Collection<Layer> resultLayers, string fileName, GetLayersParameters getLayersParameters = null)
        {
            string tableName = string.Empty;
            string featureIdColumn = "OBJECTID";
            if (getLayersParameters != null)
            {
                if (getLayersParameters.CustomData.ContainsKey("TableName"))
                {
                    tableName = getLayersParameters.CustomData["TableName"] as string;
                }
                if (getLayersParameters.CustomData.ContainsKey("ObjectId"))
                {
                    featureIdColumn = getLayersParameters.CustomData["ObjectId"] as string;
                }
            }

            if (string.IsNullOrEmpty(tableName))
            {
                FileGeoDatabaseWindow fileGeoDatabaseWindow = new FileGeoDatabaseWindow(fileName);
                fileGeoDatabaseWindow.Owner = Application.Current.MainWindow;
                fileGeoDatabaseWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (fileGeoDatabaseWindow.ShowDialog().GetValueOrDefault())
                {
                    tableName = fileGeoDatabaseWindow.TableName;
                    featureIdColumn = fileGeoDatabaseWindow.FeatureIdColumn;
                }
            }

            if (Directory.Exists(fileName) && fileName.EndsWith(".gdb"))
            {
                try
                {
                    if (!string.IsNullOrEmpty(tableName))
                    {
                        FileGeoDatabaseFeatureLayer fileGeoDatabaseFeatureLayer = new FileGeoDatabaseFeatureLayer(fileName, tableName, featureIdColumn);
                        fileGeoDatabaseFeatureLayer.Name = char.ToUpper(tableName[0]) + tableName.Substring(1);
                        resultLayers.Add(fileGeoDatabaseFeatureLayer);
                    }
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("FileLayerPluginUsedByAnotherProcessText"), GisEditor.LanguageManager.GetStringResource("FileLayerPluginFilebeingusedCaption"));
                }
            }
            else
            {
                MessageBox.Show(GisEditor.LanguageManager.GetStringResource("FileGeoFormatInvalid"));
            }
        }
    }
}