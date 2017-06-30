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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [PartNotDiscoverable]
    public class UsingOnlineServiceLayerPlugin : FeatureLayerPlugin
    {
        public UsingOnlineServiceLayerPlugin()
        {
            Name = GisEditor.LanguageManager.GetStringResource("UsingOnlineServiceLayerPluginName");
            SearchPlaceToolCore = new UsingOnlineServiceSearchPlaceTool();
            IsActive = true;
        }

        protected override Type GetLayerTypeCore()
        {
            return null;
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return null;
        }

        #region search place
        [Obsolete("This method is obsoleted, please call SearchPlaceProvider.SearchPlace(string, Layer) instead.")]
        protected override Collection<Feature> SearchPlacesCore(string inputAddress, Layer layerToSearch)
        {
            return SearchPlaceTool.SearchPlaces(inputAddress, layerToSearch);
        }

        [Obsolete("This method is obsoleted, please call SearchPlaceProvider.CanSearchPlace(Layer) instead.")]
        protected override bool CanSearchPlaceCore(Layer layer)
        {
            return true;
        } 
        #endregion
    }
}