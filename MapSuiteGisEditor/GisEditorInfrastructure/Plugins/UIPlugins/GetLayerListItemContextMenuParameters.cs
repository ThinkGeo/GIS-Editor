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

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represents contex menu arguments for layer list item
    /// </summary>
    [Serializable]
    public class GetLayerListItemContextMenuParameters
    {
        private LayerListItem layerListItem;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetLayerListItemContextMenuParameters" /> class.
        /// </summary>
        public GetLayerListItemContextMenuParameters()
            : this(null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetLayerListItemContextMenuParameters" /> class.
        /// </summary>
        /// <param name="layerListItem">The layer list item.</param>
        public GetLayerListItemContextMenuParameters(LayerListItem layerListItem)
        {
            this.layerListItem = layerListItem;
        }

        /// <summary>
        /// Gets or sets the layer list item.
        /// </summary>
        /// <value>
        /// The layer list item.
        /// </value>
        public LayerListItem LayerListItem
        {
            get { return layerListItem; }
            set { layerListItem = value; }
        }

        public static implicit operator GetLayerListItemContextMenuParameters(LayerListItem layerListItem)
        {
            return new GetLayerListItemContextMenuParameters(layerListItem);
        }
    }
}
