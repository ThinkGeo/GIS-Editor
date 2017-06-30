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
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class OutputWindow : Window
    {
        public static string SavedProj4ProjectionParametersString;

        private Uri layerUri;
        private Dictionary<string, object> customData;

        protected OutputWindow()
        {
            customData = new Dictionary<string, object>();
        }

        public Uri LayerUri
        {
            get { return layerUri; }
            protected set { layerUri = value; }
        }

        public OutputMode OutputMode { get; set; }

        public string DefaultPrefix
        {
            get { return DefaultPrefixCore; }
            set { DefaultPrefixCore = value; }
        }

        protected virtual string DefaultPrefixCore
        {
            get;
            set;
        }

        public string Proj4ProjectionParametersString
        {
            set
            {
                Proj4ProjectionParametersStringCore = value;
            }
            get
            {
                return Proj4ProjectionParametersStringCore;
            }
        }

        protected virtual string Proj4ProjectionParametersStringCore { get; set; }

        public string Extension
        {
            get;
            set;
        }

        public string ExtensionFilter
        {
            get { return ExtensionFilterCore; }
            set { ExtensionFilterCore = value; }
        }

        public Dictionary<string, object> CustomData
        {
            get { return customData; }
        }

        protected virtual string ExtensionFilterCore
        {
            get;
            set;
        }
    }
}