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


using GalaSoft.MvvmLight;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    internal class LayerPropertiesUserControlViewModel : ViewModelBase
    {
        private Layer layer;
        private Dictionary<string, object> layerInformation;
        private Encoding selectedEncoding;
        private bool isEncodingPending;

        public LayerPropertiesUserControlViewModel(Layer layer)
        {
            this.layer = layer;
            this.Initialize();
        }

        public Dictionary<string, object> LayerInformation
        {
            get { return layerInformation; }
            private set
            {
                layerInformation = value;
                RaisePropertyChanged(()=>LayerInformation);
            }
        }

        public Encoding SelectedEncoding
        {
            get { return selectedEncoding; }
            set
            {
                selectedEncoding = value;
                IsEncodingPending = true;
                RaisePropertyChanged(()=>SelectedEncoding);
            }
        }

        public bool IsEncodingPending
        {
            get { return isEncodingPending; }
            set
            {
                isEncodingPending = value;
                RaisePropertyChanged(()=>IsEncodingPending);
            }
        }

        private void Initialize()
        {
            Dictionary<string, object> information = new Dictionary<string, object>();
            information.Add("Layer Name", layer.Name);
            information.Add("Transparency", string.Format("{0:N0} %", layer.Transparency * 100 / 255));
            if (layer.HasBoundingBox)
            {
                if (!layer.IsOpen) layer.Open();
                RectangleShape boundingBox = layer.GetBoundingBox();
                information.Add("Upper Left X:", boundingBox.UpperLeftPoint.X.ToString("N4"));
                information.Add("Upper Left Y:", boundingBox.UpperLeftPoint.Y.ToString("N4"));
                information.Add("Lower Right X:", boundingBox.LowerRightPoint.X.ToString("N4"));
                information.Add("Lower Right Y:", boundingBox.LowerRightPoint.Y.ToString("N4"));
                layer.Close();
            }

            LayerInformation = information;
        }
    }
}