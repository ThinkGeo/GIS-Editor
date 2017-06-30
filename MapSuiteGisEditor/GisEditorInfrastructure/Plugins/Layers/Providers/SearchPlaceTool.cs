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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    public class SearchPlaceTool
    {
        public SearchPlaceTool()
        { }

        /// <summary>
        /// Gets a value indicating whether this instance can search place.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance can place search; otherwise, <c>false</c>.
        /// </value>
        public bool CanSearchPlace()
        {
            return CanSearchPlace(null);
        }

        /// <summary>
        /// Gets a value indicates whether this plugin can search place on the specified layer.
        /// </summary>
        /// <param name="layer">A layer to search.</param>
        /// <returns>
        /// <c>true</c> if this instance can place search; otherwise, <c>false</c>.
        /// </returns>
        public bool CanSearchPlace(Layer layer)
        {
            return CanSearchPlaceCore(layer);
        }

        /// <summary>
        /// Gets a value indicates whether this plugin can search place on the specified layer.
        /// </summary>
        /// <param name="layer">A layer to search.</param>
        /// <returns>
        /// <c>true</c> if this instance can place search; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool CanSearchPlaceCore(Layer layer)
        {
            return false;
        }

        public Collection<Feature> SearchPlaces(string inputAddress, Layer layerToSearch)
        {
            if (CanSearchPlace(layerToSearch))
            {
                return SearchPlacesCore(inputAddress, layerToSearch);
            }
            else
            {
                return new Collection<Feature>();
            }
        }

        protected virtual Collection<Feature> SearchPlacesCore(string inputAddress, Layer layerToSearch)
        {
            return new Collection<Feature>();
        }
    }
}
