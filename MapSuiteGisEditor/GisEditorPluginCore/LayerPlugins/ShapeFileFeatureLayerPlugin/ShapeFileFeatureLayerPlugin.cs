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
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.GisEditor.Toolkits;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ShapeFileFeatureLayerPlugin : FeatureLayerPlugin
    {
        private static readonly string errorMessageFormat;
        private const int characterTypeLength = 254;
        private const string featureIDColumnName = "FeatureIDColumn:";
        private const string defaultEncodingKey = "DefaultEncoding";

        private Dictionary<string, string> encodings;
        private ShapeFileSettingUserControl settingsUI;
        private Collection<FeatureLayer> layersInBuildingIndex;

        static ShapeFileFeatureLayerPlugin()
        {
            errorMessageFormat = GisEditor.LanguageManager.GetStringResource("ShapeFeatureFileLayerPluginNotFindFileText");
        }

        public ShapeFileFeatureLayerPlugin()
            : base()
        {
            layersInBuildingIndex = new Collection<FeatureLayer>();
            Name = GisEditor.LanguageManager.GetStringResource("ShapeFeatureFileLayerPluginShapefilesName");
            Description = GisEditor.LanguageManager.GetStringResource("ShapeFeatureFileLayerPluginSelectShapeFilesDescription");
            ExtensionFilterCore = "Shapefile(s) *.shp|*.shp";
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_fileicon_vector.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dataformats_shapefile.png", UriKind.RelativeOrAbsolute));
            encodings = new Dictionary<string, string>();
            Index = LayerPluginOrder.ShapeFeatureFileLayerPlugin;

            GisEditor.ProjectManager.Closed += ProjectManager_Closed;

            SearchPlaceToolCore = new ShapeFileSearchPlaceTool();
            DataSourceResolveToolCore = new ShapeFileDataSourceResolveTool(ExtensionFilter);
        }

        public Encoding DefaultEncoding
        {
            get { return ShapeFileSettingViewModel.Instance.SelectedEncoding.GetEncoding(); }
            set
            {
                if (value != null)
                {
                    ShapeFileSettingViewModel.Instance.SetEncoding(value);
                }
            }
        }

        public Dictionary<string, string> Encodings
        {
            get { return encodings; }
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

        protected override SettingUserControl GetSettingsUICore()
        {
            if (settingsUI == null)
            {
                settingsUI = new ShapeFileSettingUserControl();
                settingsUI.DataContext = ShapeFileSettingViewModel.Instance;
            }

            return settingsUI;
        }

        protected override Collection<IntermediateColumn> GetIntermediateColumnsCore(IEnumerable<FeatureSourceColumn> columns)
        {
            Collection<IntermediateColumn> resultColumns = new Collection<IntermediateColumn>();

            foreach (var column in columns)
            {
                var dbfColumn = column as DbfColumn;

                IntermediateColumn geoColumn = new IntermediateColumn();
                string columnType;

                if (dbfColumn != null)
                {
                    columnType = dbfColumn.ColumnType.ToString().ToUpper();
                    geoColumn.MaxLength = dbfColumn.MaxLength;
                    geoColumn.ColumnName = dbfColumn.ColumnName;
                }
                else
                {
                    columnType = column.TypeName;
                    geoColumn.MaxLength = column.MaxLength;
                    geoColumn.ColumnName = column.ColumnName;
                }

                switch (columnType.ToUpper())
                {
                    case "DOUBLE":
                    case "FLOAT":
                    case "NUMERIC":
                        geoColumn.IntermediateColumnType = IntermediateColumnType.Double;
                        break;

                    case "DATE":
                    case "DATETIME":
                        geoColumn.IntermediateColumnType = IntermediateColumnType.Date;
                        break;

                    case "INTEGER":
                    case "INT":
                        geoColumn.IntermediateColumnType = IntermediateColumnType.Integer;
                        break;

                    case "STRING":
                    case "CHARACTER":
                        geoColumn.IntermediateColumnType = IntermediateColumnType.String;
                        break;

                    default:
                        geoColumn.IntermediateColumnType = IntermediateColumnType.String;
                        geoColumn.MaxLength = characterTypeLength;
                        break;
                }

                resultColumns.Add(geoColumn);
            }

            return resultColumns;
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(ShapeFileFeatureLayer);
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return new Uri(layer.Cast<ShapeFileFeatureLayer>().ShapePathFilename);
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
            Collection<Layer> resultLayers = base.GetLayersCore(getLayersParameters);
            Collection<Uri> uris = getLayersParameters.LayerUris;
            Collection<string> invalidFileNames = new Collection<string>();

            foreach (Uri uri in uris)
            {
                var fileName = uri.LocalPath;
                if (ValidShapeFile(fileName))
                {
                    RemoveReadonlyAttribute(fileName);

                    try
                    {
                        var shapeFileFeatureLayer = new ShapeFileFeatureLayer(fileName, GeoFileReadWriteMode.Read);
                        shapeFileFeatureLayer.Name = Path.GetFileNameWithoutExtension(fileName);
                        shapeFileFeatureLayer.Encoding = GetEncoding(fileName);
                        shapeFileFeatureLayer.SimplificationAreaInPixel = 4;
                        shapeFileFeatureLayer.RequireIndex = false;
                        shapeFileFeatureLayer.DrawingExceptionMode = DrawingExceptionMode.DrawException;

                        resultLayers.Add(shapeFileFeatureLayer);
                    }
                    catch (Exception ex)
                    {
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                        System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("FileLayerPluginUsedByAnotherProcessText"), GisEditor.LanguageManager.GetStringResource("FileLayerPluginFilebeingusedCaption"));
                    }
                }
                else
                {
                    string shxPath = Path.ChangeExtension(fileName, ".shx");
                    string dbfPath = Path.ChangeExtension(fileName, ".dbf");
                    if (!File.Exists(fileName)) invalidFileNames.Add(fileName);
                    if (!File.Exists(shxPath)) invalidFileNames.Add(shxPath);
                    if (!File.Exists(dbfPath)) invalidFileNames.Add(dbfPath);
                }
            }
            if (invalidFileNames.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in invalidFileNames)
                {
                    sb.Append(item + ", ");
                }

                GisEditorMessageBox messageBox = new GisEditorMessageBox(MessageBoxButton.OK);
                messageBox.Title = GisEditor.LanguageManager.GetStringResource("MissingShapefilesCaption");
                messageBox.Message = GisEditor.LanguageManager.GetStringResource("SomeShapeFilesMissingMessage");
                messageBox.ErrorMessage = string.Format(errorMessageFormat, sb.Remove(sb.Length - 2, 2).ToString());
                messageBox.ShowDialog();
            }
            return resultLayers;
        }

        protected override ConfigureFeatureLayerParameters GetConfigureFeatureLayerParametersCore(FeatureLayer featureLayer)
        {
            CreateFeatureLayerUserControl userControl = new ConfigShapeFileUserControl((ShapeFileFeatureLayer)featureLayer);
            GeneralWindow window = new GeneralWindow();
            window.Tag = userControl;
            window.OkButtonClicking += Window_OkButtonClicking;
            window.Title = GisEditor.LanguageManager.GetStringResource("ShapefileWindowTitle");
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
        }

        protected override ConfigureFeatureLayerParameters GetCreateFeatureLayerParametersCore(IEnumerable<FeatureSourceColumn> columns)
        {
            MemoColumnConvertMode memoColumnMode = MemoColumnConvertMode.None;
            LongColumnTruncateMode longColumnNameMode = LongColumnTruncateMode.None;
            List<FeatureSourceColumn> memoColumns = columns.Where(c => c.TypeName.Equals("Memo", StringComparison.InvariantCultureIgnoreCase)).ToList();
            bool hasLongColumn = columns.Any(c => c.ColumnName.Length > 10);
            bool isColumnCheckCanceled = false;

            if (memoColumns.Count > 0)
            {
                MemoColumnModeWarningWindow memoColumnModeWarningWindow = new MemoColumnModeWarningWindow(GisEditor.LanguageManager.GetStringResource("ExportToShapefileLongColumnNameWarningCaption"), "There're one or more memo type columns, which might not be supported by other products. Click 'Convert and Save' would convert memo type to character type, click 'Save' would keep memo types.");
                if (memoColumnModeWarningWindow.ShowDialog().GetValueOrDefault())
                {
                    memoColumnMode = memoColumnModeWarningWindow.MemoColumnMode;
                }
                else
                {
                    isColumnCheckCanceled = true;
                }
            }

            if (!isColumnCheckCanceled && hasLongColumn)
            {
                LongColumnNameWarningWindow longColumnNameWarningWindow = new LongColumnNameWarningWindow(GisEditor.LanguageManager.GetStringResource("ExportToShapefileLongColumnNameWarningCaption"), GisEditor.LanguageManager.GetStringResource("ExportToShapefileLongColumnNameWarning"));
                if (longColumnNameWarningWindow.ShowDialog().GetValueOrDefault())
                {
                    longColumnNameMode = longColumnNameWarningWindow.LongColumnNameMode;
                }
                else
                {
                    isColumnCheckCanceled = true;
                }
            }

            ConfigureFeatureLayerParameters configureFeatureLayerParameters = null;
            if (!isColumnCheckCanceled)
            {
                var outputWindow = GisEditor.ControlManager.GetUI<OutputWindow>();
                outputWindow.OutputMode = OutputMode.ToFile;
                outputWindow.Extension = ".shp";
                outputWindow.CustomData["Columns"] = new Collection<FeatureSourceColumn>(columns.ToList());
                if (longColumnNameMode == LongColumnTruncateMode.Truncate)
                {
                    outputWindow.CustomData["CustomizeColumnNames"] = true;
                }

                if (string.IsNullOrEmpty(OutputWindow.SavedProj4ProjectionParametersString))
                {
                    outputWindow.Proj4ProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
                }
                else
                {
                    outputWindow.Proj4ProjectionParametersString = OutputWindow.SavedProj4ProjectionParametersString;
                }
                if (outputWindow.ShowDialog().GetValueOrDefault())
                {
                    configureFeatureLayerParameters = new ConfigureFeatureLayerParameters(outputWindow.LayerUri);
                    configureFeatureLayerParameters.MemoColumnConvertMode = memoColumnMode;
                    configureFeatureLayerParameters.LongColumnTruncateMode = longColumnNameMode;

                    configureFeatureLayerParameters.Proj4ProjectionParametersString = outputWindow.Proj4ProjectionParametersString;
                    OutputWindow.SavedProj4ProjectionParametersString = outputWindow.Proj4ProjectionParametersString;
                    foreach (var item in outputWindow.CustomData)
                    {
                        configureFeatureLayerParameters.CustomData[item.Key] = item.Value;
                    }
                }
            }

            return configureFeatureLayerParameters;
        }

        protected override FeatureLayer CreateFeatureLayerCore(ConfigureFeatureLayerParameters featureLayerStructureParameters)
        {
            string layerPath = LayerPluginHelper.GetLayerUriToSave(featureLayerStructureParameters.LayerUri, ExtensionFilterCore);
            if (string.IsNullOrEmpty(layerPath))
            {
                return null;
            }
            featureLayerStructureParameters.LayerUri = new Uri(layerPath);

            ShapeFileType shapeFileType = ShapeFileType.Null;

            switch (featureLayerStructureParameters.WellKnownType)
            {
                case WellKnownType.Multipoint:
                    shapeFileType = ShapeFileType.Multipoint;
                    break;

                case WellKnownType.Point:
                    shapeFileType = ShapeFileType.Point;
                    break;

                case WellKnownType.Line:
                case WellKnownType.Multiline:
                    shapeFileType = ShapeFileType.Polyline;
                    break;

                case WellKnownType.Polygon:
                case WellKnownType.Multipolygon:
                    shapeFileType = ShapeFileType.Polygon;
                    break;
            }

            Dictionary<string, DbfColumn> dbfColumns = new Dictionary<string, DbfColumn>();
            Collection<FeatureSourceColumn> addedColumns = featureLayerStructureParameters.AddedColumns;
            Dictionary<string, string> oldNewNames = new Dictionary<string, string>();
            Collection<Feature> addedFeatures = featureLayerStructureParameters.AddedFeatures;

            bool truncateLongColumn = featureLayerStructureParameters.LongColumnTruncateMode == LongColumnTruncateMode.Truncate;

            if (truncateLongColumn)
            {
                Dictionary<string, string> editColumns = new Dictionary<string, string>();
                if (featureLayerStructureParameters.CustomData.ContainsKey("EditedColumns"))
                {
                    editColumns = featureLayerStructureParameters.CustomData["EditedColumns"] as Dictionary<string, string>;
                }
                addedColumns = TruncateLongColumnNames(featureLayerStructureParameters.AddedColumns, oldNewNames, editColumns);
            }

            foreach (var column in addedColumns)
            {
                if (!string.IsNullOrEmpty(column.ColumnName))
                {
                    DbfColumn dbfColumn = column as DbfColumn;
                    if (dbfColumn != null)
                    {
                        if (dbfColumn.ColumnType == DbfColumnType.DoubleInBinary || dbfColumn.ColumnType == DbfColumnType.DateTime)
                        {
                            dbfColumn.Length = 8;
                            dbfColumn.DecimalLength = 0;
                        }
                        else if (dbfColumn.ColumnType == DbfColumnType.IntegerInBinary)
                        {
                            dbfColumn.Length = 4;
                            dbfColumn.DecimalLength = 0;
                        }
                    }
                    else
                    {
                        int columnLenght = column.MaxLength;
                        int decimalLength = 0;

                        switch (column.TypeName.ToUpperInvariant())
                        {
                            case "DOUBLE":
                            case "NUMERIC":
                                columnLenght = columnLenght == 0 ? 10 : columnLenght;
                                if (columnLenght < 4)
                                {
                                    columnLenght = 10;
                                }
                                decimalLength = 4;
                                break;

                            case "DATE":
                            case "DATETIME":
                                columnLenght = columnLenght == 0 ? 10 : columnLenght;
                                decimalLength = 0;
                                break;

                            case "INTEGER":
                            case "INT":
                                columnLenght = columnLenght == 0 ? 10 : columnLenght;
                                decimalLength = 0;
                                break;

                            case "STRING":
                            case "CHARACTER":
                                columnLenght = columnLenght == 0 ? characterTypeLength : columnLenght;
                                decimalLength = 0;
                                break;

                            case "LOGICAL":
                                columnLenght = 5;
                                decimalLength = 0;
                                break;
                        }

                        DbfColumnType type = DbfColumnType.Character;
                        if (column.TypeName.Equals("DOUBLE", StringComparison.InvariantCultureIgnoreCase))
                            column.TypeName = DbfColumnType.Float.ToString();
                        if (column.TypeName.Equals("INT", StringComparison.InvariantCultureIgnoreCase))
                            column.TypeName = DbfColumnType.Numeric.ToString();
                        bool isSuccess = Enum.TryParse<DbfColumnType>(column.TypeName, true, out type);
                        if (!isSuccess) type = DbfColumnType.Character;

                        dbfColumn = new DbfColumn(column.ColumnName, type, columnLenght, decimalLength);
                        dbfColumn.TypeName = column.TypeName;
                        dbfColumn.MaxLength = column.MaxLength;
                    }
                    //Feature firstFeature = featureLayerStructureParameters.AddedFeatures.FirstOrDefault();

                    ////This is to fix that fox pro columns cannot write to dbf, convert all linked columns to character column type.
                    //string tempColumnName = column.ColumnName;
                    //if (oldNewNames.ContainsValue(column.ColumnName))
                    //{
                    //    tempColumnName = oldNewNames.FirstOrDefault(f => f.Value == column.ColumnName).Key;
                    //}
                    //if (tempColumnName.Contains(".") && firstFeature != null && firstFeature.LinkColumnValues.ContainsKey(tempColumnName))
                    //{
                    //    if (dbfColumn.ColumnType != DbfColumnType.Memo)
                    //    {
                    //        dbfColumn.ColumnType = DbfColumnType.Character;
                    //        dbfColumn.Length = characterTypeLength;
                    //        dbfColumn.DecimalLength = 0;
                    //    }
                    //}

                    dbfColumns[dbfColumn.ColumnName] = dbfColumn;
                }
            }

            bool convertMemoToCharacter = featureLayerStructureParameters.MemoColumnConvertMode == MemoColumnConvertMode.ToCharacter;

            Dictionary<string, int> columnLength = new Dictionary<string, int>();

            foreach (var feature in addedFeatures)
            {
                //foreach (var linkColumnValue in feature.LinkColumnValues)
                //{
                //    if (!feature.ColumnValues.ContainsKey(linkColumnValue.Key))
                //    {
                //        string[] values = linkColumnValue.Value.Select(v =>
                //        {
                //            if (v.Value == null)
                //            {
                //                return string.Empty;
                //            }
                //            if (v.Value is DateTime)
                //            {
                //                return ((DateTime)v.Value).ToShortDateString();
                //            }
                //            return v.Value.ToString();
                //        }).ToArray();
                //        if (values.All(v => string.IsNullOrEmpty(v) || string.IsNullOrWhiteSpace(v)))
                //        {
                //            if (oldNewNames.ContainsKey(linkColumnValue.Key))
                //                feature.ColumnValues[oldNewNames[linkColumnValue.Key]] = string.Empty;
                //            else
                //                feature.ColumnValues[linkColumnValue.Key] = string.Empty;
                //        }
                //        else
                //        {
                //            string tempColumName = linkColumnValue.Key;
                //            if (oldNewNames.ContainsKey(linkColumnValue.Key))
                //            {
                //                tempColumName = oldNewNames[linkColumnValue.Key];
                //            }
                //            string linkValue = string.Join(",", values);
                //            feature.ColumnValues[tempColumName] = linkValue;

                //            //Choose the max length
                //            if (columnLength.ContainsKey(tempColumName))
                //            {
                //                if (columnLength[tempColumName] < linkValue.Length)
                //                {
                //                    columnLength[tempColumName] = linkValue.Length;
                //                }
                //            }
                //            else
                //            {
                //                columnLength[tempColumName] = linkValue.Length > 254 ? 254 : linkValue.Length;
                //            }
                //        }
                //    }
                //}
                foreach (var item in oldNewNames)
                {
                    if (feature.ColumnValues.ContainsKey(item.Key))
                    {
                        feature.ColumnValues[oldNewNames[item.Key]] = feature.ColumnValues[item.Key];
                        feature.ColumnValues.Remove(item.Key);
                    }
                }
                if (!convertMemoToCharacter)
                {
                    foreach (var item in feature.ColumnValues)
                    {
                        if (item.Value.Length > characterTypeLength && dbfColumns[item.Key].ColumnType != DbfColumnType.Memo)
                        {
                            dbfColumns[item.Key].ColumnType = DbfColumnType.Memo;
                            dbfColumns[item.Key].Length = 4;
                            dbfColumns[item.Key].DecimalLength = 0;
                        }
                    }
                }
            }

            foreach (var column in dbfColumns)
            {
                Feature firstFeature = featureLayerStructureParameters.AddedFeatures.FirstOrDefault();
                //This is to fix that fox pro columns cannot write to dbf, convert all linked columns to character column type.
                string tempColumnName = column.Key;
                if (oldNewNames.ContainsValue(column.Key))
                {
                    tempColumnName = oldNewNames.FirstOrDefault(f => f.Value == column.Key).Key;
                }
                //if (tempColumnName.Contains(".") && firstFeature != null && firstFeature.LinkColumnValues.ContainsKey(tempColumnName))
                //{
                //    if (column.Value.ColumnType != DbfColumnType.Memo)
                //    {
                //        column.Value.ColumnType = DbfColumnType.Character;
                //        //column.Value.Length = characterTypeLength;
                //        column.Value.DecimalLength = 0;

                //        if (columnLength.ContainsKey(tempColumnName) && column.Value.Length < columnLength[tempColumnName])
                //        {
                //            column.Value.Length = columnLength[tempColumnName];
                //        }
                //    }
                //}
            }

            ShapeFileFeatureLayer.CreateShapeFile(shapeFileType,
                featureLayerStructureParameters.LayerUri.OriginalString,
                dbfColumns.Values,
                DefaultEncoding,
                OverwriteMode.Overwrite);

            string encodingPathFileName = Path.ChangeExtension(featureLayerStructureParameters.LayerUri.OriginalString, ".cpg");
            if (File.Exists(encodingPathFileName)) File.Delete(encodingPathFileName);
            File.WriteAllText(encodingPathFileName, DefaultEncoding.CodePage.ToString(CultureInfo.InvariantCulture));

            string prjPath = Path.ChangeExtension(featureLayerStructureParameters.LayerUri.OriginalString, "prj");
            File.WriteAllText(prjPath, Proj4Projection.ConvertProj4ToPrj(featureLayerStructureParameters.Proj4ProjectionParametersString));

            ShapeFileFeatureLayer resultLayer = new ShapeFileFeatureLayer(featureLayerStructureParameters.LayerUri.LocalPath, GeoFileReadWriteMode.ReadWrite);

            if (addedFeatures.Count > 0)
            {
                resultLayer.Open();
                resultLayer.EditTools.BeginTransaction();
                foreach (var feature in addedFeatures)
                {
                    if (convertMemoToCharacter)
                    {
                        foreach (var item in dbfColumns)
                        {
                            if (feature.ColumnValues.ContainsKey(item.Key) && feature.ColumnValues[item.Key].Length > 254)
                            {
                                feature.ColumnValues[item.Key] = feature.ColumnValues[item.Key].Substring(0, 254);
                            }
                            if (feature.ColumnValues.ContainsKey(item.Key) && feature.ColumnValues[item.Key].Length > item.Value.MaxLength)
                            {
                                feature.ColumnValues[item.Key] = feature.ColumnValues[item.Key].Substring(0, item.Value.MaxLength);
                            }
                        }
                    }

                    resultLayer.EditTools.Add(feature);
                }
                resultLayer.EditTools.CommitTransaction();
                resultLayer.Close();
            }

            return resultLayer;
        }

        private Collection<FeatureSourceColumn> TruncateLongColumnNames(IEnumerable<FeatureSourceColumn> addedColumns, Dictionary<string, string> oldNewNames, Dictionary<string, string> editColumns)
        {
            Collection<FeatureSourceColumn> fixedColumns = new Collection<FeatureSourceColumn>();

            foreach (FeatureSourceColumn column in addedColumns)
            {
                string tempColumName = column.ColumnName;
                if (editColumns.ContainsKey(tempColumName))
                {
                    tempColumName = editColumns[tempColumName];
                }

                if (tempColumName.Length > 10)
                {
                    string oldName = column.ColumnName;
                    column.ColumnName = tempColumName.Substring(0, 10);
                    int i = 1;
                    column.ColumnName = column.ColumnName.Replace('.', '_');
                    while (fixedColumns.Select(a => a.ColumnName).Contains(column.ColumnName))
                    {
                        int length = i.ToString(CultureInfo.InvariantCulture).Length;
                        column.ColumnName = column.ColumnName.Substring(0, 10 - length) + i;
                        i++;
                    }

                    oldNewNames[oldName] = column.ColumnName;
                }
                else
                {
                    if (column.ColumnName != tempColumName)
                    {
                        oldNewNames[column.ColumnName] = tempColumName;
                    }
                    column.ColumnName = tempColumName;
                }

                fixedColumns.Add(column);
            }

            return fixedColumns;
        }

        private void RemoveReadonlyAttribute(string fileName)
        {
            string[] extensions = { ".shp", ".shx", ".dbf", ".ids", ".idx", ".prj" };
            string tempFileName = fileName;
            foreach (var extension in extensions)
            {
                tempFileName = fileName.Replace(".shp", extension);
                if (File.Exists(tempFileName))
                {
                    try
                    {
                        File.SetAttributes(tempFileName, FileAttributes.Normal);
                    }
                    catch (Exception ex)
                    {
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    }
                }
            }
        }

        protected override UserControl GetPropertiesUICore(Layer layer)
        {
            ShapefileFeatureLayerPropertiesUserControl metadataUserControl = new ShapefileFeatureLayerPropertiesUserControl((ShapeFileFeatureLayer)layer);
            LayerPluginHelper.SaveFeatureIDColumns((FeatureLayer)layer, metadataUserControl);

            return metadataUserControl;
        }

        protected override void OnGottenLayers(GottenLayersLayerPluginEventArgs e)
        {
            base.OnGottenLayers(e);
            BuildIndexAdapter adapter = new ShapeFileBuildIndexAdapter(this);
            adapter.BuildIndex(e.Layers.OfType<FeatureLayer>());
        }

        protected override SimpleShapeType GetFeatureSimpleShapeTypeCore(FeatureLayer featureLayer)
        {
            SimpleShapeType result = SimpleShapeType.Unknown;
            ShapeFileFeatureLayer shapeFileFeatureLayer = featureLayer as ShapeFileFeatureLayer;
            bool isDataSourceAvailable = DataSourceResolveTool.IsDataSourceAvailable(featureLayer);
            if (shapeFileFeatureLayer != null && isDataSourceAvailable)
            {
                ShapeFileType shapeFileType = ShapeFileType.Null;
                shapeFileFeatureLayer.SafeProcess(()
                    => shapeFileType = shapeFileFeatureLayer.GetShapeFileType());

                switch (shapeFileType)
                {
                    case ShapeFileType.Point:
                    case ShapeFileType.PointZ:
                    case ShapeFileType.Multipoint:
                    case ShapeFileType.PointM:
                    case ShapeFileType.MultipointM:
                        result = SimpleShapeType.Point;
                        break;

                    case ShapeFileType.Polyline:
                    case ShapeFileType.PolylineZ:
                    case ShapeFileType.PolylineM:
                    case ShapeFileType.Multipatch:
                        result = SimpleShapeType.Line;
                        break;

                    case ShapeFileType.Polygon:
                    case ShapeFileType.PolygonM:
                    case ShapeFileType.PolygonZ:
                        result = SimpleShapeType.Area;
                        break;
                }
            }

            return result;
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            settings.GlobalSettings[defaultEncodingKey] = DefaultEncoding.CodePage.ToString(CultureInfo.InvariantCulture);

            foreach (var item in encodings)
            {
                settings.GlobalSettings[item.Key] = item.Value;
            }
            foreach (var item in GisEditor.LayerManager.FeatureIdColumnNames)
            {
                settings.ProjectSettings[featureIDColumnName + item.Key] = item.Value;
            }
            settings.ProjectSettings["ExportProjection"] = OutputWindow.SavedProj4ProjectionParametersString;
            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            if (settings.ProjectSettings.ContainsKey("ExportProjection"))
            {
                OutputWindow.SavedProj4ProjectionParametersString = settings.ProjectSettings["ExportProjection"];
            }

            foreach (var item in settings.ProjectSettings)
            {
                if (item.Key.StartsWith(featureIDColumnName))
                {
                    GisEditor.LayerManager.FeatureIdColumnNames[item.Key.Replace(featureIDColumnName, string.Empty)] = item.Value;
                }
            }

            if (settings.GlobalSettings.ContainsKey(defaultEncodingKey))
            {
                int codePage = -1;
                string codePageString = settings.GlobalSettings[defaultEncodingKey];
                if (int.TryParse(codePageString, out codePage))
                {
                    DefaultEncoding = Encoding.GetEncoding(codePage);
                }
            }
            else DefaultEncoding = Encoding.UTF8;

            foreach (var item in settings.GlobalSettings.Where(i => i.Key != defaultEncodingKey))
            {
                if (File.Exists(item.Key))
                {
                    encodings[item.Key] = item.Value;
                }
            }
        }

        protected override LayerListItem GetLayerListItemCore(Layer layer)
        {
            var shapeFileLayerListItem = base.GetLayerListItemCore(layer);
            shapeFileLayerListItem.Name = layer.Name;
            var shapeFileLayer = layer as ShapeFileFeatureLayer;
            if (shapeFileLayer != null && !shapeFileLayer.RequireIndex)
            {
                string noIndexToolTip = "This layer doesn't use spatial index file";
                if (GisEditor.LanguageManager != null)
                {
                    GisEditor.LanguageManager.GetStringResource("MapElementsListUserControlNoIndexFileToolTip");
                }
                shapeFileLayerListItem.WarningImages.Add(new Image { Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorInfrastructure;component/Images/NoIndexFile.png", UriKind.Absolute)), Width = 14, Height = 14, ToolTip = noIndexToolTip });
            }
            return shapeFileLayerListItem;
        }

        protected override Collection<MenuItem> GetLayerListItemContextMenuItemsCore(GetLayerListItemContextMenuParameters parameters)
        {
            var menuItems = base.GetLayerListItemContextMenuItemsCore(parameters);
            ShapeFileFeatureLayer shpLayer = (ShapeFileFeatureLayer)parameters.LayerListItem.ConcreteObject;
            MenuItem rebuildShpItem = new MenuItem();
            rebuildShpItem.Header = GisEditor.LanguageManager.GetStringResource("ShapefileRebuildHeader");
            rebuildShpItem.Name = "Rebuild";
            rebuildShpItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/rebuildShp.png", UriKind.RelativeOrAbsolute)) };
            rebuildShpItem.Click += (s, e) =>
            {
                string directory = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "Output");
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                shpLayer.CloseInOverlay();

                //bool isCanceld = false;

                ProgressWindow window = new ProgressWindow();
                window.Title = GisEditor.LanguageManager.GetStringResource("RebuildShapefileWindowTitle");
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Owner = Application.Current.MainWindow;
                window.MainContent = "Rebuilding...";

                window.ProgressAction = () =>
                {
                    shpLayer.SafeProcess(() =>
                    {
                        ShapeFileFeatureSource.Rebuilding += (sender, e1) =>
                        {
                            if (e1.ShapePathFilename.Equals(shpLayer.ShapePathFilename, StringComparison.InvariantCultureIgnoreCase))
                            {
                                e1.Cancel = window.IsCanceled;
                                Application.Current.Dispatcher.BeginInvoke(() =>
                                {
                                    window.Maximum = e1.RecordCount;
                                    window.ProgressValue = e1.CurrentRecordIndex;
                                });
                            }
                        };
                        ShapeFileFeatureSource.Rebuild(shpLayer.ShapePathFilename);
                    });

                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (window.DialogResult == null)
                        {
                            window.DialogResult = !window.IsCanceled;
                        }
                    });
                };

                if (window.ShowDialog().GetValueOrDefault())
                {
                    GisEditor.ActiveMap.GetOverlaysContaining(shpLayer).ForEach(o => o.Invalidate());
                    MessageBox.Show(GisEditor.LanguageManager.GetStringResource("RebuildCompletedLabel"));
                }
            };
            var index = menuItems.Count - 3;
            if (index >= 0 && index <= menuItems.Count)
            {
                menuItems.Insert(index, LayerListMenuItemHelper.GetRebuildIndexMenuItem());
                menuItems.Insert(index++, rebuildShpItem);
            }
            return menuItems;
        }

        private bool ValidShapeFile(string shpPath)
        {
            string shxPath = Path.ChangeExtension(shpPath, ".shx");
            string dbfPath = Path.ChangeExtension(shpPath, ".dbf");
            return File.Exists(shpPath) && File.Exists(shxPath) && File.Exists(dbfPath);
        }

        private Encoding GetEncoding(string fileName)
        {
            Encoding encoding = null;

            encoding = GetEncodingFromCache(fileName);
            if (encoding == null) encoding = GetEncodingFromCpg(fileName);
            if (encoding == null) encoding = GetEncodingFromDbf(fileName);
            if (encoding == null) encoding = DefaultEncoding;
            return encoding;
        }

        private Encoding GetEncodingFromCpg(string fileName)
        {
            Encoding encoding = null;
            string cpgFileName = Path.ChangeExtension(fileName, ".cpg");
            if (File.Exists(cpgFileName))
            {
                string codePageString = File.ReadAllText(cpgFileName).Trim();
                int codePage = 0;
                if (int.TryParse(codePageString, out codePage))
                {
                    encoding = Encoding.GetEncoding(codePage);
                }
                else if (!string.IsNullOrEmpty(codePageString))
                {
                    try
                    {
                        encoding = Encoding.GetEncoding(codePageString);
                    }
                    catch { }
                }
            }

            return encoding;
        }

        private Encoding GetEncodingFromCache(string fileName)
        {
            Encoding encoding = null;
            if (Encodings.ContainsKey(fileName))
            {
                string codePageString = Encodings[fileName];
                int codePage;
                if (int.TryParse(codePageString, out codePage))
                {
                    encoding = Encoding.GetEncoding(codePage);
                }
            }
            return encoding;
        }

        private static Encoding GetEncodingFromDbf(string fileName)
        {
            FileStream fs = null;
            Encoding encoding = null;

            try
            {
                fs = new FileStream(Path.ChangeExtension(fileName, ".dbf"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fs.Seek(29, SeekOrigin.Begin);
                int ldid = fs.ReadByte();
                encoding = DetecteDbfEncoding(ldid.ToString("x"));
            }
            catch (IOException ioException)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ioException.Message, new ExceptionInfo(ioException));
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
            return encoding;
        }

        private static Encoding DetecteDbfEncoding(string ldid)
        {
            Encoding encoding = null;

            Stream ldidStream = Application.GetResourceStream(new Uri("/GisEditorPluginCore;component/Images/LDID.dat", UriKind.RelativeOrAbsolute)).Stream;
            using (StreamReader sr = new StreamReader(ldidStream))
            {
                string line = String.Empty;
                while (!String.IsNullOrEmpty((line = sr.ReadLine())))
                {
                    var pair = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (pair.Length == 2)
                    {
                        if (pair[0].Trim().Equals(ldid, StringComparison.OrdinalIgnoreCase))
                        {
                            encoding = Encoding.GetEncoding(Int32.Parse(pair[1].Trim()));
                            break;
                        }
                    }
                }
            }

            return encoding;
        }

        private void ProjectManager_Closed(object sender, EventArgs e)
        {
            OutputWindow.SavedProj4ProjectionParametersString = string.Empty;
        }

        #region search place
        [Obsolete("This method is obsoleted, please call SearchPlaceProvider.CanSearchPlace(Layer) instead.")]
        protected override bool CanSearchPlaceCore(Layer layer)
        {
            return SearchPlaceTool.CanSearchPlace(layer);
        }

        [Obsolete("This method is obsoleted, please call SearchPlaceProvider.SearchPlaces(string, Layer) instead.")]
        protected override Collection<Feature> SearchPlacesCore(string inputAddress, Layer layerToSearch)
        {
            return SearchPlaceTool.SearchPlaces(inputAddress, layerToSearch);
        }
        #endregion
    }
}