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
using System.Linq;
using System.ComponentModel;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class SharedViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private static SharedViewModel instance;

        private SharedViewModel()
        { }

        public static SharedViewModel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SharedViewModel();
                }

                return instance;
            }
        }

        public GisEditorTrackInteractiveOverlay TrackOverlay
        {
            get
            {
                if (GisEditor.ActiveMap != null)
                {
                    var trackOverlay = GisEditor.ActiveMap.InteractiveOverlays.OfType<GisEditorTrackInteractiveOverlay>().FirstOrDefault();
                    return trackOverlay;
                }
                return null;
            }
        }

        public GisEditorEditInteractiveOverlay EditOverlay
        {
            get
            {
                if (GisEditor.ActiveMap != null)
                {
                    GisEditorEditInteractiveOverlay editOverlay = GisEditor.ActiveMap.FeatureLayerEditOverlay;
                    if (editOverlay != null)
                    {
                        return editOverlay;
                    }
                }
                return null;
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}