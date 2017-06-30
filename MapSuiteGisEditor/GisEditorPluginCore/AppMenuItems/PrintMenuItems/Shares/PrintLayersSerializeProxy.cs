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
using System.Reflection;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    [Obfuscation]
    public class PrintLayersSerializeProxy
    {
        [Obfuscation]
        private Collection<PrinterLayer> printerLayers;

        [Obfuscation]
        private PrinterLayer gridPrinterLayer;

        public PrintLayersSerializeProxy()
            : this(new PrinterLayer[] { })
        { }

        public PrintLayersSerializeProxy(IEnumerable<PrinterLayer> printerLayers)
        {
            this.printerLayers = new Collection<PrinterLayer>();
            foreach (var layer in printerLayers)
            {
                this.printerLayers.Add(layer);
            }
        }

        public Collection<PrinterLayer> PrinterLayers
        {
            get { return printerLayers; }
        }

        public PrinterLayer GridPrinterLayer
        {
            get { return gridPrinterLayer; }
            set { gridPrinterLayer = value; }
        }
    }
}