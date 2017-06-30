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


using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class is the base class of all LayerPlugins.
    /// This plugin provides the functionality of creating a specific type of layer instance.
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(LayerPlugin))]
    public abstract class LayerPlugin : Plugin
    {
        private static LinearGradientBrush defaultBackground;

        [NonSerialized]
        private OpenFileDialog openFileDialog;

        [Obfuscation]
        private string extensionFilter;

        [Obfuscation]
        private bool canResolveDataSource;

        [Obfuscation]
        [NonSerialized]
        private SearchPlaceTool searchPlaceTool;

        [Obfuscation]
        [NonSerialized]
        private DataSourceResolveTool dataSourceResolveTool;

        public event EventHandler<GettingLayersLayerPluginEventArgs> GettingLayers;

        public event EventHandler<GottenLayersLayerPluginEventArgs> GottenLayers;

        public static event EventHandler<GettingLayerPreviewSourceLayerPluginEventArgs> GettingLayerPreviewSource;

        public static event EventHandler<GottenLayerPreviewSourceLayerPluginEventArgs> GottenLayerPreviewSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayerPlugin" /> class.
        /// </summary>
        protected LayerPlugin()
        {
            ExtensionFilterCore = string.Empty;
            searchPlaceTool = new SearchPlaceTool();
            dataSourceResolveTool = new DataSourceResolveTool(ExtensionFilter);
        }

        /// <summary>
        /// Gets or sets the extension filter that allows to create a specific layer.
        /// </summary>
        /// <value>
        /// The extension filter to create by this plugin.
        /// </value>
        public string ExtensionFilter
        {
            get { return ExtensionFilterCore; }
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual string ExtensionFilterCore
        {
            get { return extensionFilter; }
            set { extensionFilter = value; }
        }

        public SearchPlaceTool SearchPlaceTool
        {
            get { return SearchPlaceToolCore; }
        }

        protected virtual SearchPlaceTool SearchPlaceToolCore
        {
            get { return searchPlaceTool; }
            set { searchPlaceTool = value; }
        }

        public DataSourceResolveTool DataSourceResolveTool
        {
            get { return DataSourceResolveToolCore; }
        }

        protected virtual DataSourceResolveTool DataSourceResolveToolCore
        {
            get { return dataSourceResolveTool; }
            set { dataSourceResolveTool = value; }
        }

        /// <summary>
        /// Gets the type of the layer.
        /// GisEditor will use this method to find a match plugin to create a specific layer instance.
        /// </summary>
        /// <returns>A type of subclasses that inherit from ThinkGeo.MapSuite.Layer.</returns>
        public Type GetLayerType()
        {
            return GetLayerTypeCore();
        }

        /// <summary>
        /// Gets the type of the layer.
        /// GisEditor will use this method to find a match plugin to create a specific layer instance.
        /// This is the core method of <code>public Type GetLayerType()</code> for override.
        /// </summary>
        /// <returns>A type of subclasses that inherit from ThinkGeo.MapSuite.Layer.</returns>
        protected abstract Type GetLayerTypeCore();

        /// <summary>
        /// This method creates layers instance.
        /// In this method, it might popup a dialog to configuration.
        /// For example, a ShapeFileFeatureLayer is based on a shapefile, we need popup an OpenFileDialog to choose a file.
        /// Or MSSql layer needs a connection string to connect to the server, etc.
        /// </summary>
        /// <returns>A set of specific layers that created by this plugin.</returns>
        public Collection<Layer> GetLayers()
        {
            Collection<Layer> layers = GetLayers(new GetLayersParameters());
            return layers;
        }

        public Collection<Layer> GetLayers(GetLayersParameters getLayersParameters)
        {
            if (getLayersParameters == null)
            {
                getLayersParameters = new GetLayersParameters();
            }

            GettingLayersLayerPluginEventArgs gettingLayersEventArgs = new GettingLayersLayerPluginEventArgs(getLayersParameters);
            OnGettingLayers(gettingLayersEventArgs);
            getLayersParameters = gettingLayersEventArgs.Parameters;

            Collection<Layer> layers = GetLayersCore(getLayersParameters);

            GottenLayersLayerPluginEventArgs gotLayersEventArgs = new GottenLayersLayerPluginEventArgs(layers, getLayersParameters);
            OnGottenLayers(gotLayersEventArgs);
            return gotLayersEventArgs.Layers;
        }

        /// <summary>
        /// This method creates layers instance.
        /// In this method, it might popup a dialog to configuration.
        /// For example, a ShapeFileFeatureLayer is based on a shapefile, we need popup an OpenFileDialog to choose a file.
        /// Or MSSql layer needs a connection string to connect to the server, etc.
        /// </summary>
        /// <returns>A set of specific layers that created by this plugin.</returns>
        protected virtual Collection<Layer> GetLayersCore(GetLayersParameters getLayersParameters)
        {
            if (!String.IsNullOrEmpty(ExtensionFilter) && (getLayersParameters == null || getLayersParameters.LayerUris.Count == 0))
            {
                if (openFileDialog == null)
                {
                    openFileDialog = new OpenFileDialog();
                    openFileDialog.Multiselect = true;
                    openFileDialog.Filter = ExtensionFilter;
                }

                if (openFileDialog.ShowDialog().GetValueOrDefault())
                {
                    foreach (string fileName in openFileDialog.FileNames)
                    {
                        getLayersParameters.LayerUris.Add(new Uri(fileName));
                    }
                }
            }

            return new Collection<Layer>();
        }

        /// <summary>
        /// This method gets a hierarchy object to build the layer list tree.
        /// </summary>
        /// <param name="layer">The layer list item to build the layer list tree.</param>
        /// <returns></returns>
        public LayerListItem GetLayerListItem(Layer layer)
        {
            return GetLayerListItemCore(layer);
        }

        /// <summary>
        /// This method gets a hierarchy object to build the layer list tree.
        /// </summary>
        /// <param name="layer">The layer list item to build the layer list tree.</param>
        /// <returns></returns>
        protected virtual LayerListItem GetLayerListItemCore(Layer layer)
        {
            var layerListItem = new LayerListItem(layer);
            layerListItem.SideImage = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorInfrastructure;component/Images/arrowDown.png", UriKind.Absolute)) };
            layerListItem.HighlightBackgroundBrush = GetDefaultLayerBackground();
            layerListItem.ConcreteObject = layer;
            layerListItem.CheckBoxVisibility = Visibility.Visible;
            layerListItem.IsChecked = layer.IsVisible;
            layerListItem.Name = layer.Name;
            layerListItem.PropertyChanged += LayerIsVisiblePropertyChanged;
            SetStyleSampleImage(layer, layerListItem);
            return layerListItem;
        }

        /// <summary>
        /// Gets an UI that holds information of the pass layer.
        /// </summary>
        /// <param name="layer">The layer that to build the UI.</param>
        /// <returns>An UI that holds information of the pass layer</returns>
        public UserControl GetPropertiesUI(Layer layer)
        {
            return GetPropertiesUICore(layer);
        }

        /// <summary>
        /// Gets an UI that holds information of the pass layer.
        /// </summary>
        /// <param name="layer">The layer that to build the UI.</param>
        /// <returns>An UI that holds information of the pass layer</returns>
        protected virtual UserControl GetPropertiesUICore(Layer layer)
        {
            LayerPropertiesUserControl metadataUserControl = new LayerPropertiesUserControl(layer);
            return metadataUserControl;
        }

        /// <summary>
        /// Gets the name of passed layer
        /// </summary>
        /// <param name="layer">The layer to get path file name.</param>
        /// <returns>The path file name of this layer.</returns>
        public Uri GetUri(Layer layer)
        {
            return GetUriCore(layer);
        }

        /// <summary>
        /// Gets the name of passed layer
        /// </summary>
        /// <param name="layer">The layer to get path file name.</param>
        /// <returns>The path file name of this layer.</returns>
        protected abstract Uri GetUriCore(Layer layer);

        [Obsolete("This method is obsoleted, please call DataSourceResolver.IsDataSourceAvailable(Layer) instead.")]
        public bool IsDataSourceAvailable(Layer layer)
        {
            return DataSourceResolveTool.IsDataSourceAvailable(layer);
        }

        [Obsolete("This method is obsoleted, please call DataSourceResolver.IsDataSourceAvailable(Layer) instead.")]
        protected virtual bool IsDataSourceAvailableCore(Layer layer)
        {
            return DataSourceResolveTool.IsDataSourceAvailable(layer);
        }

        [Obsolete("This method is obsoleted, please call DataSourceResolver.IsDataSourceAvailable(Layer) instead.")]
        public void ResolveDataSource(Layer layer)
        {
            DataSourceResolveTool.ResolveDataSource(layer);
        }

        [Obsolete("This method is obsoleted, please call DataSourceResolver.IsDataSourceAvailable(Layer) instead.")]
        protected virtual void ResolveDataSourceCore(Layer layer)
        {
            DataSourceResolveTool.ResolveDataSource(layer);
        }

        protected virtual void OnGettingLayers(GettingLayersLayerPluginEventArgs e)
        {
            EventHandler<GettingLayersLayerPluginEventArgs> handler = GettingLayers;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnGottenLayers(GottenLayersLayerPluginEventArgs e)
        {
            EventHandler<GottenLayersLayerPluginEventArgs> handler = GottenLayers;
            if (handler != null) handler(this, e);
        }

        public Collection<MenuItem> GetLayerListItemContextMenuItems(GetLayerListItemContextMenuParameters parameters)
        {
            Collection<MenuItem> menuItems = new Collection<MenuItem>();

            menuItems.Add(LayerListMenuItemHelper.GetMovementMenuItem(MovementAction.Up));
            menuItems.Add(LayerListMenuItemHelper.GetMovementMenuItem(MovementAction.Down));
            menuItems.Add(LayerListMenuItemHelper.GetMovementMenuItem(MovementAction.ToTop));
            menuItems.Add(LayerListMenuItemHelper.GetMovementMenuItem(MovementAction.ToBottom));

            //menuItems.Add(new MenuItem() { Header = "--" });
            menuItems.Add(LayerListMenuItemHelper.GetZoomToExtentMenuItem());
            menuItems.Add(LayerListMenuItemHelper.GetRenameMenuItem());
            menuItems.Add(LayerListMenuItemHelper.GetRemoveLayerMenuItem());

            //menuItems.Add(new MenuItem() { Header = "--" });

            float transparency = 255;
            RasterLayer rasterLayer = parameters.LayerListItem.ConcreteObject as RasterLayer;
            if (rasterLayer != null) transparency = rasterLayer.Transparency;
            else
            {
                transparency = ((Layer)parameters.LayerListItem.ConcreteObject).Transparency;
            }

            menuItems.Add(LayerListMenuItemHelper.GetTransparencyMenuItem(
                transparency));
            Collection<MenuItem> newMenuItems = GetLayerListItemContextMenuItemsCore(parameters);
            foreach (MenuItem menuItem in newMenuItems)
            {
                menuItems.Add(menuItem);
            }

            return menuItems;
        }

        protected virtual Collection<MenuItem> GetLayerListItemContextMenuItemsCore(GetLayerListItemContextMenuParameters parameters)
        {
            return new Collection<MenuItem>();
        }

        public ImageSource GetLayerPreviewSource(Layer layer)
        {
            ImageSource imageSource = null;

            GettingLayerPreviewSourceLayerPluginEventArgs arg1 = new GettingLayerPreviewSourceLayerPluginEventArgs(null, layer);
            OnGettingLayerPreview(arg1);

            if (arg1.Cancel)
            {
                imageSource = arg1.ImageSource ?? new BitmapImage();
            }
            else
            {
                imageSource = GetLayerPreviewSourceCore(layer);

                GottenLayerPreviewSourceLayerPluginEventArgs arg2 = new GottenLayerPreviewSourceLayerPluginEventArgs(imageSource, layer);
                OnGottenLayerPreview(arg2);

                if (arg2 != null)
                {
                    imageSource = arg2.ImageSource;
                }
            }

            return imageSource;
        }

        protected virtual ImageSource GetLayerPreviewSourceCore(Layer layer)
        {
            ImageSource imageSource = null;
            FeatureLayer featureLayer = layer as FeatureLayer;
            if (featureLayer != null && GisEditor.ActiveMap != null)
            {
                var zoomLevel = featureLayer.ZoomLevelSet.CustomZoomLevels.OrderBy(z => Math.Abs(z.Scale - GisEditor.ActiveMap.CurrentScale)).FirstOrDefault();
                if (zoomLevel != null && zoomLevel.CustomStyles.Count > 0 && zoomLevel.CustomStyles.Count > 0)
                {
                    BitmapSource bitmapSource = new BitmapImage();
                    var style = zoomLevel.CustomStyles.LastOrDefault(l =>
                    {
                        bool isActive = l != null;
                        if (isActive)
                        {
                            isActive = (l is CompositeStyle) && ((CompositeStyle)l).Styles.Any(s => s is AreaStyle || s is LineStyle || s is PointStyle);
                        }
                        return isActive;
                    });

                    if (style == null)
                    {
                        style = zoomLevel.CustomStyles.LastOrDefault(l =>
                        {
                            bool isActive = l != null;
                            if (isActive)
                            {
                                isActive = (l is CompositeStyle) && ((CompositeStyle)l).Styles.Any(s => !(s is AreaStyle || s is LineStyle || s is PointStyle));
                            }
                            return isActive;
                        });
                    }

                    if (style != null)
                    {
                        var styleItem = GisEditor.StyleManager.GetStyleLayerListItem(style);
                        if (styleItem != null) bitmapSource = styleItem.GetPreviewSource(23, 23);
                    }
                    imageSource = bitmapSource;
                }
                else
                {
                    var disableIconSource = new BitmapImage(new Uri("pack://application:,,,/GisEditorInfrastructure;component/Images/Unavailable.png", UriKind.RelativeOrAbsolute));
                    imageSource = disableIconSource;
                }
            }
            else
            {
                var layerSample = new BitmapImage(new Uri("/GisEditorInfrastructure;component/Images/dr_fileicon_raster.png", UriKind.RelativeOrAbsolute));
                imageSource = layerSample;
            }

            return imageSource;
        }

        protected virtual void OnGettingLayerPreview(GettingLayerPreviewSourceLayerPluginEventArgs e)
        {
            EventHandler<GettingLayerPreviewSourceLayerPluginEventArgs> handler = GettingLayerPreviewSource;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnGottenLayerPreview(GottenLayerPreviewSourceLayerPluginEventArgs e)
        {
            EventHandler<GottenLayerPreviewSourceLayerPluginEventArgs> handler = GottenLayerPreviewSource;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void SetStyleSampleImage(Layer layer, LayerListItem layerListItem)
        {
            ImageSource imageSource = GetLayerPreviewSource(layer);
            if (imageSource != null)
            {
                layerListItem.PreviewImage = new Image { Source = imageSource, Stretch = Stretch.None };
            }
        }

        private static LinearGradientBrush GetDefaultLayerBackground()
        {
            if (defaultBackground == null)
            {
                GradientStopCollection gradientStopCollection = new GradientStopCollection();
                gradientStopCollection.Add(new GradientStop(Colors.LightGray, 0));
                gradientStopCollection.Add(new GradientStop(Colors.White, 0.2));
                gradientStopCollection.Add(new GradientStop(Colors.White, 0.9));
                defaultBackground = new LinearGradientBrush(gradientStopCollection, new Point(0, 0), new Point(0, 1));
            }

            return defaultBackground;
        }

        private static void LayerIsVisiblePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var currentLayerListItem = sender as LayerListItem;
            if (e.PropertyName == "IsChecked" && GisEditor.LayerListManager.SelectedLayerListItems.Contains(currentLayerListItem))
            {
                foreach (var selectedViewModel in GisEditor.LayerListManager.SelectedLayerListItems)
                {
                    if (selectedViewModel.IsChecked != currentLayerListItem.IsChecked)
                    {
                        selectedViewModel.IsChecked = currentLayerListItem.IsChecked;
                    }
                }
            }
        }

        #region search places
        /// <summary>
        /// Gets a value indicating whether this instance can place search.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance can place search; otherwise, <c>false</c>.
        /// </value>
        [Obsolete("This method is obsoleted, please call SearchPlaceProvider.CanSearchPlace() instead.")]
        public bool CanSearchPlace()
        {
            return SearchPlaceTool.CanSearchPlace();
        }

        [Obsolete("This method is obsoleted, please call SearchPlaceProvider.CanSearchPlace(Layer) instead.")]
        public bool CanSearchPlace(Layer layer)
        {
            return SearchPlaceTool.CanSearchPlace(layer);
        }

        [Obsolete("This method is obsoleted, please call SearchPlaceProvider.CanSearchPlace(Layer) instead.")]
        protected virtual bool CanSearchPlaceCore(Layer layer)
        {
            return SearchPlaceTool.CanSearchPlace(layer);
        }

        [Obsolete("This method is obsoleted, please call SearchPlaceProvider.SearchPlaces(string, Layer) instead.")]
        public Collection<Feature> SearchPlaces(string inputAddress, Layer layerToSearch)
        {
            return SearchPlaceTool.SearchPlaces(inputAddress, layerToSearch);
        }

        [Obsolete("This method is obsoleted, please call SearchPlaceProvider.SearchPlaces(string, Layer) instead.")]
        protected virtual Collection<Feature> SearchPlacesCore(string inputAddress, Layer layerToSearch)
        {
            return SearchPlaceTool.SearchPlaces(inputAddress, layerToSearch);
        }
        #endregion
    }
}