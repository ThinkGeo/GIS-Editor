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
    /// 
    /// </summary>
    [Serializable]
    public class DocumentWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentWindow" /> class.
        /// </summary>
        public DocumentWindow()
            : this(null, string.Empty, string.Empty, false)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentWindow" /> class.
        /// </summary>
        /// <param name="content">The content.</param>
        public DocumentWindow(Control content)
            : this(content, string.Empty, string.Empty, false)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentWindow" /> class.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="title">The title.</param>
        public DocumentWindow(Control content, string title)
            : this(content, title, title, false)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentWindow" /> class.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="title">The title.</param>
        /// <param name="name">The name.</param>
        public DocumentWindow(Control content, string title, string name)
            : this(content, title, name, false)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentWindow" /> class.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="title">The title.</param>
        /// <param name="name">The name.</param>
        /// <param name="canFloat">if set to <c>true</c> [can float].</param>
        public DocumentWindow(Control content, string title, string name, bool canFloat)
        {
            Content = content;
            Name = name;
            Title = title;
            CanFloat = canFloat;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>
        /// The content.
        /// </value>
        public Control Content { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can float.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can float; otherwise, <c>false</c>.
        /// </value>
        public bool CanFloat { get; set; }
    }
}