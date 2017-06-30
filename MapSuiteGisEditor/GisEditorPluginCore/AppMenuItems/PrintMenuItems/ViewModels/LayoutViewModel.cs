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
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class LayoutViewModel : ViewModelBase
    {
        private string image;

        private PrinterPageSize pageSize;

        private FilterPageOrientation orientation;

        private ObservableCollection<PrinterLayer> printerLayers;

        private string description;

        public LayoutViewModel()
        {
            printerLayers = new ObservableCollection<PrinterLayer>();
        }

        public string Image
        {
            get { return image; }
            set
            {
                image = value;
                RaisePropertyChanged(()=>Image);
            }
        }

        public string Preview
        {
            get
            {
                return image.Replace("_small", "_large");
            }
        }

        public PrinterPageSize PageSize
        {
            get { return pageSize; }
            set
            {
                pageSize = value;
                RaisePropertyChanged(()=>PageSize);
            }
        }

        public FilterPageOrientation Orientation
        {
            get { return orientation; }
            set { orientation = value; }
        }

        public ObservableCollection<PrinterLayer> PrinterLayers
        {
            get { return printerLayers; }
        }

        public string Description
        {
            get { return description; }
            set { description = value; }
        }
    }
}