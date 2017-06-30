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
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represents a list item for a style layer
    /// </summary>
    [Serializable]
    public class StyleLayerListItem : LayerListItem
    {
        [NonSerialized]
        private EventHandler concreteObjectUpdated;

        [NonSerialized]
        private string zoomLevelRange;
        [NonSerialized]
        private bool preventRaisingConcreteObjectUpdated;

        [Obfuscation]
        private bool canAddInnerStyle;

        [Obfuscation]
        private bool canRename;

        [Obfuscation]
        private string nameCore;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyleLayerListItem" /> class.
        /// </summary>
        /// <param name="concreteObject">The concrete object.</param>
        public StyleLayerListItem(object concreteObject)
        {
            Style style = concreteObject as Style;
            if (style != null)
            {
                this.CheckBoxVisibility = System.Windows.Visibility.Visible;
                this.IsChecked = style.IsActive;
            }
            else
            {
                this.CheckBoxVisibility = System.Windows.Visibility.Collapsed;
            }
            this.ConcreteObject = concreteObject;
            this.Children.CollectionChanged += new NotifyCollectionChangedEventHandler(Items_CollectionChanged);
        }

        /// <summary>
        /// Occurs when [concrete object updated].
        /// </summary>
        public event EventHandler ConcreteObjectUpdated
        {
            add
            {
                concreteObjectUpdated -= value;
                concreteObjectUpdated += value;
                foreach (var child in Children.OfType<StyleLayerListItem>())
                {
                    child.concreteObjectUpdated -= value;
                    child.concreteObjectUpdated += value;
                }
            }
            remove
            {
                concreteObjectUpdated -= value;
                foreach (var child in Children.OfType<StyleLayerListItem>())
                {
                    child.concreteObjectUpdated -= value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the zoom level range.
        /// </summary>
        /// <value>
        /// The zoom level range.
        /// </value>
        public string ZoomLevelRange
        {
            get { return zoomLevelRange; }
            set
            {
                zoomLevelRange = value;
                OnPropertyChanged("ZoomLevelRange");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can rename.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance can rename; otherwise, <c>false</c>.
        /// </value>
        public bool CanRename
        {
            get { return canRename; }
            protected set { canRename = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can add inner style.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance can add inner style; otherwise, <c>false</c>.
        /// </value>
        public bool CanAddInnerStyle
        {
            get { return canAddInnerStyle; }
            protected set { canAddInnerStyle = value; }
        }

        /// <summary>
        /// Gets or sets the name core.
        /// </summary>
        /// <value>
        /// The name core.
        /// </value>
        protected override string NameCore
        {
            get
            {
                Style actualStyle = ConcreteObject as Style;
                if (actualStyle != null)
                {
                    nameCore = actualStyle.Name;
                }
                else if (string.IsNullOrEmpty(nameCore))
                {
                    nameCore = "Unknown";
                }

                return nameCore;
            }
            set
            {
                nameCore = value;
                Style actualStyle = ConcreteObject as Style;
                if (actualStyle != null)
                {
                    actualStyle.Name = value;
                }
            }
        }

        /// <summary>
        /// Determines whether this instance [can contain style item] the specified style item.
        /// </summary>
        /// <param name="styleItem">The style item.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can contain style item] the specified style item; otherwise, <c>false</c>.
        /// </returns>
        public bool CanContainStyleItem(StyleLayerListItem styleItem)
        {
            return CanContainStyleItemCore(styleItem);
        }

        /// <summary>
        /// Determines whether this instance [can contain style item core] the specified style item.
        /// </summary>
        /// <param name="styleItem">The style item.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can contain style item core] the specified style item; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool CanContainStyleItemCore(StyleLayerListItem styleItem)
        {
            return false;
        }

        /// <summary>
        /// Gets the restrict style categories.
        /// </summary>
        /// <returns>The style categories</returns>
        public StyleCategories GetRestrictStyleCategories()
        {
            return GetRestrictStyleCategoriesCore();
        }

        /// <summary>
        /// Gets the restrict style categories core.
        /// </summary>
        /// <returns>The style categories</returns>
        protected virtual StyleCategories GetRestrictStyleCategoriesCore()
        {
            return StyleCategories.None;
        }

        /// <summary>
        /// Gets the UI.
        /// </summary>
        /// <param name="styleArguments">The style arguments.</param>
        /// <returns>The style user control</returns>
        public StyleUserControl GetUI(StyleBuilderArguments styleArguments)
        {
            StyleUserControl styleUI = GetUICore(styleArguments);
            if (styleUI != null) styleUI.StyleItem = this;
            return styleUI;
        }

        /// <summary>
        /// Gets the UI core.
        /// </summary>
        /// <param name="styleArguments">The style arguments.</param>
        /// <returns>The style user control</returns>
        protected virtual StyleUserControl GetUICore(StyleBuilderArguments styleArguments)
        {
            return null;
        }

        /// <summary>
        /// Gets the preview image.
        /// </summary>
        /// <param name="screenWidth">Width of the screen.</param>
        /// <param name="screenHeight">Height of the screen.</param>
        /// <returns>The previewImage buffer bytes arry</returns>
        public byte[] GetPreviewImage(int screenWidth, int screenHeight)
        {
            return GetPreviewImageCore(screenWidth, screenHeight);
        }

        /// <summary>
        /// Gets the preview image core.
        /// </summary>
        /// <param name="screenWidth">Width of the screen.</param>
        /// <param name="screenHeight">Height of the screen.</param>
        /// <returns>The previewImage buffer bytes arry</returns>
        protected virtual byte[] GetPreviewImageCore(int screenWidth, int screenHeight)
        {
            var actualStyle = ConcreteObject as Style;
            if (actualStyle != null)
            {
                return actualStyle.GetPreviewBinary(screenWidth, screenHeight);
            }
            else return null;
        }

        /// <summary>
        /// Gets the preview source.
        /// </summary>
        /// <param name="screenWidth">Width of the screen.</param>
        /// <param name="screenHeight">Height of the screen.</param>
        /// <returns>Preview bitmap source</returns>
        internal BitmapSource GetPreviewSource(int screenWidth, int screenHeight)
        {
            BitmapImage bitmapImage = new BitmapImage();
            var imageBuffer = GetPreviewImage(screenWidth, screenHeight);
            if (imageBuffer != null)
            {
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(imageBuffer);
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            return bitmapImage;
        }

        /// <summary>
        /// Updates the concrete object.
        /// </summary>
        public void UpdateConcreteObject()
        {
            UpdateConcreteObjectCore();
            UpdateStyleItem();
        }

        /// <summary>
        /// Updates the concrete object core.
        /// </summary>
        protected virtual void UpdateConcreteObjectCore()
        { }

        /// <summary>
        /// Updates the UI.
        /// </summary>
        /// <param name="styleItemUI">The style item UI.</param>
        public void UpdateUI(StyleUserControl styleItemUI)
        {
            preventRaisingConcreteObjectUpdated = true;

            try
            {
                if (styleItemUI != null)
                {
                    UpdateUICore(styleItemUI);
                }
            }
            catch (Exception e)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
            }
            finally
            {
                preventRaisingConcreteObjectUpdated = false;
            }
        }

        /// <summary>
        /// Updates the UI core.
        /// </summary>
        /// <param name="styleItemUI">The style item UI.</param>
        protected virtual void UpdateUICore(UserControl styleItemUI)
        { }

        /// <summary>
        /// Clones the deep.
        /// </summary>
        /// <returns>clone deep style layer list item object</returns>
        public StyleLayerListItem CloneDeep()
        {
            return CloneDeepCore();
        }

        /// <summary>
        /// Clones the deep core.
        /// </summary>
        /// <returns>clone deep style layer list item object</returns>
        protected virtual StyleLayerListItem CloneDeepCore()
        {
            BinaryFormatter serializer = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, this);
                stream.Seek(0, SeekOrigin.Begin);
                var newItem = (StyleLayerListItem)serializer.Deserialize(stream);
                newItem.concreteObjectUpdated = concreteObjectUpdated;
                return newItem;
            }
        }

        /// <summary>
        /// Updates the style item.
        /// </summary>
        public void UpdateStyleItem()
        {
            if (!preventRaisingConcreteObjectUpdated)
            {
                UpdateStyleItemCore();
            }
        }

        /// <summary>
        /// Updates the style item core.
        /// </summary>
        protected virtual void UpdateStyleItemCore()
        {
            OnConcreteObjectUpdated();
        }

        /// <summary>
        /// Called when [concrete object updated].
        /// </summary>
        protected virtual void OnConcreteObjectUpdated()
        {
            var action = concreteObjectUpdated;
            if (action != null)
            {
                action(this, new EventArgs());
            }
        }

        /// <summary>
        /// Handles the CollectionChanged event of the Items control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs" /> instance containing the event data.</param>
        private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<StyleLayerListItem>())
                {
                    item.Parent = this;
                    item.ConcreteObjectUpdated -= Item_UnderlieObjectUpdated;
                    item.ConcreteObjectUpdated += Item_UnderlieObjectUpdated;
                }
            }
            else if ((e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset) && e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<StyleLayerListItem>())
                {
                    item.ConcreteObjectUpdated -= Item_UnderlieObjectUpdated;
                }
            }
        }

        /// <summary>
        /// Handles the UnderlieObjectUpdated event of the Item control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void Item_UnderlieObjectUpdated(object sender, EventArgs e)
        {
            UpdateStyleItem();
        }
    }
}