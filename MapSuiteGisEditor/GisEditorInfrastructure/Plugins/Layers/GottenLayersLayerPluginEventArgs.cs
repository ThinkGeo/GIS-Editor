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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    public class GottenLayersLayerPluginEventArgs : EventArgs
    {
        private Collection<Layer> layers;
        private GetLayersParameters parameters;

        public GottenLayersLayerPluginEventArgs()
            : this(new Collection<Layer>(), new GetLayersParameters())
        { }

        public GottenLayersLayerPluginEventArgs(IEnumerable<Layer> layers, GetLayersParameters parameters)
        {
            this.parameters = parameters;
            this.layers = new Collection<Layer>();
            foreach (var layer in layers)
            {
                this.layers.Add(layer);
            }
        }

        public Collection<Layer> Layers
        {
            get { return layers; }
        }

        public GetLayersParameters Parameters
        {
            get { return parameters; }
            set { parameters = value; }
        }
    }
}