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
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DelimitedTextFeatureLayerPlugin : FeatureLayerPlugin
    {
        private static string LongitudeColumn = "X";
        private static string LatitudeColumn = "Y";
        private static string WKTColumn = "WKT";

        public DelimitedTextFeatureLayerPlugin()
            : base()
        {
            Name = GisEditor.LanguageManager.GetStringResource("DelimitedTextFeatureLayerPluginName");
            Description = GisEditor.LanguageManager.GetStringResource("DelimitedTextFeatureLayerPluginDescription");
            ExtensionFilterCore = "Delimited Text File(s) *.csv;*.txt|*.csv;*.txt";
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dataformats_delimitedtext.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dataformats_delimitedtext.png", UriKind.RelativeOrAbsolute));
            Index = LayerPluginOrder.CsvFileLayerPlugin;
            GisEditor.ProjectManager.Closed += ProjectManager_Closed;

            DataSourceResolveToolCore = new FileDataSourceResolveTool<CsvFeatureLayer>(ExtensionFilter,
                l => l.DelimitedPathFilename,
                (l, newPathFilename) => l.DelimitedPathFilename = newPathFilename);
        }

        protected override bool CanPageFeaturesEfficientlyCore
        {
            get { return true; }
        }

        protected override bool CanCreateFeatureLayerCore
        {
            get { return true; }
        }

        protected override ConfigureFeatureLayerParameters GetConfigureFeatureLayerParametersCore(FeatureLayer featureLayer)
        {
            ConfigCsvFileUserControl userControl = new ConfigCsvFileUserControl(featureLayer);
            GeneralWindow window = new GeneralWindow();
            window.Tag = userControl;
            window.OkButtonClicking += Window_OkButtonClicking;
            window.Title = GisEditor.LanguageManager.GetStringResource("CSVLayerWindowTitle");
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
                parameters.CustomData["Delimiter"] = userControl.ViewModel.Delimiter;
                parameters.CustomData["MappingType"] = userControl.ViewModel.MappingType;

                parameters.CustomData["X"] = GetSpecificColumnName(userControl.ViewModel.CsvColumns, CsvColumnType.Longitude);
                parameters.CustomData["Y"] = GetSpecificColumnName(userControl.ViewModel.CsvColumns, CsvColumnType.Latitude);
                parameters.CustomData["WKT"] = GetSpecificColumnName(userControl.ViewModel.CsvColumns, CsvColumnType.WKT);

                CsvFeatureLayer csvFeatureLayer = featureLayer as CsvFeatureLayer;
                if (csvFeatureLayer != null)
                {
                    csvFeatureLayer.XColumnName = parameters.CustomData["X"].ToString();
                    csvFeatureLayer.YColumnName = parameters.CustomData["Y"].ToString();
                    csvFeatureLayer.WellKnownTextColumnName = parameters.CustomData["WKT"].ToString();
                }
            }
            return parameters;
        }

        private static string GetSpecificColumnName(IEnumerable<AddNewCsvColumnViewModel> columns, CsvColumnType csvColumnType)
        {
            AddNewCsvColumnViewModel resultColumn = columns.FirstOrDefault(c => c.SelectedCsvColumnType == csvColumnType);
            return resultColumn == null ? string.Empty : resultColumn.ColumnName;
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(CsvFeatureLayer);
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return new Uri(layer.Cast<CsvFeatureLayer>().DelimitedPathFilename);
        }

        protected override FeatureLayer CreateFeatureLayerCore(ConfigureFeatureLayerParameters featureLayerStructureParameters)
        {
            bool hasFeatures = featureLayerStructureParameters.AddedFeatures.Count > 0;
            if (hasFeatures)
            {
                List<string> columns = featureLayerStructureParameters.AddedFeatures[0].ColumnValues.Keys.ToList();
                WKTColumn = GetUniqueColumn(WKTColumn, columns, 0);
                LongitudeColumn = GetUniqueColumn(LongitudeColumn, columns, 0);
                LatitudeColumn = GetUniqueColumn(LatitudeColumn, columns, 0);

                foreach (var feature in featureLayerStructureParameters.AddedFeatures)
                {
                    PointShape pointBaseShape = feature.GetShape() as PointShape;
                    if (pointBaseShape != null)
                    {
                        feature.ColumnValues[WKTColumn] = pointBaseShape.GetWellKnownText();
                    }
                    else
                    {
                        feature.ColumnValues[LongitudeColumn] = "";
                        feature.ColumnValues[LatitudeColumn] = "";
                    }
                }
            }

            string layerPath = LayerPluginHelper.GetLayerUriToSave(featureLayerStructureParameters.LayerUri, ExtensionFilter);

            if (string.IsNullOrEmpty(layerPath))
            {
                return null;
            }
            featureLayerStructureParameters.LayerUri = new Uri(layerPath);

            Collection<string> csvColumns = new Collection<string>();

            foreach (var column in featureLayerStructureParameters.AddedColumns)
            {
                csvColumns.Add(column.ColumnName);
            }

            string delimiter = ",";
            if (featureLayerStructureParameters.CustomData.ContainsKey("Delimiter"))
            {
                delimiter = featureLayerStructureParameters.CustomData["Delimiter"].ToString();
            }
            else if (featureLayerStructureParameters.WellKnownType != WellKnownType.Point && featureLayerStructureParameters.WellKnownType != WellKnownType.Multipoint)
            {
                delimiter = "\t";
            }

            DelimitedSpatialColumnsType csvMappingType = DelimitedSpatialColumnsType.WellKnownText;

            if (featureLayerStructureParameters.CustomData.ContainsKey("MappingType"))
            {
                csvMappingType = (DelimitedSpatialColumnsType)featureLayerStructureParameters.CustomData["MappingType"];
            }
            else
            {
                if (featureLayerStructureParameters.WellKnownType == WellKnownType.Point || featureLayerStructureParameters.WellKnownType == WellKnownType.Multipoint)
                {
                    csvMappingType = DelimitedSpatialColumnsType.XAndY;
                    featureLayerStructureParameters.CustomData["X"] = LongitudeColumn;
                    featureLayerStructureParameters.CustomData["Y"] = LatitudeColumn;
                    csvColumns.Add(LongitudeColumn);
                    csvColumns.Add(LatitudeColumn);
                    if (hasFeatures)
                    {
                        csvColumns.Add(WKTColumn);
                    }
                }
                else
                {
                    csvMappingType = DelimitedSpatialColumnsType.WellKnownText;
                    featureLayerStructureParameters.CustomData["WKT"] = WKTColumn;
                    if (hasFeatures)
                    {
                        csvColumns.Add(LongitudeColumn);
                        csvColumns.Add(LatitudeColumn);
                    }
                    csvColumns.Add(WKTColumn);
                }
            }

            CsvFeatureLayer.CreateDelimitedFile(featureLayerStructureParameters.LayerUri.OriginalString, csvColumns, delimiter, OverwriteMode.Overwrite);

            string prjPath = Path.ChangeExtension(featureLayerStructureParameters.LayerUri.OriginalString, "prj");
            File.WriteAllText(prjPath, Proj4Projection.ConvertProj4ToPrj(featureLayerStructureParameters.Proj4ProjectionParametersString));

            var resultLayer = new CsvFeatureLayer();
            resultLayer.DelimitedPathFilename = featureLayerStructureParameters.LayerUri.LocalPath;
            resultLayer.Delimiter = delimiter;
            resultLayer.SpatialColumnType = csvMappingType;
            resultLayer.RequireIndex = false;

            if (featureLayerStructureParameters.CustomData.ContainsKey("X"))
            {
                resultLayer.XColumnName = featureLayerStructureParameters.CustomData["X"].ToString();
            }
            if (featureLayerStructureParameters.CustomData.ContainsKey("Y"))
            {
                resultLayer.YColumnName = featureLayerStructureParameters.CustomData["Y"].ToString();
            }
            if (featureLayerStructureParameters.CustomData.ContainsKey("WKT"))
            {
                resultLayer.WellKnownTextColumnName = featureLayerStructureParameters.CustomData["WKT"].ToString();
            }
            resultLayer.Open();
            if (featureLayerStructureParameters.AddedFeatures.Count > 0)
            {
                resultLayer.EditTools.BeginTransaction();
                foreach (var feature in featureLayerStructureParameters.AddedFeatures)
                {
                    resultLayer.EditTools.Add(feature);
                }
                resultLayer.EditTools.CommitTransaction();
            }
            CSVModel.BuildDelimitedConfigurationFile(resultLayer);
            resultLayer.Close();

            return resultLayer;
        }

        private string GetUniqueColumn(string column, List<string> columns, int index)
        {
            if (columns.Contains(column))
            {
                index++;
                return GetUniqueColumn(column + index, columns, index);
            }
            return column;
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

            Collection<CSVInfoModel> entities = new Collection<CSVInfoModel>();
            bool hasUnconfiguredCsv = false;
            foreach (string fileName in getLayersParameters.LayerUris.Select(u => u.LocalPath))
            {
                string configFileName = Path.ChangeExtension(fileName, Path.GetExtension(fileName) + ".config");
                if (File.Exists(configFileName))
                {
                    var csvFileLastWriteTime = new FileInfo(fileName).LastWriteTime;
                    var csvConfLastWriteTime = GetConfigFileLastWriteTime(fileName);

                    if (csvFileLastWriteTime.ToString(CultureInfo.InvariantCulture).Equals(csvConfLastWriteTime.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
                    {
                        entities.Add(CSVInfoModel.FromConfig(configFileName));
                    }
                    else
                    {
                        CleanAttachedFiles(fileName, configFileName);
                        entities.Add(new CSVInfoModel(fileName, ","));
                        hasUnconfiguredCsv = true;
                    }
                }
                else
                {
                    entities.Add(new CSVInfoModel(fileName, ","));
                    hasUnconfiguredCsv = true;
                }
            }

            try
            {
                if (entities.Count == 0) return resultLayers;

                List<CSVModel> csvModels = new List<CSVModel>();
                if (hasUnconfiguredCsv)
                {
                    var viewModel = new CSVViewModel(entities);
                    CSVConfigWindow window = new CSVConfigWindow(viewModel);
                    viewModel.SelectedCSVModel.AutoMatch();
                    if (window.ShowDialog().GetValueOrDefault())
                    {
                        window.CSVModelList.ForEach(csvModels.Add);
                    }
                }
                else
                {
                    foreach (var csvModel in new CSVViewModel(entities).CSVModelList)
                    {
                        csvModels.Add(csvModel);
                    }
                }

                csvModels.ForEach(m => resultLayers.Add(m.CsvFeatureLayer));
                foreach (CsvFeatureLayer delimitedFeatureLayer in resultLayers.OfType<CsvFeatureLayer>())
                {
                    delimitedFeatureLayer.FeatureSource.CommittedTransaction -= delegate { CSVModel.BuildDelimitedConfigurationFile(delimitedFeatureLayer); };
                    delimitedFeatureLayer.FeatureSource.CommittedTransaction += delegate { CSVModel.BuildDelimitedConfigurationFile(delimitedFeatureLayer); };
                }
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                System.Windows.Forms.MessageBox.Show(ex.Message, "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }

            return resultLayers;
        }

        protected override ConfigureFeatureLayerParameters GetCreateFeatureLayerParametersCore(IEnumerable<FeatureSourceColumn> columns)
        {
            ConfigureFeatureLayerParameters configureFeatureLayerParameters = null;
            var featureLayer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as FeatureLayer;
            if (featureLayer != null)
            {
                WellKnownType wellKnownType =
                    featureLayer.FeatureSource.GetFirstFeaturesWellKnownType();

                OutputWindow outputWindow = new CsvOutputWindow(wellKnownType);
                outputWindow.OutputMode = OutputMode.ToFile;
                outputWindow.Extension = ".csv";
                outputWindow.ExtensionFilter = "Csv Files|*.csv";
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

        protected override UserControl GetPropertiesUICore(Layer layer)
        {
            UserControl propertiesUserControl = base.GetPropertiesUICore(layer);

            LayerPluginHelper.SaveFeatureIDColumns((FeatureLayer)layer, propertiesUserControl);

            return propertiesUserControl;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            if (settings.ProjectSettings.ContainsKey("ExportProjection"))
            {
                OutputWindow.SavedProj4ProjectionParametersString = settings.ProjectSettings["ExportProjection"];
            }
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            settings.ProjectSettings["ExportProjection"] = OutputWindow.SavedProj4ProjectionParametersString;
            return settings;
        }

        protected override void OnGottenLayers(GottenLayersLayerPluginEventArgs e)
        {
            base.OnGottenLayers(e);
            BuildIndexAdapter adapter = new DelimitedTextBuildIndexAdapter(this);
            adapter.BuildIndex(e.Layers.OfType<FeatureLayer>());
        }

        protected override SimpleShapeType GetFeatureSimpleShapeTypeCore(FeatureLayer featureLayer)
        {
            CsvFeatureLayer csvFeatureLayer = (CsvFeatureLayer)featureLayer;
            if (!string.IsNullOrEmpty(csvFeatureLayer.DelimitedPathFilename))
            {
                return SimpleShapeType.Unknown;
            }
            return SimpleShapeType.Point;
        }

        protected override LayerListItem GetLayerListItemCore(Layer layer)
        {
            var layerListItem = base.GetLayerListItemCore(layer);
            var csvLayer = (CsvFeatureLayer)layer;
            if (!csvLayer.RequireIndex)
            {
                const string noIndexToolTip = "This layer doesn't use spatial index file";
                if (GisEditor.LanguageManager != null)
                {
                    GisEditor.LanguageManager.GetStringResource("MapElementsListUserControlNoIndexFileToolTip");
                }
                layerListItem.WarningImages.Add(new Image { Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorInfrastructure;component/Images/NoIndexFile.png", UriKind.Absolute)), Width = 14, Height = 14, ToolTip = noIndexToolTip });
            }
            if (layerListItem.Children.Count == 0)
            {
                LayerListItem noStyleEntity = new LayerListItem
                {
                    Name = GisEditor.LanguageManager.GetStringResource("DelimitedTextFeatureLayerPluginDoubleClick"),
                    CheckBoxVisibility = Visibility.Collapsed,
                    DoubleClicked = () => LayerListHelper.AddStyle(new PointStyle(), csvLayer),
                    Parent = layerListItem,
                    ConcreteObject = csvLayer,
                    FontWeight = FontWeights.Bold
                };
                layerListItem.Children.Add(noStyleEntity);
            }
            return layerListItem;
        }

        private static void CleanAttachedFiles(string fileName, string configFileName)
        {
            File.Delete(configFileName);
            foreach (var ex in new[] { ".idx", ".ids" })
            {
                string tmpPathFileName = Path.ChangeExtension(fileName, ex);
                if (File.Exists(tmpPathFileName))
                {
                    File.Delete(tmpPathFileName);
                }
            }
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

        private void ProjectManager_Closed(object sender, EventArgs e)
        {
            OutputWindow.SavedProj4ProjectionParametersString = string.Empty;
        }

        private static DateTime GetConfigFileLastWriteTime(string delimitedPathFileName)
        {
            DateTime lastWriteTime = default(DateTime);
            string configurationPathFileName = delimitedPathFileName + ".config";
            string[] configurationItems = File.ReadAllLines(configurationPathFileName);
            if (configurationItems.Length == 7)
            {
                if (!DateTime.TryParse(configurationItems[6], out lastWriteTime))
                {
                    lastWriteTime = default(DateTime);
                }
            }

            return lastWriteTime;
        }
    }
}