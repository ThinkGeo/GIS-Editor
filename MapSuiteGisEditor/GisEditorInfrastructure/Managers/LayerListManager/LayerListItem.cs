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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [Obfuscation]
    public class LayerListItem : INotifyPropertyChanged
    {
        //give the isVisible a default value of true. because for most of the time, when we add a map element, that element is visible.
        //another reason is that changing the visibility leads to refreshing, refreshing leads to copying layers, copying layers leads to lock.
        //doing this avoids dead locking
        //when a layer overlay is drawing (which means the copied layers are locked), if we create a new entity, then there will be dead locking.
        //public static string ZoomLevelPattern = "(?<=\\()\\d+ to \\d+(?=\\))";
        /// <summary>
        /// Occurs when [property changed].
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private object concreteObject;
        private string nameCore;
        private ObservableCollection<LayerListItem> children;
        [NonSerialized]
        private bool isChecked = true;
        [NonSerialized]
        private bool isSelected;
        [NonSerialized]
        private bool isRenaming;
        [NonSerialized]
        private DispatcherTimer delayRefreshingTimer;
        [NonSerialized]
        private Visibility childrenContainerVisibility;
        [NonSerialized]
        private Image sideIcon;
        [NonSerialized]
        private Image previewImage;
        [NonSerialized]
        private Brush highlightBackgroundBrush;
        [NonSerialized]
        private bool isExpanded;
        [NonSerialized]
        private Visibility lowerLineVisibility;
        [NonSerialized]
        private Visibility upperLineVisibility;
        [NonSerialized]
        private Brush backgroundBrush;
        [NonSerialized]
        private LayerListItem parent;
        [NonSerialized]
        private Visibility checkBoxVisibility;
        [NonSerialized]
        private Collection<MenuItem> contextMenuItems;
        [NonSerialized]
        private FontWeight fontWeight;
        [NonSerialized]
        private Brush fontBrush;
        [NonSerialized]
        private FontStyle fontStyle;
        [NonSerialized]
        private Visibility expandButtonVisibility;
        [NonSerialized]
        private bool needRefresh;
        [NonSerialized]
        private ObservableCollection<Image> warningImages;
        [NonSerialized]
        private Action load;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayerListItem" /> class.
        /// </summary>
        public LayerListItem() : this(null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LayerListItem" /> class.
        /// </summary>
        /// <param name="concreteObject">The concrete object.</param>
        public LayerListItem(object concreteObject)
        {
            warningImages = new ObservableCollection<Image>();
            this.concreteObject = concreteObject;
            children = new ObservableCollection<LayerListItem>();
            contextMenuItems = new Collection<MenuItem>();
            //isEnabled = true;
            delayRefreshingTimer = new DispatcherTimer();
            delayRefreshingTimer.Interval = TimeSpan.FromMilliseconds(200);
            delayRefreshingTimer.Tick += new EventHandler(delayRefreshingTimer_Tick);
            FontWeight = FontWeights.Normal;
            childrenContainerVisibility = Visibility.Collapsed;
            isExpanded = true;
            FontBrush = new SolidColorBrush(Colors.Black);
            FontStyle = FontStyles.Normal;
            lowerLineVisibility = Visibility.Hidden;
            upperLineVisibility = Visibility.Hidden;
            expandButtonVisibility = Visibility.Visible;
            needRefresh = true;
        }

        public Action Load
        {
            get { return load; }
            set { load = value; }
        }

        /// <summary>
        /// Gets or sets the concrete object.
        /// </summary>
        /// <value>
        /// The concrete object.
        /// </value>
        public object ConcreteObject
        {
            get { return concreteObject; }
            set { concreteObject = value; }
        }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>
        /// The parent.
        /// </value>
        public LayerListItem Parent
        {
            get { return parent; }
            set
            {
                parent = value;
            }
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <value>
        /// The children.
        /// </value>
        public ObservableCollection<LayerListItem> Children
        {
            get { return children; }
        }

        /// <summary>
        /// Gets or sets the check box visibility.
        /// </summary>
        /// <value>
        /// The check box visibility.
        /// </value>
        public Visibility CheckBoxVisibility
        {
            get { return checkBoxVisibility; }
            set { checkBoxVisibility = value; }
        }

        /// <summary>
        /// Gets or sets the expand button visibility.
        /// </summary>
        /// <value>
        /// The expand button visibility.
        /// </value>
        public Visibility ExpandButtonVisibility
        {
            get { return expandButtonVisibility; }
            set { expandButtonVisibility = value; }
        }

        /// <summary>
        /// Gets the context menu items.
        /// </summary>
        /// <value>
        /// The context menu items.
        /// </value>
        public Collection<MenuItem> ContextMenuItems
        {
            get { return contextMenuItems; }
        }

        /// <summary>
        /// Gets or sets the double clicked.
        /// </summary>
        /// <value>
        /// The double clicked.
        /// </value>
        public Action DoubleClicked { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name
        {
            get { return NameCore; }
            set
            {
                NameCore = value;
            }
        }

        /// <summary>
        /// Gets or sets the name core.
        /// </summary>
        /// <value>
        /// The name core.
        /// </value>
        protected virtual string NameCore
        {
            get { return nameCore; }
            set
            {
                nameCore = value;
                OnPropertyChanged("Name");
                TryChangeNameOrIDPropertyOfMapElement();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is checked.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is checked; otherwise, <c>false</c>.
        /// </value>
        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                if (isChecked != value)
                {
                    isChecked = value;
                    if (parent == null) return;
                    VisibilityChanged(value);
                    OnPropertyChanged("IsChecked");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is selected.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is selected; otherwise, <c>false</c>.
        /// </value>
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

        //public bool IsEnabled
        //{
        //    get { return isEnabled; }
        //    set
        //    {
        //        isEnabled = value;
        //        OnPropertyChanged("IsEnabled");
        //    }
        //}

        /// <summary>
        /// Gets or sets a value indicating whether this instance is renaming.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is renaming; otherwise, <c>false</c>.
        /// </value>
        public bool IsRenaming
        {
            get { return isRenaming; }
            set
            {
                isRenaming = value;
                OnPropertyChanged("IsRenaming");
            }
        }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        /// <value>
        /// The font weight.
        /// </value>
        public FontWeight FontWeight
        {
            get { return fontWeight; }
            set
            {
                fontWeight = value;
                OnPropertyChanged("FontWeight");
            }
        }

        /// <summary>
        /// Gets or sets the children container visibility.
        /// </summary>
        /// <value>
        /// The children container visibility.
        /// </value>
        public Visibility ChildrenContainerVisibility
        {
            get { return childrenContainerVisibility; }
            set
            {
                childrenContainerVisibility = value;
                if (load != null && childrenContainerVisibility == Visibility.Visible)
                {
                    load();
                    load = null;
                }

                OnPropertyChanged("ChildrenContainerVisibility");
            }
        }

        /// <summary>
        /// Gets or sets the side image.
        /// </summary>
        /// <value>
        /// The side image.
        /// </value>
        public Image SideImage
        {
            get { return sideIcon; }
            set
            {
                sideIcon = value;
                OnPropertyChanged("SideImage");
            }
        }

        /// <summary>
        /// Gets or sets the preview image.
        /// </summary>
        /// <value>
        /// The preview image.
        /// </value>
        public Image PreviewImage
        {
            get { return previewImage; }
            set
            {
                previewImage = value;
                OnPropertyChanged("PreviewImage");
            }
        }

        /// <summary>
        /// Gets the background brush.
        /// </summary>
        /// <value>
        /// The background brush.
        /// </value>
        public Brush BackgroundBrush
        {
            get { return backgroundBrush; }
        }

        /// <summary>
        /// Gets or sets the highlight background brush.
        /// </summary>
        /// <value>
        /// The highlight background brush.
        /// </value>
        public Brush HighlightBackgroundBrush
        {
            get { return highlightBackgroundBrush; }
            set
            {
                highlightBackgroundBrush = value;
                if (backgroundBrush == null)
                {
                    backgroundBrush = value;
                }
                OnPropertyChanged("HighlightBackgroundBrush");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is expanded.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is expanded; otherwise, <c>false</c>.
        /// </value>
        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                isExpanded = value;
                OnPropertyChanged("IsExpanded");
            }
        }

        /// <summary>
        /// Gets or sets the font brush.
        /// </summary>
        /// <value>
        /// The font brush.
        /// </value>
        public Brush FontBrush
        {
            get { return fontBrush; }
            set
            {
                fontBrush = value;
                OnPropertyChanged("FontBrush");
            }
        }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        /// <value>
        /// The font style.
        /// </value>
        public FontStyle FontStyle
        {
            get { return fontStyle; }
            set
            {
                fontStyle = value;
                OnPropertyChanged("FontStyle");
            }
        }

        /// <summary>
        /// Gets or sets the lower line visibility.
        /// </summary>
        /// <value>
        /// The lower line visibility.
        /// </value>
        public Visibility LowerLineVisibility
        {
            get { return lowerLineVisibility; }
            set
            {
                if (value == lowerLineVisibility) return;
                lowerLineVisibility = value;
                OnPropertyChanged("LowerLineVisibility");
            }
        }

        /// <summary>
        /// Gets or sets the upper line visibility.
        /// </summary>
        /// <value>
        /// The upper line visibility.
        /// </value>
        public Visibility UpperLineVisibility
        {
            get { return upperLineVisibility; }
            set
            {
                if (value == upperLineVisibility) return;
                upperLineVisibility = value;
                OnPropertyChanged("UpperLineVisibility");
            }
        }

        /// <summary>
        /// Gets or sets the warning image.
        /// </summary>
        /// <value>
        /// The warning image.
        /// </value>
        public ObservableCollection<Image> WarningImages
        {
            get { return warningImages; }
            //set
            //{
            //    warningImage = value;
            //    OnPropertyChanged("WarningImage");
            //}
        }

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void VisibilityChanged(bool value)
        {
            if (ConcreteObject is LayerOverlay && needRefresh)
            {
                foreach (var subEntity in Children)
                {
                    subEntity.needRefresh = false;
                    LayerPlugin layerPlugin = GisEditor.LayerManager.GetLayerPlugins(subEntity.ConcreteObject.GetType()).FirstOrDefault();
                    if (layerPlugin != null)
                    {
                        bool isDataSourceAvailable = layerPlugin.DataSourceResolveTool.IsDataSourceAvailable((Layer)subEntity.ConcreteObject);
                        if (isDataSourceAvailable)
                        {
                            subEntity.IsChecked = IsChecked;
                            ((Layer)subEntity.ConcreteObject).IsVisible = IsChecked;
                        }
                    }
                    subEntity.needRefresh = true;
                }
                var tileOverlay = (TileOverlay)ConcreteObject;
                tileOverlay.IsVisible = isChecked;
                if (!isChecked) RefreshOverlay(tileOverlay);
                GisEditor.UIManager.InvokeRefreshPlugins();
            }
            else if (ConcreteObject is Layer && needRefresh)
            {
                LayerPlugin layerPlugin = GisEditor.LayerManager.GetLayerPlugins(ConcreteObject.GetType()).FirstOrDefault();
                if (layerPlugin != null)
                {
                    bool isDataSourceAvailable = layerPlugin.DataSourceResolveTool.IsDataSourceAvailable((Layer)ConcreteObject);
                    if (!isDataSourceAvailable)
                    {
                        ((Layer)ConcreteObject).IsVisible = false;
                        isChecked = false;
                        OnPropertyChanged("IsChecked");
                        return;
                    }
                }

                if (!isChecked)
                {
                    Image image = warningImages.FirstOrDefault(i => i.Name.Equals("InEditing", StringComparison.InvariantCultureIgnoreCase));
                    if (image != null) warningImages.Remove(image);
                }
                ((Layer)ConcreteObject).IsVisible = isChecked;
                Parent.needRefresh = false;
                Parent.IsChecked = Parent.Children.Any(m => m.IsChecked);
                Parent.needRefresh = true;
                TileOverlay tileOverlay = Parent.ConcreteObject as TileOverlay;
                if (tileOverlay != null)
                {
                    //In this case, tileOverlay will execute Refresh() in default.
                    if (!tileOverlay.IsVisible && Parent.IsChecked)
                    {
                        tileOverlay.IsVisible = Parent.IsChecked;
                    }
                    else
                    {
                        tileOverlay.IsVisible = Parent.IsChecked;
                        tileOverlay.Invalidate();
                        RefreshOverlay(tileOverlay);
                    }
                }
                GisEditor.UIManager.InvokeRefreshPlugins();
            }

            if (!(ConcreteObject is Layer) && !(ConcreteObject is LayerOverlay))
            {
                TryChangeIsVisiblePropertyOfMapElement();
            }

            if (ConcreteObject is Styles.Style)
            {
                Styles.Style concreteStyle = (Styles.Style)ConcreteObject;
                concreteStyle.IsActive = value;
                Layer layer = LayerListHelper.FindMapElementInTree<Layer>(this);
                if (layer != null && layer.IsVisible)
                {
                    LayerOverlay layerOverlay = LayerListHelper.FindMapElementInTree<LayerOverlay>(this);
                    if (layerOverlay != null && layerOverlay.IsVisible)
                    {
                        layerOverlay.Invalidate();
                        RefreshOverlay(layerOverlay);
                    }
                }
            }
        }

        private bool IsComponentStyle(object mapElement)
        {
            var styleItem = mapElement as StyleLayerListItem;
            if (styleItem != null)
                return styleItem.ConcreteObject is CompositeStyle;
            else
                return false;
        }

        private void TryChangeIsVisiblePropertyOfMapElement()
        {
            if (ConcreteObject != null)
            {
                SetMapElementProperty("IsVisible", isChecked);
            }
        }

        private void TryChangeNameOrIDPropertyOfMapElement()
        {
            if (ConcreteObject != null)
            {
                SetMapElementProperty("Name", nameCore);
                SetMapElementProperty("Id", nameCore);
            }
        }

        private void SetMapElementProperty(string propertyName, object value)
        {
            PropertyInfo prop = ConcreteObject.GetType().GetProperty(propertyName);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(ConcreteObject, value, null);
            }
        }


        private void delayRefreshingTimer_Tick(object sender, EventArgs e)
        {
            delayRefreshingTimer.Stop();
            OnPropertyChanged("IsChecked");
        }

        private static void RefreshOverlay(TileOverlay tileOverlay)
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action<TileOverlay>(tmpOverlay =>
                {
                    tmpOverlay.RefreshWithBufferSettings();
                }), tileOverlay);
            }
            else
            {
                tileOverlay.RefreshWithBufferSettings();
            }
        }
    }
}
