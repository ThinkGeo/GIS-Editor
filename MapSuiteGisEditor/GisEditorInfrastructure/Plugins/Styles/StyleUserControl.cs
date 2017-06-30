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
    /// This class represents an user control with specified style
    /// </summary>
    public class StyleUserControl : UserControl
    {
        private Uri helpUri;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyleUserControl" /> class.
        /// </summary>
        protected StyleUserControl()
        { }

        /// <summary>
        /// Gets or sets the style item.
        /// </summary>
        /// <value>
        /// The style item.
        /// </value>
        public StyleLayerListItem StyleItem { get; set; }

        /// <summary>
        /// Gets or sets the style builder arguments.
        /// </summary>
        /// <value>
        /// The style builder arguments.
        /// </value>
        public StyleBuilderArguments StyleBuilderArguments { get; set; }

        /// <summary>
        /// Validates this instance.
        /// </summary>
        /// <returns>the validate result</returns>
        public bool Validate()
        {
            return ValidateCore();
        }

        /// <summary>
        /// Validates the core.
        /// </summary>
        /// <returns>the validate result</returns>
        protected virtual bool ValidateCore()
        {
            return true;
        }

        /// <summary>
        /// Gets or sets the help URI.
        /// </summary>
        /// <value>
        /// The help URI.
        /// </value>
        public Uri HelpUri
        {
            get { return helpUri; }
            protected set { helpUri = value; }
        }
    }
}