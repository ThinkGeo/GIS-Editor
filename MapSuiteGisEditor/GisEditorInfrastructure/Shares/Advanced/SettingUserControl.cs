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
using System.Windows.Controls;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represents an user control for a setting
    /// </summary>
    [Serializable]
    public class SettingUserControl : UserControl
    {
        private string title;
        private string category;
        private string description;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingUserControl" /> class.
        /// </summary>
        protected SettingUserControl()
            : this(string.Empty, string.Empty, string.Empty)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingUserControl" /> class.
        /// </summary>
        /// <param name="title">The title.</param>
        protected SettingUserControl(string title)
            : this(title, string.Empty, string.Empty)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingUserControl" /> class.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="category">The category.</param>
        protected SettingUserControl(string title, string category)
            : this(title, category, string.Empty)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingUserControl" /> class.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="category">The category.</param>
        /// <param name="description">The description.</param>
        protected SettingUserControl(string title, string category, string description)
        {
            this.title = title;
            this.category = category;
            this.description = description;
        }

        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        /// <value>
        /// The category.
        /// </value>
        public string Category
        {
            get { return category; }
            set { category = value; }
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
    }
}