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
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class TinyGeoFeatureLayerPlugin : FeatureLayerPlugin
    {
        public TinyGeoFeatureLayerPlugin()
            : base()
        {
            Name = GisEditor.LanguageManager.GetStringResource("TinyGeoFeatureLayerPluginName");
            Description = GisEditor.LanguageManager.GetStringResource("TinyGeoFeatureLayerPluginDescription");
            ExtensionFilterCore = "TinyGeo File(s) *.tgeo|*.tgeo";
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_fileicon_raster.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dataformats_tgeo.png", UriKind.RelativeOrAbsolute));
            Index = LayerPluginOrder.TinyGeoFeatureFileLayerPlugin;

            GisEditor.ProjectManager.Closed += ProjectManager_Closed;
            DataSourceResolveToolCore = new FileDataSourceResolveTool<TinyGeoFeatureLayer>(ExtensionFilter,
                l => l.TinyGeoPathFilename,
                (l, newPathFilename) => l.TinyGeoPathFilename = newPathFilename);
        }

        protected override bool CanPageFeaturesEfficientlyCore
        {
            get { return true; }
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(TinyGeoFeatureLayer);
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return new Uri(layer.Cast<TinyGeoFeatureLayer>().TinyGeoPathFilename);
        }

        protected override bool CanCreateFeatureLayerCore
        {
            get
            {
                return true;
            }
        }

        protected override ConfigureFeatureLayerParameters GetCreateFeatureLayerParametersCore(IEnumerable<FeatureSourceColumn> columns)
        {
            OutputWindow outputWindow = GisEditor.ControlManager.GetUI<OutputWindow>();
            outputWindow.OutputMode = OutputMode.ToFile;
            outputWindow.Extension = ".tgeo";
            outputWindow.ExtensionFilter = "TinyGeo Files|*.tgeo";
            if (string.IsNullOrEmpty(OutputWindow.SavedProj4ProjectionParametersString))
            {
                outputWindow.Proj4ProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
            }
            else
            {
                outputWindow.Proj4ProjectionParametersString = OutputWindow.SavedProj4ProjectionParametersString;
            }
            outputWindow.CustomData["Columns"] = new Collection<FeatureSourceColumn>(columns.ToList());
            ConfigureFeatureLayerParameters configureFeatureLayerParameters = null;
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
            return configureFeatureLayerParameters;
        }

        protected override ConfigureFeatureLayerParameters GetConfigureFeatureLayerParametersCore(FeatureLayer featureLayer)
        {
            CreateFeatureLayerUserControl userControl = new ConfigTinyGeoFileUserControl(featureLayer);
            GeneralWindow window = new GeneralWindow();
            window.Tag = userControl;
            window.OkButtonClicking += OKButtonClicking;
            window.Title = GisEditor.LanguageManager.GetStringResource("TinyGeoWindowTitle");
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

        protected override FeatureLayer CreateFeatureLayerCore(ConfigureFeatureLayerParameters featureLayerStructureParameters)
        {
            string layerPath = LayerPluginHelper.GetLayerUriToSave(featureLayerStructureParameters.LayerUri, ExtensionFilter);

            if (string.IsNullOrEmpty(layerPath))
            {
                return null;
            }
            featureLayerStructureParameters.LayerUri = new Uri(layerPath);

            Collection<TabDbfColumn> tabDbfColumns = new Collection<TabDbfColumn>();

            foreach (var column in featureLayerStructureParameters.AddedColumns)
            {
                var columnLenght = column.MaxLength;
                var decimalLength = 0;
                DbfColumnType columnType = DbfColumnType.Character;

                switch (column.TypeName.ToUpperInvariant())
                {
                    case "DOUBLE":
                        columnLenght = columnLenght == 0 ? 10 : columnLenght;
                        decimalLength = 4;
                        columnType = DbfColumnType.Float;
                        break;

                    case "DATE":
                    case "DATETIME":
                        columnLenght = columnLenght == 0 ? 10 : columnLenght;
                        decimalLength = 0;
                        columnType = DbfColumnType.Date;
                        break;

                    case "INTEGER":
                    case "INT":
                        columnLenght = columnLenght == 0 ? 10 : columnLenght;
                        decimalLength = 0;
                        columnType = DbfColumnType.Numeric;
                        break;

                    case "STRING":
                    case "CHARACTER":
                        columnLenght = columnLenght == 0 ? 255 : columnLenght;
                        decimalLength = 0;
                        columnType = DbfColumnType.Character;
                        break;
                    default:
                        break;
                }

                tabDbfColumns.Add(new TabDbfColumn(column.ColumnName, columnType, columnLenght, decimalLength, false, false));
            }

            if (featureLayerStructureParameters.CustomData.ContainsKey("SourceLayer"))
            {
                FeatureLayer sourcefeatureLayer = featureLayerStructureParameters.CustomData["SourceLayer"] as FeatureLayer;
                double precisionInMeter = TinyGeoFeatureLayer.GetOptimalPrecision(sourcefeatureLayer, GisEditor.ActiveMap.MapUnit, DistanceUnit.Meter, TinyGeoPrecisionMode.PreventSplitting);

                TinyGeoFeatureLayer.CreateTinyGeoFile(featureLayerStructureParameters.LayerUri.OriginalString, sourcefeatureLayer, GisEditor.ActiveMap.MapUnit, featureLayerStructureParameters.AddedColumns.Select(f => f.ColumnName).ToList(), "", precisionInMeter, Encoding.Default, featureLayerStructureParameters.WellKnownType);

                string prjPath = Path.ChangeExtension(featureLayerStructureParameters.LayerUri.OriginalString, "prj");
                File.WriteAllText(prjPath, Proj4Projection.ConvertProj4ToPrj(featureLayerStructureParameters.Proj4ProjectionParametersString));
            }

            var resultLayer = new TinyGeoFeatureLayer(featureLayerStructureParameters.LayerUri.OriginalString);
            return resultLayer;
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
            foreach (var fileName in getLayersParameters.LayerUris.Select(u => u.LocalPath))
            {
                TinyGeoFeatureLayer layer = new TinyGeoFeatureLayer(fileName);
                layer.Name = Path.GetFileNameWithoutExtension(fileName);
                resultLayers.Add(layer);
            }
            return resultLayers;
        }

        protected override SimpleShapeType GetFeatureSimpleShapeTypeCore(FeatureLayer featureLayer)
        {
            WellKnownType result = WellKnownType.Invalid;
            if (featureLayer != null)
            {
                featureLayer.SafeProcess(() =>
                {
                    Feature firstFeature = featureLayer.QueryTools.GetFeatureById("1", ReturningColumnsType.NoColumns);
                    result = firstFeature.GetWellKnownType();

                    if (result == WellKnownType.GeometryCollection)
                    {
                        GeometryCollectionShape geometryCollectionShape = (GeometryCollectionShape)firstFeature.GetShape();
                        result = geometryCollectionShape.Shapes[0].GetWellKnownType();
                    }
                });
            }

            return MapHelper.GetSimpleShapeType(result);
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            if (settings.ProjectSettings.ContainsKey("ExportProjection"))
            {
                OutputWindow.SavedProj4ProjectionParametersString = settings.ProjectSettings["ExportProjection"];
            }
        }

        protected override UserControl GetPropertiesUICore(Layer layer)
        {
            UserControl propertiesUserControl = base.GetPropertiesUICore(layer);

            LayerPluginHelper.SaveFeatureIDColumns((FeatureLayer)layer, propertiesUserControl);

            return propertiesUserControl;
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            settings.ProjectSettings["ExportProjection"] = OutputWindow.SavedProj4ProjectionParametersString;
            return settings;
        }

        private static void OKButtonClicking(object sender, RoutedEventArgs e)
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
    }
}