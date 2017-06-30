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
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    public abstract partial class FeatureLayerPlugin : LayerPlugin
    {
        [Obfuscation]
        private readonly string featureLayerStyleFolderPath;

        [Obfuscation]
        private readonly string featureLayerStyleSubFolderPath;

        [Obfuscation]
        private readonly string indexXmlPath;

        [Obfuscation]
        private readonly string featureLayerStylesFolderName = "FeatureLayerStyles";

        [Obfuscation]
        private readonly string featureLayerStylesSubFolderName = "Styles";

        [Obfuscation]
        private readonly string indexXmlFileName = "Index.xml";

        [Obfuscation]
        private readonly string xmlFileNameFormat = "{0}.xml";

        [Obfuscation]
        private readonly string defaultStylePathElementName = "StylePathFileName";

        [Obfuscation]
        private bool canCreateFeatureLayerCore;

        [Obfuscation]
        private bool canQueryFeaturesEfficiently;

        [Obfuscation]
        private bool canGetFeaturesByColumnValueEfficiently;

        [Obfuscation]
        private bool canPageFeaturesEfficiently;

        public FeatureLayerPlugin()
            : base()
        {
            canQueryFeaturesEfficiently = true;
            featureLayerStyleFolderPath = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, featureLayerStylesFolderName);
            featureLayerStyleSubFolderPath = Path.Combine(featureLayerStyleFolderPath, featureLayerStylesSubFolderName);
            indexXmlPath = Path.Combine(featureLayerStyleFolderPath, indexXmlFileName);
            GisEditor.ProjectManager.GottenDeserializedMaps -= ProjectManager_GottenDeserializedMaps;
            GisEditor.ProjectManager.GottenDeserializedMaps += ProjectManager_GottenDeserializedMaps;
        }

        public bool CanPageFeaturesEfficiently
        {
            get { return CanPageFeaturesEfficientlyCore; }
        }

        protected virtual bool CanPageFeaturesEfficientlyCore
        {
            get { return canPageFeaturesEfficiently; }
            set { canPageFeaturesEfficiently = value; }
        }

        [Obsolete("This property is obsoleted, please call CanGetFeaturesByColumnValueEfficiently instead.")]
        public bool CanQueryFeaturesEfficiently
        {
            get { return CanQueryFeaturesEfficientlyCore; }
        }

        [Obsolete("This property is obsoleted, please call CanGetFeaturesByColumnValueEfficientlyCore instead.")]
        protected virtual bool CanQueryFeaturesEfficientlyCore
        {
            get { return canQueryFeaturesEfficiently; }
            set { canQueryFeaturesEfficiently = value; }
        }

        public bool CanGetFeaturesByColumnValueEfficiently
        {
            get { return CanGetFeaturesByColumnValueEfficientlyCore; }
        }

        protected virtual bool CanGetFeaturesByColumnValueEfficientlyCore
        {
            get { return canGetFeaturesByColumnValueEfficiently; }
            set { canGetFeaturesByColumnValueEfficiently = value; }
        }

        public bool CanCreateFeatureLayer
        {
            get { return CanCreateFeatureLayerCore; }
        }

        protected virtual bool CanCreateFeatureLayerCore
        {
            get { return canCreateFeatureLayerCore; }
            set { canCreateFeatureLayerCore = value; }
        }

        public bool CanCreateFeatureLayerWithSourceColumns(FeatureLayerPlugin sourceFeatureLayerPlugin)
        {
            return CanCreateFeatureLayerWithSourceColumnsCore(sourceFeatureLayerPlugin);
        }

        protected virtual bool CanCreateFeatureLayerWithSourceColumnsCore(FeatureLayerPlugin sourceFeatureLayerPlugin)
        {
            return this.GetType().FullName == sourceFeatureLayerPlugin.GetType().FullName;
        }

        public UserControl GetViewDataUI()
        {
            return GetViewDataUICore();
        }

        protected virtual UserControl GetViewDataUICore()
        {
            FeatureLayer selectedLayer = null;
            if (GisEditor.LayerListManager.SelectedLayerListItem != null)
            {
                selectedLayer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as FeatureLayer;
            }
            DataViewerUserControl content = new DataViewerUserControl(selectedLayer);
            return content;
        }

        protected override UserControl GetPropertiesUICore(Layer layer)
        {
            FeatureLayerPropertiesUserControl metadataUserControl = new FeatureLayerPropertiesUserControl((FeatureLayer)layer);
            return metadataUserControl;
        }

        public SimpleShapeType GetFeatureSimpleShapeType(FeatureLayer featureLayer)
        {
            return GetFeatureSimpleShapeTypeCore(featureLayer);
        }

        public ConfigureFeatureLayerParameters GetConfigureFeatureLayerParameters()
        {
            return GetConfigureFeatureLayerParameters(null);
        }

        public ConfigureFeatureLayerParameters GetConfigureFeatureLayerParameters(FeatureLayer featureLayer)
        {
            if (CanCreateFeatureLayer)
            {
                return GetConfigureFeatureLayerParametersCore(featureLayer);
            }
            else throw new NotSupportedException();
        }

        protected virtual ConfigureFeatureLayerParameters GetConfigureFeatureLayerParametersCore(FeatureLayer featureLayer)
        {
            return null;
        }

        public ConfigureFeatureLayerParameters GetCreateFeatureLayerParameters()
        {
            return GetCreateFeatureLayerParametersCore(new Collection<FeatureSourceColumn>());
        }

        public ConfigureFeatureLayerParameters GetCreateFeatureLayerParameters(IEnumerable<FeatureSourceColumn> columns)
        {
            if (columns == null) columns = new Collection<FeatureSourceColumn>();
            return GetCreateFeatureLayerParametersCore(columns);
        }

        protected virtual ConfigureFeatureLayerParameters GetCreateFeatureLayerParametersCore(IEnumerable<FeatureSourceColumn> columns)
        {
            return null;
        }

        public FeatureLayer CreateFeatureLayer(ConfigureFeatureLayerParameters featureLayerStructureParameters)
        {
            FeatureLayer resultLayer = null;

            if (CanCreateFeatureLayer) resultLayer = CreateFeatureLayerCore(featureLayerStructureParameters);

            return resultLayer;
        }

        protected virtual FeatureLayer CreateFeatureLayerCore(ConfigureFeatureLayerParameters featureLayerStructureParameters)
        {
            throw new NotImplementedException();
        }

        public Collection<IntermediateColumn> GetIntermediateColumns(FeatureSource featureSource)
        {
            var featureSourceColumns = new Collection<IntermediateColumn>();

            featureSource.SafeProcess(() =>
            {
                featureSourceColumns = GetIntermediateColumns(featureSource.GetColumns());
            });

            return featureSourceColumns;
        }

        public Collection<IntermediateColumn> GetIntermediateColumns(IEnumerable<FeatureSourceColumn> columns)
        {
            return GetIntermediateColumnsCore(columns);
        }

        protected virtual Collection<IntermediateColumn> GetIntermediateColumnsCore(IEnumerable<FeatureSourceColumn> columns)
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

                    case "INTEGER":
                    case "INT":
                        geoColumn.IntermediateColumnType = IntermediateColumnType.Integer;
                        break;

                    case "STRING":
                    default:
                        geoColumn.IntermediateColumnType = IntermediateColumnType.String;
                        geoColumn.MaxLength = 255;
                        break;
                }

                resultColumns.Add(geoColumn);
            }

            return resultColumns;
        }

        protected virtual SimpleShapeType GetFeatureSimpleShapeTypeCore(FeatureLayer featureLayer)
        {
            var simpleType = SimpleShapeType.Unknown;
            WellKnownType shapeType = WellKnownType.Invalid;
            featureLayer.SafeProcess(() =>
            {
                var feature = featureLayer.QueryTools.GetAllFeatures(ReturningColumnsType.NoColumns).FirstOrDefault();
                if (feature != null && feature.GetWellKnownBinary() != null)
                {
                    shapeType = feature.GetWellKnownType();
                }

                if (shapeType == WellKnownType.GeometryCollection)
                {
                    var geometryCollectionShape = feature.GetShape() as GeometryCollectionShape;
                    shapeType = geometryCollectionShape.Shapes[0].GetWellKnownType();
                }
            });
            switch (shapeType)
            {
                case WellKnownType.Point:
                case WellKnownType.Multipoint:
                    simpleType = SimpleShapeType.Point;
                    break;

                case WellKnownType.Line:
                case WellKnownType.Multiline:
                    simpleType = SimpleShapeType.Line;
                    break;

                case WellKnownType.Polygon:
                case WellKnownType.Multipolygon:
                    simpleType = SimpleShapeType.Area;
                    break;

                case WellKnownType.Invalid:
                default:
                    simpleType = SimpleShapeType.Unknown;
                    break;
            }
            return simpleType;
        }

        protected override void OnGottenLayers(GottenLayersLayerPluginEventArgs e)
        {
            base.OnGottenLayers(e);

            List<FeatureLayer> layers = e.Layers.OfType<FeatureLayer>().ToList();
            layers.ForEach(tmpLayer =>
            {
                if (tmpLayer.DrawingMarginInPixel <= 0)
                {
                    tmpLayer.DrawingMarginInPixel = 200;
                }
            });

            ApplyStyles(layers);
            foreach (var item in layers)
            {
                if (item.ZoomLevelSet.CustomZoomLevels.Any(zoomLevel => HasTextStyleOnZoomLevel(zoomLevel) || HasPointStyleOnZoomLevel(zoomLevel)))
                    item.DrawingMarginInPixel = 512;

                item.FeatureSource.CommittingTransaction -= FeatureSource_CommittingTransaction;
                item.FeatureSource.CommittingTransaction += FeatureSource_CommittingTransaction;
            }
            ApplyProjection(layers);

            var resultLayers = layers.Where(l =>
            {
                Proj4ProjectionInfo proj4ProjectionInfo = l.GetProj4ProjectionInfo();
                return proj4ProjectionInfo != null
                    && !string.IsNullOrEmpty(proj4ProjectionInfo.InternalProjectionParametersString)
                    && !string.IsNullOrEmpty(proj4ProjectionInfo.ExternalProjectionParametersString);
            });

            e.Layers.Clear();
            foreach (var layer in resultLayers)
            {
                e.Layers.Add(layer);
                LoadDefaultStyle(layer);
            }
        }

        private static void FeatureSource_CommittingTransaction(object sender, CommittingTransactionEventArgs e)
        {
            bool canSaveProject = GisEditor.ProjectManager.CanSaveProject(new ProjectStreamInfo(GisEditor.ProjectManager.ProjectUri, null));

            if (!canSaveProject)
            {
                e.TransactionBuffer.AddBuffer.Clear();
                e.TransactionBuffer.EditBuffer.Clear();
                e.TransactionBuffer.DeleteBuffer.Clear();
                e.TransactionBuffer.AddColumnBuffer.Clear();
                e.TransactionBuffer.DeleteColumnBuffer.Clear();
                e.TransactionBuffer.UpdateColumnBuffer.Clear();
            }
        }

        private void ProjectManager_GottenDeserializedMaps(object sender, GottenDeserializedMapsEventArgs e)
        {
            IEnumerable<FeatureSource> featureSources = e.Maps.SelectMany(m => m.Overlays).OfType<LayerOverlay>().SelectMany(l => l.Layers).OfType<FeatureLayer>().Select(f => f.FeatureSource);
            foreach (var item in featureSources)
            {
                item.CommittingTransaction -= FeatureSource_CommittingTransaction;
                item.CommittingTransaction += FeatureSource_CommittingTransaction;
            }
        }

        /// <summary>
        /// This method gets a hierarchy object to build the layer list tree.
        /// </summary>
        /// <param name="layer">The layer list item to build the layer list tree.</param>
        /// <returns></returns>
        protected override LayerListItem GetLayerListItemCore(Layer layer)
        {
            LayerListItem featureLayerListItem = base.GetLayerListItemCore(layer);
            featureLayerListItem.Load = () =>
            {
                FeatureLayer featureLayer = layer as FeatureLayer;
                List<LayerListItem> styleLayerListItems = LayerListHelper.CollectCompositeStyleLayerListItem(featureLayer);
                if (styleLayerListItems != null)
                {
                    if (styleLayerListItems.Count == 0)
                    {
                        SimpleShapeType simpleShapeType = SimpleShapeType.Unknown;
                        featureLayer.SafeProcess(() =>
                        {
                            simpleShapeType = GetFeatureSimpleShapeType(featureLayer);
                        });
                        var noStyleEntity = new LayerListItem
                        {
                            Name = GisEditor.LanguageManager.GetStringResource("DelimitedTextFeatureLayerPluginDoubleClick"),
                            CheckBoxVisibility = Visibility.Collapsed,
                            DoubleClicked = () =>
                            {
                                Styles.Style shpStyle = null;
                                switch (simpleShapeType)
                                {
                                    case SimpleShapeType.Unknown:
                                    default:
                                        break;

                                    case SimpleShapeType.Point:
                                        shpStyle = new PointStyle();
                                        break;

                                    case SimpleShapeType.Line:
                                        shpStyle = new LineStyle();
                                        break;

                                    case SimpleShapeType.Area:
                                        shpStyle = new AreaStyle();
                                        break;
                                }

                                LayerListHelper.AddStyle();
                            },
                            Parent = featureLayerListItem,
                            ConcreteObject = layer,
                        };

                        switch (simpleShapeType)
                        {
                            case SimpleShapeType.Point:
                                noStyleEntity.ContextMenuItems.Add(LayerListMenuItemHelper.GetAddStyleMenuItem(AddStyleTypes.AddPointStyle | AddStyleTypes.AddTextStyle, featureLayer));
                                break;

                            case SimpleShapeType.Line:
                                noStyleEntity.ContextMenuItems.Add(LayerListMenuItemHelper.GetAddStyleMenuItem(AddStyleTypes.AddLineStyle | AddStyleTypes.AddTextStyle, featureLayer));
                                break;

                            case SimpleShapeType.Area:
                                noStyleEntity.ContextMenuItems.Add(LayerListMenuItemHelper.GetAddStyleMenuItem(AddStyleTypes.AddAreaStyle | AddStyleTypes.AddLineStyle, featureLayer));
                                break;
                        }

                        featureLayerListItem.Children.Add(noStyleEntity);
                    }
                }

                foreach (var styleLayerListItem in styleLayerListItems)
                {
                    styleLayerListItem.Parent = featureLayerListItem;
                    styleLayerListItem.IsExpanded = true;

                    Styles.Style tempStyle = styleLayerListItem.ConcreteObject as Styles.Style;
                    if (tempStyle.Filters.Count > 0)
                    {
                        if (featureLayer.IsOpen && featureLayer.HasBoundingBox)
                        {
                            RectangleShape bbox = featureLayer.GetBoundingBox();
                            Collection<Feature> allDataFeatures = featureLayer.FeatureSource.GetFeaturesForDrawing(bbox, GisEditor.ActiveMap.Width,
    GisEditor.ActiveMap.Height, new Collection<string>());

                            if (allDataFeatures.Count == 0)
                                styleLayerListItem.FontStyle = FontStyles.Italic;
                        }
                    }

                    featureLayerListItem.Children.Add(styleLayerListItem);
                }
            };

            featureLayerListItem.PropertyChanged -= LayerListItemPropertyChanged;
            featureLayerListItem.PropertyChanged += LayerListItemPropertyChanged;
            return featureLayerListItem;
        }

        protected override Collection<MenuItem> GetLayerListItemContextMenuItemsCore(GetLayerListItemContextMenuParameters parameters)
        {
            var menuItems = base.GetLayerListItemContextMenuItemsCore(parameters);
            var featureLayer = parameters.LayerListItem.ConcreteObject as FeatureLayer;
            if (featureLayer != null)
            {
                var transpaencyMenuItem = menuItems.LastOrDefault();
                if (transpaencyMenuItem != null) menuItems.Remove(transpaencyMenuItem);

                //if (GetViewDataUI() != null)
                {
                    //menuItems.Insert(0, new MenuItem() { Header = "--" });
                    menuItems.Insert(0, LayerListMenuItemHelper.GetViewDataMenuItem(null));
                }

                var simpleShapeType = GetFeatureSimpleShapeType(featureLayer);
                AddStyleTypes addStyleType = AddStyleTypes.AddPointStyle;
                switch (simpleShapeType)
                {
                    case SimpleShapeType.Unknown:
                    default:
                        addStyleType = AddStyleTypes.AddPointStyle | AddStyleTypes.AddLineStyle | AddStyleTypes.AddAreaStyle;
                        break;

                    case SimpleShapeType.Point:
                        addStyleType = AddStyleTypes.AddPointStyle;
                        break;

                    case SimpleShapeType.Line:
                        addStyleType = AddStyleTypes.AddLineStyle;
                        break;

                    case SimpleShapeType.Area:
                        addStyleType = AddStyleTypes.AddAreaStyle;
                        break;
                }

                MenuItem saveDefaultStyleMenuItem = LayerListMenuItemHelper.GetMenuItem("Set as Default Style", null, new ObservedCommand(() =>
                {
                    FeatureLayer currentFeatureLayer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as FeatureLayer;
                    if (currentFeatureLayer != null) SaveDefaultStyle(currentFeatureLayer);
                }, () => true));
                saveDefaultStyleMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorInfrastructure;component/Images/set_as_default_style_16x16.png", UriKind.RelativeOrAbsolute)) };

                MenuItem clearDefaultStyleMenuItem = LayerListMenuItemHelper.GetMenuItem("Clear the Default Style", null, new ObservedCommand(() =>
                {
                    FeatureLayer currentFeatureLayer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as FeatureLayer;
                    if (currentFeatureLayer != null) ClearDefaultStyle(currentFeatureLayer);
                }, () =>
                {
                    return featureLayer != null && File.Exists(GetDefaultStylePath(featureLayer));
                }));
                clearDefaultStyleMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorInfrastructure;component/Images/clear.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };

                menuItems.Add(LayerListMenuItemHelper.GetAddStyleMenuItem(addStyleType | AddStyleTypes.AddTextStyle, featureLayer));
                MenuItem stylingMenuItem = LayerListMenuItemHelper.GetMenuItem("Current Styling", new Image { Source = new BitmapImage(new Uri("/GisEditorInfrastructure;component/Images/current_styling_16x16.png", UriKind.RelativeOrAbsolute)) }, null);

                stylingMenuItem.Items.Add(saveDefaultStyleMenuItem);
                stylingMenuItem.Items.Add(clearDefaultStyleMenuItem);
                stylingMenuItem.Items.Add(LayerListMenuItemHelper.GetDrawingMarginMenuItem(featureLayer));

                menuItems.Add(stylingMenuItem);

                menuItems.Add(LayerListMenuItemHelper.GetExportAsCodeMenuItem());
                menuItems.Add(LayerListMenuItemHelper.GetReprojectMenuItem());
                menuItems.Add(LayerListMenuItemHelper.GetPropertiesMenuItem());

                MenuItem exportToFileMenuItem = new MenuItem();
                exportToFileMenuItem.Name = "ExportToFile";
                exportToFileMenuItem.Header = GisEditor.LanguageManager.GetStringResource("ExportToShapefile");
                exportToFileMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Export.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };

                foreach (var plugin in GisEditor.LayerManager.GetActiveLayerPlugins<FeatureLayerPlugin>().Where(p => p.CanCreateFeatureLayer))
                {
                    MenuItem menuItem = new MenuItem();
                    menuItem.Header = plugin.Name;
                    menuItem.Tag = plugin;
                    var bitmap = plugin.SmallIcon as BitmapImage;
                    if (bitmap != null)
                    {
                        menuItem.Icon = new Image
                        {
                            Source = new BitmapImage(bitmap.UriSource),
                            Width = 16,
                            Height = 16
                        };
                    }
                    menuItem.Click += ToExportFileMenuItemClick;
                    exportToFileMenuItem.Items.Add(menuItem);
                }
                //if (featureLayer.FeatureSource.LinkSources.Count > 0)
                //{
                //    MenuItem excelItem = GetExportToShpWithExcelMenuItem();
                //    exportToFileMenuItem.Items.Add(excelItem);
                //}

                menuItems.Add(LayerListMenuItemHelper.GetSaveLayerMenuItem());
                menuItems.Add(exportToFileMenuItem);
            }

            return menuItems;
        }

        private MenuItem GetExportToShpWithExcelMenuItem()
        {
            MenuItem excelItem = new MenuItem();
            excelItem.Header = "Shapefiles with Excel";
            excelItem.Click += ExportToShpWithExcelMenuItemClick;
            excelItem.Icon = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorInfrastructure;component/Images/MSExcel-50.png", UriKind.Absolute)),
                Width = 16,
                Height = 16
            };
            return excelItem;
        }

        private string GetDefaultStylePath(FeatureLayer featureLayer)
        {
            string path = string.Empty;

            Uri uri = GetUri(featureLayer);
            string keyPath = uri.OriginalString;

            if (File.Exists(indexXmlPath))
            {
                XElement xElement = XElement.Load(indexXmlPath);
                XElement resultXElement = xElement.Descendants("FeatureLayer").FirstOrDefault(x => x.FirstAttribute != null && x.FirstAttribute.Value.Equals(keyPath));
                if (resultXElement != null && resultXElement.Element(defaultStylePathElementName) != null)
                {
                    path = resultXElement.Element(defaultStylePathElementName).Value;
                }
            }

            return path;
        }

        private void ClearDefaultStyle(FeatureLayer featureLayer)
        {
            string styleFilePath = GetDefaultStylePath(featureLayer);
            if (!string.IsNullOrEmpty(styleFilePath)
                && File.Exists(styleFilePath))
            {
                try
                {
                    File.Delete(styleFilePath);
                }
                catch (Exception e)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
                }
            }
        }

        private void SaveDefaultStyle(FeatureLayer featureLayer)
        {
            if (!Directory.Exists(featureLayerStyleFolderPath)) Directory.CreateDirectory(featureLayerStyleFolderPath);
            if (!Directory.Exists(featureLayerStyleSubFolderPath)) Directory.CreateDirectory(featureLayerStyleSubFolderPath);
            if (!File.Exists(indexXmlPath))
            {
                XElement rootXElement = new XElement("Root");
                rootXElement.Save(indexXmlPath);
            }
            Uri uri = GetUri(featureLayer);
            string keyPath = uri.OriginalString;
            string defaultStyleXmlPath = Path.Combine(featureLayerStyleSubFolderPath, string.Format(xmlFileNameFormat, featureLayer.Name + "_" + uri.GetHashCode()));
            XElement rootElement = XElement.Load(indexXmlPath);
            XElement resultXElement = rootElement.Descendants("FeatureLayer").FirstOrDefault(x => x.FirstAttribute != null && x.FirstAttribute.Value.Equals(keyPath));
            try
            {
                XElement stylesXElement = new XElement("Styles");
                foreach (var zoomLevel in LayerListHelper.GetZoomLevelsAccordingToSacle(featureLayer))
                {
                    double lowerScale = GisEditor.ActiveMap.ZoomLevelSet.GetZoomLevels()[(int)zoomLevel.ApplyUntilZoomLevel - 1].Scale;
                    StyleWrapper styleWrapper = new StyleWrapper(zoomLevel.Scale, lowerScale, zoomLevel.CustomStyles.FirstOrDefault() as CompositeStyle);
                    stylesXElement.Add(styleWrapper.ToXml());
                }
                stylesXElement.Save(defaultStyleXmlPath);
                if (resultXElement == null)
                {
                    rootElement.Add(new XElement("FeatureLayer"
                        , new XAttribute("Name", keyPath)
                        , new XElement(defaultStylePathElementName, defaultStyleXmlPath)));
                }
                else
                {
                    XElement featureLayerXElement = resultXElement.Element(defaultStylePathElementName);
                    if (featureLayerXElement != null) featureLayerXElement.Value = defaultStyleXmlPath;
                    else resultXElement.Add(new XElement(defaultStylePathElementName, defaultStyleXmlPath));
                }
            }
            catch (Exception e)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
            }
            rootElement.Save(indexXmlPath);
        }

        private void LoadDefaultStyle(FeatureLayer featureLayer)
        {
            Uri uri = GetUri(featureLayer);
            string keyPath = uri.OriginalString;
            if (File.Exists(indexXmlPath))
            {
                XElement xElement = XElement.Load(indexXmlPath);
                XElement resultXElement = xElement.Descendants("FeatureLayer").FirstOrDefault(x => x.FirstAttribute != null && x.FirstAttribute.Value.Equals(keyPath));
                if (resultXElement != null && resultXElement.Element(defaultStylePathElementName) != null)
                {
                    string styleFilePath = resultXElement.Element(defaultStylePathElementName).Value;
                    if (File.Exists(styleFilePath))
                    {
                        try
                        {
                            Collection<StyleWrapper> styles = new Collection<StyleWrapper>();
                            XElement stylesXElement = XElement.Load(styleFilePath);
                            foreach (var styleXElement in stylesXElement.Descendants("Style"))
                            {
                                styles.Add(new StyleWrapper(styleXElement));
                            }
                            foreach (var zoomLevel in featureLayer.ZoomLevelSet.CustomZoomLevels)
                            {
                                zoomLevel.CustomStyles.Clear();
                                foreach (var item in styles)
                                {
                                    if (item.UpperScale >= zoomLevel.Scale && zoomLevel.Scale >= item.LowerScale)
                                    {
                                        zoomLevel.CustomStyles.Add(item.Style);
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
                        }
                    }
                }
            }
        }

        private void ExportToShpWithExcelMenuItemClick(object sender, RoutedEventArgs e)
        {
            var featureLayer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as FeatureLayer;
            if (featureLayer != null)
            {
                featureLayer.Open();

                var targetLayerPlugin = GisEditor.LayerManager.GetLayerPlugins<ShapeFileFeatureLayer>().FirstOrDefault() as FeatureLayerPlugin;
                if (targetLayerPlugin != null)
                {
                    var sourceColumns = featureLayer.FeatureSource.GetColumns(GettingColumnsType.FeatureSourceOnly);
                    var parameters = targetLayerPlugin.GetCreateFeatureLayerParameters(sourceColumns);
                    if (parameters != null)
                    {
                        Collection<string> columns = new Collection<string>();
                        if (parameters.CustomData.ContainsKey("Columns"))
                        {
                            columns = parameters.CustomData["Columns"] as Collection<string>;
                        }

                        var featureColumns = sourceColumns.Where(c => columns.Contains(c.ColumnName));
                        foreach (var item in featureColumns)
                        {
                            string columnName = item.ColumnName;
                            FeatureSourceColumn column = new FeatureSourceColumn(columnName, item.TypeName, item.MaxLength);
                            if (column.TypeName.Equals("c", StringComparison.InvariantCultureIgnoreCase))
                            {
                                column.TypeName = "Character";
                            }
                            parameters.AddedColumns.Add(column);
                        }

                        parameters.WellKnownType = featureLayer.FeatureSource.GetFirstFeaturesWellKnownType();
                        parameters.CustomData["SourceLayer"] = featureLayer;

                        Proj4Projection proj4 = new Proj4Projection();
                        proj4.InternalProjectionParametersString = parameters.Proj4ProjectionParametersString;
                        proj4.ExternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
                        proj4.SyncProjectionParametersString();
                        proj4.Open();

                        var columnNames = parameters.AddedColumns.Select(c => c.ColumnName);
                        var allFeatures = featureLayer.QueryTools.GetAllFeatures(columnNames);
                        foreach (var item in allFeatures)
                        {
                            Feature feature = proj4.ConvertToInternalProjection(item);
                            parameters.AddedFeatures.Add(feature);
                        }

                        targetLayerPlugin.CreateFeatureLayer(parameters);

                        //var tables = GetDataTables(featureLayer);
                        //if (tables.Count > 0)
                        //{
                        //    if (tables.Select(t => t.TableName).Distinct().Count() < tables.Count)
                        //    {
                        //        MessageBox.Show("There are duplicated Link Sources names.");
                        //    }
                        //    else
                        //    {
                        //        GenerateExcel(tables);
                        //    }
                        //}
                    }
                }
            }
        }

        //static Collection<DataTable> GetDataTables(FeatureLayer featureLayer)
        //{
        //    Collection<DataTable> tables = new Collection<DataTable>();

        //    featureLayer.Open();
        //    Collection<LinkSource> linkSources = GetAllLinkSources(featureLayer.FeatureSource);
        //    foreach (var linkSource in linkSources)
        //    {
        //        var linkTable = GetTable(linkSource);
        //        linkTable.TableName = linkSource.Name;
        //        tables.Add(linkTable);
        //    }

        //    return tables;
        //}

        //static Collection<LinkSource> GetAllLinkSources(FeatureSource featureSource)
        //{
        //    Collection<LinkSource> linkSources = new Collection<LinkSource>();
        //    foreach (var linkSource in featureSource.LinkSources)
        //    {
        //        linkSources.Add(linkSource);
        //        FillLinkSources(linkSource, linkSources);
        //    }

        //    return linkSources;
        //}

        //static void FillLinkSources(LinkSource linkSource, Collection<LinkSource> linkSources)
        //{
        //    foreach (var item in linkSource.LinkSources)
        //    {
        //        linkSources.Add(item);
        //        FillLinkSources(item, linkSources);
        //    };
        //}

        //static DataTable GetTable(LinkSource linkSource)
        //{
        //    DataTable dataTable = new DataTable();
        //    linkSource.Open();

        //    Dictionary<string, object> values = new Dictionary<string, object>();
        //    var columns = linkSource.GetColumns().Select(c => c.ColumnName).ToList();
        //    foreach (var item in linkSource.ColumnNamesToExclude)
        //    {
        //        if (!columns.Contains(item))
        //        {
        //            columns.Add(item);
        //        }
        //    }
        //    var dataColumns = columns.Select(tmpColumnName => new DataColumn(tmpColumnName)).ToList();
        //    dataTable.Columns.AddRange(dataColumns.ToArray());

        //    for (int i = 1; i <= linkSource.Records.Count(); i++)
        //    {
        //        try
        //        {
        //            var row = dataTable.NewRow();
        //            var columnValues = linkSource.Records.GetColumnValues(i, columns);
        //            foreach (var item in columnValues)
        //            {
        //                row[item.Key] = item.Value;
        //            }
        //            dataTable.Rows.Add(row);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex.Message);
        //        }
        //    }

        //    return dataTable;
        //}

        static DataTable GetTable(IEnumerable<Feature> features, IEnumerable<FeatureSourceColumn> featureColumns)
        {
            DataTable dataTable = new DataTable();
            if (features.Count() > 0)
            {
                List<DataColumn> columns = null; // features[0].ColumnValues.Select(v => new DataColumn(v.Key)).ToArray();

                columns = featureColumns.Select(tmpColumnName => new DataColumn(tmpColumnName.ColumnName)).ToList();

                dataTable.Columns.AddRange(columns.ToArray());

                foreach (var feature in features)
                {
                    var row = dataTable.NewRow();

                    string tempPreviousValue = string.Empty;
                    foreach (var columnValue in feature.ColumnValues)
                    {
                        if (columns.Any(c => c.ColumnName.Equals(columnValue.Key)))
                        {
                            string value = columnValue.Value;
                            row[columnValue.Key] = columnValue.Value;
                            tempPreviousValue = value;
                        }
                    }

                    dataTable.Rows.Add(row);
                }
            }
            return dataTable;
        }

        static void GenerateExcel(Collection<DataTable> tables)
        {
            Microsoft.Office.Interop.Excel.Application appexcel = new Microsoft.Office.Interop.Excel.Application();
            System.Reflection.Missing miss = System.Reflection.Missing.Value;
            appexcel = new Microsoft.Office.Interop.Excel.Application();

            //set object as invisible.
            appexcel.Visible = false;
            Microsoft.Office.Interop.Excel.Workbook workbookdata = appexcel.Workbooks.Add(miss);

            foreach (var table in tables)
            {
                Microsoft.Office.Interop.Excel.Worksheet worksheetdata = (Microsoft.Office.Interop.Excel.Worksheet)workbookdata.Worksheets.Add(miss, miss, miss, miss);

                //sheet name
                worksheetdata.Name = table.TableName;
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    worksheetdata.Cells[1, i + 1] = table.Columns[i].ColumnName.ToString();
                }

                //we write the first row, so begin with "a2"
                Microsoft.Office.Interop.Excel.Range rangedata = worksheetdata.get_Range("a2", miss);
                Microsoft.Office.Interop.Excel.Range xlrang = null;
                //irowcount is actual row count.
                int irowcount = table.Rows.Count;
                int iparstedrow = 0, icurrsize = 0;
                //ieachsize is row count.
                int ieachsize = 1000;

                //icolumnaccount is the actual column count.
                int icolumnaccount = table.Columns.Count;

                //define a ieachsizexicolumnaccount array in memory, ieachsize is the max row when every time saving, icolumnaccount is the max column when every time saving.
                object[,] objval = new object[ieachsize, icolumnaccount];
                icurrsize = ieachsize;
                while (iparstedrow < irowcount)
                {
                    if ((irowcount - iparstedrow) < ieachsize)
                        icurrsize = irowcount - iparstedrow;
                    for (int i = 0; i < icurrsize; i++)
                    {
                        for (int j = 0; j < icolumnaccount; j++)
                            objval[i, j] = table.Rows[i + iparstedrow][j].ToString();
                        System.Windows.Forms.Application.DoEvents();
                    }

                    string X = "A" + ((int)(iparstedrow + 2)).ToString();
                    string col = "";

                    if (icolumnaccount <= 26)
                    {
                        col = ((char)('A' + icolumnaccount - 1)).ToString() + ((int)(iparstedrow + icurrsize + 1)).ToString();
                    }
                    else
                    {
                        col = ((char)('A' + (icolumnaccount / 26 - 1))).ToString() + ((char)('A' + (icolumnaccount % 26 - 1))).ToString() + (iparstedrow + icurrsize + 1).ToString();
                    }

                    xlrang = worksheetdata.get_Range(X, col);
                    //call range's value2 property to assigne value in memory to excel.
                    xlrang.Value2 = objval;
                    iparstedrow = iparstedrow + icurrsize;
                }

                // save workbook
                if (xlrang != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(xlrang);
                    xlrang = null;
                }
            }
            //call method to close excel process.
            appexcel.Visible = true;
        }

        private void ToExportFileMenuItemClick(object sender, RoutedEventArgs e)
        {
            var featureLayer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as FeatureLayer;
            if (featureLayer != null)
            {
                int count = 0;
                featureLayer.SafeProcess(() =>
                {
                    count = featureLayer.QueryTools.GetCount();
                    count += featureLayer.FeatureIdsToExclude.Count;
                });

                if (count > 0)
                {
                    var targetLayerPlugin = ((MenuItem)sender).Tag as FeatureLayerPlugin;
                    FeatureLayer resultLayer = null;
                    GetLayersParameters getLayerParameters = new GetLayersParameters();

                    if (targetLayerPlugin != null)
                    {
                        lock (featureLayer)
                        {
                            featureLayer.Open();
                            var sourceColumns = featureLayer.FeatureSource.GetColumns();
                            ConfigureFeatureLayerParameters parameters = targetLayerPlugin.GetCreateFeatureLayerParameters(sourceColumns);
                            var sourceLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).OfType<FeatureLayerPlugin>().FirstOrDefault();
                            if (parameters != null && sourceLayerPlugin != null)
                            {
                                bool needColumns = false;
                                Collection<string> columns = new Collection<string>();
                                if (parameters.CustomData.ContainsKey("Columns"))
                                {
                                    columns = parameters.CustomData["Columns"] as Collection<string>;
                                }
                                else
                                {
                                    needColumns = true;
                                }

                                var featureColumns = sourceColumns.Where(c => needColumns || columns.Contains(c.ColumnName));
                                if (targetLayerPlugin.CanCreateFeatureLayerWithSourceColumns(sourceLayerPlugin))
                                {
                                    foreach (var item in featureColumns)
                                    {
                                        string columnName = item.ColumnName;
                                        FeatureSourceColumn column = new FeatureSourceColumn(columnName, item.TypeName, item.MaxLength);
                                        if (column.TypeName.Equals("c", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            column.TypeName = "Character";
                                        }
                                        parameters.AddedColumns.Add(column);
                                    }
                                }
                                else
                                {
                                    var geoColumns = sourceLayerPlugin.GetIntermediateColumns(featureColumns);
                                    foreach (var item in geoColumns)
                                    {
                                        if (item.TypeName.Equals("c", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            item.TypeName = "Character";
                                        }
                                        parameters.AddedColumns.Add(item);
                                    }
                                }

                                parameters.WellKnownType = featureLayer.FeatureSource.GetFirstFeaturesWellKnownType();
                                parameters.CustomData["SourceLayer"] = featureLayer;

                                getLayerParameters.LayerUris.Add(parameters.LayerUri);
                                foreach (var item in parameters.CustomData)
                                {
                                    getLayerParameters.CustomData[item.Key] = item.Value;
                                }

                                Proj4Projection proj4 = new Proj4Projection();
                                proj4.InternalProjectionParametersString = parameters.Proj4ProjectionParametersString;
                                proj4.ExternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
                                proj4.SyncProjectionParametersString();
                                proj4.Open();

                                foreach (var item in featureLayer.QueryTools.GetAllFeatures(ReturningColumnsType.AllColumns))
                                {
                                    Feature feature = proj4.ConvertToInternalProjection(item);
                                    parameters.AddedFeatures.Add(feature);
                                }

                                if (parameters.MemoColumnConvertMode == MemoColumnConvertMode.ToCharacter)
                                {
                                    foreach (var item in parameters.AddedColumns.Where(c => c.TypeName.Equals("Memo", StringComparison.InvariantCultureIgnoreCase)).ToList())
                                    {
                                        item.TypeName = "Character";
                                        item.MaxLength = 254;
                                        DbfColumn tmpDbfColumn = item as DbfColumn;
                                        if (tmpDbfColumn != null)
                                        {
                                            tmpDbfColumn.ColumnType = DbfColumnType.Character;
                                            tmpDbfColumn.Length = 254;
                                        }
                                    }
                                }
                                resultLayer = targetLayerPlugin.CreateFeatureLayer(parameters);

                                resultLayer.FeatureSource.Projection = proj4;
                                resultLayer = targetLayerPlugin.GetLayers(getLayerParameters).FirstOrDefault() as FeatureLayer;
                            }
                        }
                    }

                    if (resultLayer != null)
                    {
                        GisEditorMessageBox messageBox = new GisEditorMessageBox(MessageBoxButton.YesNo);
                        messageBox.Owner = Application.Current.MainWindow;
                        messageBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        messageBox.Title = GisEditor.LanguageManager.GetStringResource("NavigatePluginAddToMap");
                        messageBox.Message = GisEditor.LanguageManager.GetStringResource("DoYouWantToAddToMap");
                        messageBox.ErrorMessage = string.Empty;
                        if (messageBox.ShowDialog().Value)
                        {
                            GisEditor.ActiveMap.AddLayerToActiveOverlay(resultLayer);
                            GisEditor.ActiveMap.RefreshActiveOverlay();
                            RefreshArgs refreshArgs = new RefreshArgs(this, RefreshArgsDescriptions.LoadToMapCoreDescription);
                            GisEditor.UIManager.InvokeRefreshPlugins(refreshArgs);
                            GisEditor.ActiveMap.Refresh();
                        }
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("There's no features in this layer.", "Export File");
                }
            }
        }

        private void LayerListItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var currentLayerListItem = sender as LayerListItem;
            if (e.PropertyName == "IsChecked" && !currentLayerListItem.IsChecked)
            {
                RefreshInteractiveOverlays(currentLayerListItem);
            }
        }

        private static void RefreshInteractiveOverlays(LayerListItem layerListItem)
        {
            var editOverlay = GisEditor.ActiveMap.InteractiveOverlays
                .OfType<GisEditorEditInteractiveOverlay>().FirstOrDefault();

            if (editOverlay != null)
            {
                if (layerListItem.ConcreteObject is FeatureLayer && editOverlay.EditTargetLayer == layerListItem.ConcreteObject)
                {
                    ClearEditOverlay(editOverlay);
                }
                else if (layerListItem.ConcreteObject is LayerOverlay && ((LayerOverlay)layerListItem.ConcreteObject).Layers.Contains((Layer)editOverlay.EditTargetLayer))
                {
                    ClearEditOverlay(editOverlay);
                }
            }

            if (layerListItem.ConcreteObject is LayerOverlay)
            {
                foreach (var item in ((LayerOverlay)layerListItem.ConcreteObject).Layers.OfType<FeatureLayer>())
                {
                    GisEditor.SelectionManager.ClearSelectedFeatures(item);
                }
            }
            else if (layerListItem.ConcreteObject is FeatureLayer)
            {
                GisEditor.SelectionManager.ClearSelectedFeatures((FeatureLayer)layerListItem.ConcreteObject);
            }
        }

        private static void ClearEditOverlay(GisEditorEditInteractiveOverlay editOverlay)
        {
            editOverlay.EditTargetLayer.FeatureIdsToExclude.Clear();

            editOverlay.EditShapesLayer.InternalFeatures.Clear();
            editOverlay.AssociateControlPointsLayer.InternalFeatures.Clear();
            editOverlay.ReshapeControlPointsLayer.InternalFeatures.Clear();

            editOverlay.EditShapesLayer.BuildIndex();
            editOverlay.AssociateControlPointsLayer.BuildIndex();
            editOverlay.ReshapeControlPointsLayer.BuildIndex();

            GisEditor.ActiveMap.Refresh(editOverlay);
        }

        private void ApplyStyles(IEnumerable<FeatureLayer> layers)
        {
            if (GisEditor.ActiveMap != null)
            {
                foreach (var featureLayer in layers)
                {
                    List<ZoomLevel> defaultZoomLevels = GisEditor.ActiveMap.ZoomLevelSet.GetZoomLevels().Where(z => z.GetType() == typeof(ZoomLevel)).ToList();
                    if (featureLayer.ZoomLevelSet.CustomZoomLevels.Count != defaultZoomLevels.Count
                        || !featureLayer.ZoomLevelSet.CustomZoomLevels.Any(z => z.CustomStyles.Any(s => s is CompositeStyle)))
                    {
                        featureLayer.ZoomLevelSet.CustomZoomLevels.Clear();
                        for (int i = 0; i < defaultZoomLevels.Count; i++)
                        {
                            featureLayer.ZoomLevelSet.CustomZoomLevels.Add(new ZoomLevel(defaultZoomLevels[i].Scale));
                        }

                        ApplyStyles(featureLayer, this);
                        featureLayer.DrawingQuality = DrawingQuality.CanvasSettings;
                    }
                }
            }
        }

        private static void ApplyStyles(FeatureLayer featureLayer, FeatureLayerPlugin featureLayerPlugin)
        {
            Styles.Style shapeStyle = null;
            var simpleShapeType = featureLayerPlugin.GetFeatureSimpleShapeType(featureLayer);
            StylePlugin stylePlugin = null;
            switch (simpleShapeType)
            {
                case SimpleShapeType.Point:
                    stylePlugin = GisEditor.StyleManager.GetDefaultStylePlugin(StyleCategories.Point);
                    break;

                case SimpleShapeType.Area:
                    stylePlugin = GisEditor.StyleManager.GetDefaultStylePlugin(StyleCategories.Area);
                    break;

                case SimpleShapeType.Line:
                    stylePlugin = GisEditor.StyleManager.GetDefaultStylePlugin(StyleCategories.Line);
                    break;

                default: break;
            }

            CompositeStyle componentStyle = new CompositeStyle();
            componentStyle.Name = featureLayer.Name;
            if (stylePlugin != null)
            {
                shapeStyle = stylePlugin.GetDefaultStyle().CloneDeep();
                stylePlugin.StyleCandidatesIndex++;
                if (string.IsNullOrEmpty(shapeStyle.Name))
                {
                    shapeStyle.Name = stylePlugin.Name;
                }
                componentStyle.Styles.Add(shapeStyle);
            }
            else
            {
                StylePlugin pointStylePlugin = GisEditor.StyleManager.GetDefaultStylePlugin(StyleCategories.Point);
                StylePlugin lineStylePlugin = GisEditor.StyleManager.GetDefaultStylePlugin(StyleCategories.Line);
                StylePlugin areaStylePlugin = GisEditor.StyleManager.GetDefaultStylePlugin(StyleCategories.Area);

                Styles.Style pointStyle = pointStylePlugin.GetDefaultStyle();
                Styles.Style lineStyle = lineStylePlugin.GetDefaultStyle();
                Styles.Style areaStyle = areaStylePlugin.GetDefaultStyle();
                pointStylePlugin.StyleCandidatesIndex++;
                lineStylePlugin.StyleCandidatesIndex++;
                areaStylePlugin.StyleCandidatesIndex++;

                pointStyle.Name = pointStylePlugin.Name;
                lineStyle.Name = lineStylePlugin.Name;
                areaStyle.Name = areaStylePlugin.Name;

                componentStyle.Name = featureLayer.Name;
                componentStyle.Styles.Add(areaStyle);
                componentStyle.Styles.Add(lineStyle);
                componentStyle.Styles.Add(pointStyle);
            }

            foreach (var zoomLevel in featureLayer.ZoomLevelSet.CustomZoomLevels)
            {
                zoomLevel.CustomStyles.Add(componentStyle);
            }
        }

        private void ApplyProjection(IEnumerable<FeatureLayer> layers)
        {
            SetInternalProjections(layers);
            SetExternalProjections(layers);
        }

        private static bool HasTextStyleOnZoomLevel(ZoomLevel zoomLevel)
        {
            return zoomLevel.CustomStyles.OfType<CompositeStyle>().Any(comStyle => comStyle.Styles.Any(style => style is IconTextStyle || style is TextFilterStyle));
        }

        private static bool HasPointStyleOnZoomLevel(ZoomLevel zoomLevel)
        {
            return zoomLevel.CustomStyles.OfType<CompositeStyle>().Any(comStyle => comStyle.Styles.Any(style => style is PointStyle || style is DotDensityStyle));
        }
    }
}