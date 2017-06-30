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
using System.Linq;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represents a style builder arguments
    /// </summary>
    [Serializable]
    public class StyleBuilderArguments
    {
        private Collection<string> columnNames;
        private bool isSubStyleReadonly;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyleBuilderArguments" /> class.
        /// </summary>
        public StyleBuilderArguments()
        {
            columnNames = new Collection<string>();
            AvailableUIElements = StyleBuilderUIElements.ZoomLevelPicker | StyleBuilderUIElements.StyleList;
            AvailableStyleCategories = StyleCategories.Area | StyleCategories.Line | StyleCategories.Point;
            FromZoomLevelIndex = 1;
            if (GisEditor.ActiveMap != null) ToZoomLevelIndex = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Count;
            else ToZoomLevelIndex = 25;
        }

        /// <summary>
        /// Gets or sets the style to edit.
        /// </summary>
        /// <value>
        /// The style to edit.
        /// </value>
        public CompositeStyle StyleToEdit { get; set; }

        /// <summary>
        /// Gets or sets the available UI elements.
        /// </summary>
        /// <value>
        /// The available UI elements.
        /// </value>
        public StyleBuilderUIElements AvailableUIElements { get; set; }

        /// <summary>
        /// Gets the column names.
        /// </summary>
        /// <value>
        /// The column names.
        /// </value>
        public Collection<string> ColumnNames { get { return columnNames; } }

        /// <summary>
        /// Gets or sets the available style categories.
        /// </summary>
        /// <value>
        /// The available style categories.
        /// </value>
        public StyleCategories AvailableStyleCategories { get; set; }

        /// <summary>
        /// starts from 1.
        /// </summary>
        public int FromZoomLevelIndex { get; set; }

        /// <summary>
        /// starts from 1.
        /// </summary>
        public int ToZoomLevelIndex { get; set; }

        /// <summary>
        /// Gets or sets the feature layer.
        /// </summary>
        /// <value>
        /// The feature layer.
        /// </value>
        public FeatureLayer FeatureLayer { get; set; }

        /// <summary>
        /// Gets or sets the selected concrete object.
        /// </summary>
        /// <value>
        /// The selected concrete object. 
        /// Such as Style, ValueItem, ClassBreak etc.
        /// </value>
        public object SelectedConcreteObject { get; set; }

        public bool IsSubStyleReadonly
        {
            get { return isSubStyleReadonly; }
            set { isSubStyleReadonly = value; }
        }

        /// <summary>
        /// Gets or sets the applied callback.
        /// </summary>
        /// <value>
        /// The applied callback.
        /// </value>
        public Action<StyleBuilderResult> AppliedCallback { get; set; }

        internal void FillRequiredColumnNames()
        {
            if (ColumnNames.Count == 0 && FeatureLayer != null)
            {
                FeatureLayer.SafeProcess(() =>
                {
                    ColumnNames.Clear();
                    foreach (var columnName in FeatureLayer.FeatureSource.GetColumns(GettingColumnsType.All).Select(column => column.ColumnName))
                    {
                        if (!string.IsNullOrEmpty(columnName) && !ColumnNames.Contains(columnName))
                        {
                            ColumnNames.Add(columnName);
                        }
                    }
                });
            }
        }
    }
}