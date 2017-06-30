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


using System.Globalization;
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class ZoomLevelViewModel : ViewModelBase
    {
        private bool isRenaming;
        private int zoomLevelIndex;
        private double scale;

        public ZoomLevelViewModel(int zoomLevelIndex, double scale)
        {
            this.zoomLevelIndex = zoomLevelIndex;
            this.scale = scale;
        }

        public bool IsRenaming
        {
            get { return isRenaming; }
            set
            {
                isRenaming = value;
                RaisePropertyChanged(()=>IsRenaming);
            }
        }

        public int ZoomLevelIndex
        {
            get { return zoomLevelIndex; }
            set
            {
                zoomLevelIndex = value;
                RaisePropertyChanged(()=>ZoomLevelText);
            }
        }

        public double Scale
        {
            get { return scale; }
            set { scale = value; }
        }

        public string ZoomLevelText
        {
            get { return string.Format(CultureInfo.InvariantCulture, "Level {0:D2}", ZoomLevelIndex + 1); }
        }
    }
}
