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
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class LegendImporterViewModel : ViewModelBase
    {
        private const int iconSize = 12;
        [NonSerialized]
        private ObservableCollection<LegendImporterItemViewModel> legendImporterItems;
        private ObservableCollection<GisEditorWpfMap> allMaps;
        private GisEditorWpfMap selectedMap;

        public LegendImporterViewModel()
        {
            legendImporterItems = new ObservableCollection<LegendImporterItemViewModel>();
            allMaps = new ObservableCollection<GisEditorWpfMap>();

            foreach (var item in LegendImporterItems)
            {
                item.PropertyChanged += new PropertyChangedEventHandler(LegendItemPropertyChanged);
            }

            foreach (var item in GisEditor.DockWindowManager.DocumentWindows)
            {
                allMaps.Add((GisEditorWpfMap)item.Content);
            }

            if (GisEditor.ActiveMap != null) SelectedMap = GisEditor.ActiveMap;
        }

        public ObservableCollection<LegendImporterItemViewModel> LegendImporterItems
        {
            get { return legendImporterItems; }
        }

        public ObservableCollection<GisEditorWpfMap> AllMaps
        {
            get { return allMaps; }
        }

        public GisEditorWpfMap SelectedMap
        {
            get { return selectedMap; }
            set
            {
                selectedMap = value;
                if (selectedMap != null) UpdateItems(selectedMap);
                RaisePropertyChanged(()=>SelectedMap);
            }
        }

        public bool CheckAll
        {
            get { return LegendImporterItems.All(item => item.AllowToAdd); }
            set { LegendImporterItems.ForEach(item => item.AllowToAdd = value); }
        }

        public IEnumerable<LegendItemViewModel> CollectLegendItems()
        {
            foreach (LegendImporterItemViewModel item in LegendImporterItems.Where(tmpItem => ValidateLegendItem(tmpItem)))
            {
                LegendItemViewModel legendItem = new LegendItemViewModel
                {
                    Text = item.Text,
                    ImageStyle = item.Style,
                    ImageWidth = iconSize,
                    ImageHeight = iconSize,
                    ImageLeftPadding = 5,
                    TopPadding = 3,
                    TextLeftPadding = 10,
                    NotifiedGeoFont = new GeoFontViewModel { FontSize = 8, FontName = "Arial" },
                };

                yield return legendItem;
            }
        }

        private bool ValidateLegendItem(LegendImporterItemViewModel tmpItem)
        {
            return tmpItem.Style != null
                && tmpItem.AllowToAdd
                && !(tmpItem.Style is CompositeStyle)
                && !(tmpItem.Style is ValueStyle)
                && !(tmpItem.Style is ClassBreakStyle)
                //&& !(tmpItem.Style is FilterStyle) 
                && !(tmpItem.Style is RegexStyle);
        }

        private void LegendItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AllowToAdd")
            {
                RaisePropertyChanged(()=>CheckAll);
            }
        }

        private void UpdateItems(GisEditorWpfMap map)
        {
            LegendImporterItems.Clear();
            var featureLayers = map.GetFeatureLayers(true);
            foreach (var featureLayer in featureLayers)
            {
                LegendImporterItems.Add(GenerateLayerItem(featureLayer));
                if (featureLayer.ZoomLevelSet.CustomZoomLevels.Count > 0)
                {
                    var featureLayerListItem = GisEditor.LayerManager.GetLayerListItem(featureLayer);
                    if (featureLayerListItem != null)
                    {
                        if (featureLayerListItem.Load != null)
                        {
                            featureLayerListItem.Load();
                            featureLayerListItem.Load = null;
                        }
                        foreach (var ComponentStyle in featureLayerListItem.Children)
                        {
                            if (ComponentStyle.Children.Count > 1)
                            {
                                LegendImporterItems.Add(GenerateComponentSytleItem(ComponentStyle));

                                GenerateImporterItems(ComponentStyle, LegendImporterItems, 2);
                            }
                            else
                            {
                                GenerateImporterItems(ComponentStyle, LegendImporterItems, 1);
                            }
                        }
                    }
                }
            }
        }

        private void GenerateImporterItems(LayerListItem ComponentStyle, Collection<LegendImporterItemViewModel> LegendImporterItems, int level)
        {
            foreach (var enetity in ComponentStyle.Children)
            {
                var style = enetity.ConcreteObject as Styles.Style;
                if (style != null)
                {
                    foreach (var tmpItem in GenerateStyleItems(style, level))
                    {
                        LegendImporterItems.Add(tmpItem);
                    }
                }
            }
        }

        private LegendImporterItemViewModel GenerateLayerItem(FeatureLayer featureLayer)
        {
            SimpleShapeType shpType = SimpleShapeType.Area;
            var featureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
            if (featureLayerPlugin != null) featureLayerPlugin.GetFeatureSimpleShapeType(featureLayer);

            ImageSource imageSource = GetIconSource("pointShp.png");
            switch (shpType)
            {
                case SimpleShapeType.Line:
                    imageSource = GetIconSource("lineShp.png");
                    break;
                case SimpleShapeType.Area:
                    imageSource = GetIconSource("areaShp.png");
                    break;
                default:
                    break;
            }

            LegendImporterItemViewModel layerLegendImporterItem = new LegendImporterItemViewModel
            {
                Text = featureLayer.Name,
                IconSource = imageSource,
                Level = 0
            };
            layerLegendImporterItem.PropertyChanged += LayerLegendImporterItemPropertyChanged;


            return layerLegendImporterItem;
        }

        private void LayerLegendImporterItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            LegendImporterItemViewModel itemViewModel = sender as LegendImporterItemViewModel;
            if (e.PropertyName.Equals("AllowToAdd") && itemViewModel != null)
            {
                var indexOf = legendImporterItems.IndexOf(itemViewModel);
                for (int i = indexOf + 1; i < legendImporterItems.Count; i++)
                {
                    if (legendImporterItems[i].Level > itemViewModel.Level)
                    {
                        legendImporterItems[i].AllowToAdd = itemViewModel.AllowToAdd;
                    }
                    else break;
                }
            }
        }

        private LegendImporterItemViewModel GenerateComponentSytleItem(LayerListItem layerListItem)
        {
            var style = layerListItem.ConcreteObject as CompositeStyle;
            var zoomLevelImporterItem = new LegendImporterItemViewModel();
            zoomLevelImporterItem.PropertyChanged += LayerLegendImporterItemPropertyChanged;
            zoomLevelImporterItem.Text = layerListItem.Name;
            var styleItem = layerListItem as StyleLayerListItem;
            if (styleItem != null) zoomLevelImporterItem.Text += styleItem.ZoomLevelRange;

            zoomLevelImporterItem.IconSource = layerListItem.PreviewImage.Source;
            zoomLevelImporterItem.CheckBoxVisibility = Visibility.Visible;
            zoomLevelImporterItem.Style = style;
            zoomLevelImporterItem.Level = 1;
            return zoomLevelImporterItem;
        }

        private static BitmapImage GetIconSource(string iconName)
        {
            return new BitmapImage(new Uri(String.Format(CultureInfo.InvariantCulture, "/GisEditorPluginCore;component/Images/{0}", iconName), UriKind.RelativeOrAbsolute));
        }

        private IEnumerable<LegendImporterItemViewModel> GenerateStyleItems(Styles.Style style, int level)
        {
            if (style is AreaStyle || style is PointStyle || style is LineStyle)
            {
                LegendImporterItemViewModel styleLegendItem = new LegendImporterItemViewModel
                {
                    Text = style.Name,
                    CheckBoxVisibility = Visibility.Visible,
                    Level = level,
                    LeftPaddingLevel = level,
                    Style = style
                };

                using (Bitmap nativeImage = new Bitmap(iconSize, iconSize))
                {
                    var geoCanvas = new PlatformGeoCanvas();
                    var drawingShape = new RectangleShape(-10, 10, 10, -10);
                    geoCanvas.BeginDrawing(nativeImage, drawingShape, GeographyUnit.Meter);
                    style.DrawSample(geoCanvas, new DrawingRectangleF(geoCanvas.Width * .5f, geoCanvas.Height * .5f, geoCanvas.Width, geoCanvas.Height));
                    geoCanvas.EndDrawing();

                    var streamSource = new MemoryStream();
                    nativeImage.Save(streamSource, ImageFormat.Png);

                    var imageSource = new BitmapImage();
                    imageSource.BeginInit();
                    imageSource.StreamSource = streamSource;
                    imageSource.EndInit();
                    imageSource.Freeze();

                    styleLegendItem.IconSource = imageSource;
                }

                yield return styleLegendItem;

            }
            else if (style is ClassBreakStyle || style is RegexStyle || style is ValueStyle || style is FilterStyle)
            {
                var clonedStyle = style.CloneDeep();
                var classBreakStyle = clonedStyle as ClassBreakStyle;
                var regexStyle = clonedStyle as RegexStyle;
                var valueStyle = clonedStyle as ValueStyle;
                var filterStyle = clonedStyle as FilterStyle;
                IEnumerable<Styles.Style> subStyles = new Collection<Styles.Style>();
                if (classBreakStyle != null)
                    subStyles = classBreakStyle.ClassBreaks.SelectMany(classBreakItem =>
                    {
                        foreach (var item in classBreakItem.CustomStyles)
                        {
                            item.Name = ClassBreakSubItem.GetClassBreakStyleName(classBreakItem.Value);
                        }
                        return classBreakItem.CustomStyles;
                    });
                else if (regexStyle != null)
                    subStyles = regexStyle.RegexItems.SelectMany(regexItem => regexItem.CustomStyles);
                else if (filterStyle != null)
                    subStyles = new Collection<Styles.Style>();//filterStyle.Styles;
                else
                    subStyles = valueStyle.ValueItems.SelectMany(valueItem =>
                    {
                        foreach (var item in valueItem.CustomStyles)
                        {
                            item.Name = valueItem.Value;
                        }
                        return valueItem.CustomStyles;
                    });

                LegendImporterItemViewModel styleLegendItem = new LegendImporterItemViewModel
                {
                    Text = style.Name,
                    CheckBoxVisibility = Visibility.Visible,
                    Level = level,
                    Style = style
                };
                styleLegendItem.PropertyChanged += LayerLegendImporterItemPropertyChanged;

                using (Bitmap nativeImage = new Bitmap(iconSize, iconSize))
                {
                    var geoCanvas = new PlatformGeoCanvas();
                    var drawingShape = new RectangleShape(-10, 10, 10, -10);
                    geoCanvas.BeginDrawing(nativeImage, drawingShape, GeographyUnit.Meter);
                    style.DrawSample(geoCanvas, new DrawingRectangleF(geoCanvas.Width * .5f, geoCanvas.Height * .5f, geoCanvas.Width, geoCanvas.Height));
                    geoCanvas.EndDrawing();

                    var streamSource = new MemoryStream();
                    nativeImage.Save(streamSource, ImageFormat.Png);
                    BitmapImage imageSource = new BitmapImage();
                    imageSource.BeginInit();
                    imageSource.StreamSource = streamSource;
                    imageSource.EndInit();
                    imageSource.Freeze();

                    styleLegendItem.IconSource = imageSource;
                }

                yield return styleLegendItem;

                foreach (var itemStyle in subStyles)
                {
                    foreach (var tmpItem in GenerateStyleItems(itemStyle, level + 3))
                        yield return tmpItem;
                }
            }
            else
            {
                var componentStyle = style as CompositeStyle;
                if (componentStyle != null)
                {
                    if (componentStyle.Styles.Count > 1)
                    {
                        LegendImporterItemViewModel styleLegendItem = new LegendImporterItemViewModel
                        {
                            Text = style.Name,
                            CheckBoxVisibility = Visibility.Visible,
                            Level = level,
                            IconSource = new BitmapImage()
                        };

                        yield return styleLegendItem;

                        foreach (var innerStyle in componentStyle.Styles.Reverse())
                        {
                            foreach (var tmpItem in GenerateStyleItems(innerStyle, level + 3))
                                yield return tmpItem;
                        }
                    }
                    else
                    {
                        foreach (var tmpItem in GenerateStyleItems(componentStyle.Styles.FirstOrDefault(), level))
                            yield return tmpItem;
                    }
                }
                else
                {
                    LegendImporterItemViewModel styleLegendItem = new LegendImporterItemViewModel
                    {
                        Text = style.Name,
                        CheckBoxVisibility = Visibility.Collapsed,
                        Level = level,
                        IconSource = new BitmapImage()
                    };

                    yield return styleLegendItem;
                }
            }
        }
    }
}
