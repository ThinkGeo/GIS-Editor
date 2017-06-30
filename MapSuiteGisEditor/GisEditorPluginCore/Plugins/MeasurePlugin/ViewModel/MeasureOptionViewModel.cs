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
using System;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class MeasureOptionViewModel : ViewModelBase
    {
        private MeasureSetting measureOption;

        public MeasureOptionViewModel(MeasureSetting measureOption)
        {
            this.measureOption = measureOption;
        }

        public DistanceUnit SelectedDistanceUnit
        {
            get { return measureOption.SelectedDistanceUnit; }
            set
            {
                measureOption.SelectedDistanceUnit = value;
                RaisePropertyChanged(() => SelectedDistanceUnit);
            }
        }

        public AreaUnit SelectedAreaUnit
        {
            get { return measureOption.SelectedAreaUnit; }
            set
            {
                measureOption.SelectedAreaUnit = value;
                RaisePropertyChanged(() => SelectedAreaUnit);
            }
        }

        public bool AllowCollectFixedElements
        {
            get { return measureOption.AllowCollectFixedElements; }
            set
            {
                measureOption.AllowCollectFixedElements = value;
                RaisePropertyChanged(() => AllowCollectFixedElements);
            }
        }

        public bool UseGdiPlusInsteadOfDrawingVisual
        {
            get { return measureOption.UseGdiPlusInsteadOfDrawingVisual; }
            set
            {
                measureOption.UseGdiPlusInsteadOfDrawingVisual = value;
                RaisePropertyChanged(() => UseGdiPlusInsteadOfDrawingVisual);
            }
        }

        internal void RaiseProperty()
        {
            RaisePropertyChanged(() => SelectedDistanceUnit);
            RaisePropertyChanged(() => SelectedAreaUnit);
        }
    }
}
