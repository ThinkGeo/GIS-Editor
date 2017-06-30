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
using System.ComponentModel.Composition;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represents style plugin
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(StylePlugin))]
    public abstract class StylePlugin : Plugin
    {
        private StyleCategories styleCategories;
        private int styleCandidatesIndex;
        private bool useRandomColor;
        private bool isDefaultCore;
        private bool requireColumnNames;
        private ObservableCollection<Style> styleCandidates;

        /// <summary>
        /// Initializes a new instance of the <see cref="StylePlugin" /> class.
        /// </summary>
        protected StylePlugin()
        {
            UseRandomColor = true;
            styleCategories = StyleCategories.Point | StyleCategories.Line | StyleCategories.Area | StyleCategories.Label;
            styleCandidates = new ObservableCollection<Style>();
            styleCandidatesIndex = 0;
        }

        public ObservableCollection<Style> StyleCandidates
        {
            get { return styleCandidates; }
        }

        public int StyleCandidatesIndex
        {
            get { return styleCandidatesIndex; }
            set { styleCandidatesIndex = value; }
        }

        /// <summary>
        /// This property gets a priority for getting back the provider where querying style providers.
        /// None-default style provider will be selected first;
        /// if none-provider is selected, choose default provider.
        ///
        /// So the default provider has larger range for founding provider from style.
        /// </summary>
        public bool IsDefault
        {
            get { return isDefaultCore; }
            protected set { isDefaultCore = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [require column names].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [require column names]; otherwise, <c>false</c>.
        /// </value>
        public bool RequireColumnNames
        {
            get { return requireColumnNames; }
            protected set { requireColumnNames = value; }
        }

        /// <summary>
        /// Gets or sets the style categories.
        /// </summary>
        /// <value>
        /// The style categories.
        /// </value>
        public StyleCategories StyleCategories
        {
            get { return styleCategories; }
            protected set { styleCategories = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [use random color].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use random color]; otherwise, <c>false</c>.
        /// </value>
        public bool UseRandomColor
        {
            get { return useRandomColor; }
            set { useRandomColor = value; }
        }

        /// <summary>
        /// Gets the default style.
        /// </summary>
        /// <returns>Default style</returns>
        public Style GetDefaultStyle()
        {
            Style style = null;
            if (!UseRandomColor && IsDefault && StyleCandidates.Count > 0)
            {
                if (StyleCandidatesIndex < 0) StyleCandidatesIndex = 0;

                int count = StyleCandidates.Count;
                int index = StyleCandidatesIndex % count;
                style = StyleCandidates[index].CloneDeep();
            }

            if (style == null)
            {
                style = GetDefaultStyleCore();
            }

            if (string.IsNullOrEmpty(style.Name))
            {
                style.Name = Name;
            }

            return style;
        }

        /// <summary>
        /// Gets the default style core.
        /// </summary>
        /// <returns>Default style</returns>
        protected abstract Style GetDefaultStyleCore();

        //public StyleEditResult EditStyle(Style style, StyleArguments arguments)
        //{
        //    if (style == null || !CheckIsStyleSupported(style))
        //    {
        //        style = GetDefaultStyle();
        //        arguments.IsNewStyle = true;
        //    }

        //    style.Name = arguments.StyleName;
        //    StyleEditResult result = EditStyleCore(style, arguments);
        //    return result;
        //}

        //protected abstract StyleEditResult EditStyleCore(Style style, StyleArguments arguments);

        /// <summary>
        /// Gets the style layer list item.
        /// </summary>
        /// <param name="style">The style.</param>
        /// <returns>a list item for style layer</returns>
        public StyleLayerListItem GetStyleLayerListItem(Style style)
        {
            if (style != null && string.IsNullOrEmpty(style.Name))
            {
                style.Name = style.GetType().Name;
            }
            return GetStyleLayerListItemCore(style);
        }

        /// <summary>
        /// Gets the style layer list item core.
        /// </summary>
        /// <param name="style">The style.</param>
        /// <returns>a list item for style layer</returns>
        protected virtual StyleLayerListItem GetStyleLayerListItemCore(Style style)
        {
            return new StyleLayerListItem(style);
        }

        public Collection<MenuItem> GetLayerListItemContextMenuItems(GetLayerListItemContextMenuParameters parameters)
        {
            return GetLayerListItemContextMenuItemsCore(parameters);
        }

        protected virtual Collection<MenuItem> GetLayerListItemContextMenuItemsCore(GetLayerListItemContextMenuParameters parameters)
        {
            return new Collection<MenuItem>();
        }
    }
}